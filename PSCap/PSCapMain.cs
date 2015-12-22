using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSCap
{ 
    public partial class PSCapMain : Form
    {
        enum DisableProcessSelectionReason
        {
            NoInstances,
            Attached,
        }

        enum UIState
        {
            Detached,
            Detaching,
            Attached,
            Attaching,
            Capturing,
        }


        // bump these when editing DllMessages or capture records
        public const byte GAME_LOGGER_MAJOR_VERSION = 1;
        public const byte GAME_LOGGER_MINOR_VERSION = 1;

        const string NO_INSTANCE_PLACEHOLDER = "No instances";
        ProcessScanner scanner = new ProcessScanner("PlanetSide");
        // manages the state of the logger and the transitions from detached attached etc
        CaptureLogic captureLogic = new CaptureLogic("PSLogServer" + Program.LoggerId, Path.Combine(Environment.CurrentDirectory, "pslog.dll"));
        CaptureFile captureFile = null;
        ulong estimatedCaptureSize = 0;

        int lastSelectedInstanceIndex = 0;
        bool followLast = true;
        int loggerId = 0;

        private ByteViewer byteViewer1;
        private UIState currentUIState = UIState.Detached;

        public PSCapMain(int loggerId)
        {
            this.loggerId = loggerId;
            InitializeComponent();

            // required for hotkey hooking with key modifiers
            this.KeyPreview = true;

            byteViewer1 = new ByteViewer();
            byteViewer1.Dock = System.Windows.Forms.DockStyle.Fill;
            byteViewer1.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            this.byteViewer1.Location = new System.Drawing.Point(0, 0);
            this.byteViewer1.Name = "byteViewer1";
            this.byteViewer1.Size = new System.Drawing.Size(771, 150);
            this.splitContainer1.Panel2.Controls.Add(byteViewer1);

            splitContainer1.PerformLayout();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // set the logger ID
            this.toolStripLoggerID.Text = "Logger ID " + loggerId;

            try
            {
                Log.logFile = new StreamWriter("PSGameLogger" + loggerId + "_log.txt", false);
                Log.logFile.AutoFlush = true;
                Log.logFile.WriteLine("PS1 GameLogger Logging started at " + DateTime.Now);
            }
            catch(IOException ex)
            {
                MessageBox.Show("Failed to create log file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            scanner.ProcessListUpdate += new EventHandler<Process []>(processList_update);
            captureLogic.AttachedProcessExited += new AttachedProcessExited(attachedProcessExited);
            captureLogic.NewEvent += new NewEventCallback(newUIEvent);
            captureLogic.OnNewRecord += new NewGameRecord(newRecord);

            // start off detached and with no open game instances
            enterUIState(UIState.Detached);

            setCaptureFile(captureFile);
            initListView();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.ApplicationExitCall)
                return;

            // TODO: handle the cases where we are attached, capturing, or have an unsaved capture
            if (captureFile != null && captureFile.isModified())
            {
                DialogResult result = MessageBox.Show("You have an unsaved capture file. Would you like to save it before exiting?",
                    "Save capture file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    if (captureLogic.isCapturing())
                        captureLogic.stopCapture();

                    bool canceled;
                    saveCaptureFile(out canceled);

                    if (canceled)
                    {
                        e.Cancel = true;
                    }

                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }

            if(e.Cancel)
            {
                Log.Info("Form close cancelled");
                return;
            }

            Log.Info("Form closing");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch(keyData)
            {
                case Keys.F9:
                    if (capturePauseButton.Enabled)
                        capturePauseButton_Click(this, new EventArgs());
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void initListView()
        {
            listView1.VirtualMode = true;
            listView1.VirtualListSize = 0;

            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.EnableDoubleBuffer();

            //Add column header
            listView1.Columns.Add("Event", 150);
            listView1.Columns.Add("Time", 100);
            listView1.Columns.Add("Type", 150);
            listView1.Columns.Add("Size", 30);

            eventImageList.Images.Add(global::PSCap.Properties.Resources.arrow_Up_16xLG_green);
            eventImageList.Images.Add(global::PSCap.Properties.Resources.arrow_Down_16xLG_red);
            eventImageList.Images.Add(global::PSCap.Properties.Resources.lock_16xLG);

            listView1.SmallImageList = eventImageList;
        }

        private void newRecord(List<GameRecord> recs)
        {
            List<Record> newItems = new List<Record>(recs.Count);

            foreach (GameRecord gameRec in recs)
            {
                Record rec = Record.Factory.Create(RecordType.GAME);

                switch(gameRec.type)
                {
                    case GameRecordType.PACKET:
                        GameRecordPacket record = gameRec as GameRecordPacket;
                        RecordGame gameRecord = rec as RecordGame;
                        gameRecord.setRecord(record);

                        /// XXX: nasty hack to prevent password disclosures
                        byte [] sensitive = { 0x00, 0x09, 0x00, 0x00, 0x01, 0x03 };
                        int i = 0;

                        for(i = 0; i < record.packet.Count && i < sensitive.Length; i++)
                        {
                            if (record.packet[i] != sensitive[i])
                                break;
                        }

                        if(i == sensitive.Length)
                        {
                            Log.Info("Found sensitive login packet. Scrubbing from the record");
                            record.packet.Clear();
                            record.packet.AddRange(sensitive);
                        }
                        break;
                    default:
                        Trace.Assert(false, string.Format("NewRecord: Unhandled record type {0}", gameRec.type));
                        break;
                }

                addRecordSizeEstimate(rec.size);
                captureFile.addRecord(rec);
            }

            setRecordCount(captureFile.getNumRecords());
        }

        private void attachedProcessExited(Process p)
        {
            Log.Warning("attached process has exited");

            // XXX: this is shit. We have to "stop capture" then detach. Weird state
            // to be in, but it saves code at the expense of breaking some models
            //enterUIState(UIState.Attached);
            //enterUIState(UIState.Detaching);
            //enterUIState(UIState.Detached);

            // TODO: add more messagebox types for capturing/not capturing

            /*this.SafeInvoke(delegate
            {
                if (p.ExitCode == 0)
                    MessageBox.Show("The attached process has exited safely.", "Process Exited",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(string.Format("The attached process has crashed with exit code 0x{0:X}.", p.ExitCode),
                        "Process Crashed",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
            });*/
        }

        void disableProcessSelection(DisableProcessSelectionReason reason)
        {
            switch(reason)
            {
                case DisableProcessSelectionReason.NoInstances:
                    Log.Debug("Disabling process selection due to no instances");
                    this.SafeInvoke(delegate
                    {
                        toolStripInstance.Items.Clear();
                        toolStripInstance.Enabled = false;
                        toolStripInstance.Items.Add(NO_INSTANCE_PLACEHOLDER);
                        toolStripInstance.SelectedIndex = 0;
#if !WITHOUT_GAME
                        toolStripAttachButton.Enabled = false;
#endif
                    });
                    break;
                case DisableProcessSelectionReason.Attached:
                    Log.Debug("Disabling process selection because we're attached");
                    this.SafeInvoke(delegate
                    {
                        toolStripInstance.Enabled = false;
                    });
                    break;
            }
        }

        void enableProcessSelection()
        {
            this.SafeInvoke(delegate
            {
                toolStripInstance.Enabled = true;
                toolStripAttachButton.Enabled = true;
            });
        }

        private void processList_update(object from, Process[] list)
        {
            this.SafeInvoke(delegate
            {
                if (list.Length == 0)
                {
                    disableProcessSelection(DisableProcessSelectionReason.NoInstances);
                    return;
                }

                // we have at least one element
                // clear list, refill it, select first item, enable selection
                toolStripInstance.Items.Clear();

                foreach (Process p in list)
                {
                    toolStripInstance.Items.Add(new ProcessCollectable(p));
                }

                // select the last item as a convienience
                if (lastSelectedInstanceIndex < list.Length && lastSelectedInstanceIndex >= 0)
                    toolStripInstance.SelectedIndex = lastSelectedInstanceIndex;
                else
                    toolStripInstance.SelectedIndex = 0;

                enableProcessSelection();
            });
        }

        private void setStatus(string status)
        {
            this.SafeInvoke(delegate
            {
                toolStripStatus.Text = status;
            });
        }

        private void setRecordSizeEstimate(ulong bytes)
        {
            estimatedCaptureSize = bytes;
        }

        private void addRecordSizeEstimate(ulong bytes)
        {
            estimatedCaptureSize += bytes;
        }

        private void updateCaptureFileState()
        {
            this.SafeInvoke(delegate
            {
                if (captureFile == null)
                {
                    saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    copyToolStripMenuItem.Enabled = false;
                    openToolStripMenuItem.Enabled = true;

                    setCaptureFileName("");
                    return;
                }

                if (currentUIState == UIState.Capturing)
                {
                    saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    copyToolStripMenuItem.Enabled = true;
                    openToolStripMenuItem.Enabled = false;
                    return;
                }

                string filename = Path.GetFileName(captureFile.getCaptureFilename());

                if (captureFile.isModified())
                {
                    saveToolStripMenuItem.Enabled = true;
                    filename += " (modified)";
                }
                else
                {
                    saveToolStripMenuItem.Enabled = false;
                }

                setCaptureFileName(filename);

                saveAsToolStripMenuItem.Enabled = true;
                copyToolStripMenuItem.Enabled = true;
                openToolStripMenuItem.Enabled = true;
            });
        }


        private void setCaptureFile(CaptureFile cap)
        {
            this.SafeInvoke(delegate
            {
                // must be set before
                captureFile = cap;

                if (cap == null)
                {
                    // set the estimate before updating the record count
                    setRecordSizeEstimate(0);
                    setRecordCount(0);

                    updateCaptureFileState();
                }
                else
                {
                    ulong estimatedSize = 0;

                    foreach (Record r in cap.getRecords())
                        estimatedSize += r.size;

                    setRecordSizeEstimate(estimatedSize);
                    setRecordCount(cap.getNumRecords());

                    updateCaptureFileState();
                }

            });
        }

        private void setRecordCount(int count)
        {
            this.SafeInvoke(delegate
            {
                listView1.SetVirtualListSize(count);
                if (followLast)
                    scrollToEnd();

                if (count == 0)
                {
                    recordCountLabel.Visible = false;
                    toolStripStatus.BorderSides = ToolStripStatusLabelBorderSides.None;
                }
                else
                {
                    recordCountLabel.Text = string.Format("{0} record{1}{2}",
                        count, count == 1 ? "" : "s",
                        estimatedCaptureSize == 0 ? "" :
                        string.Format(" ({0})", Util.BytesToString((long)estimatedCaptureSize)));
                    recordCountLabel.Visible = true;
                    toolStripStatus.BorderSides = ToolStripStatusLabelBorderSides.Right;
                }
            });
        }

        private void setCaptureFileName(string name)
        {
            this.SafeInvoke(delegate
            {
                if (string.Empty == name)
                    captureFileLabel.Text = "No capture file";
                else
                    captureFileLabel.Text = name;
            });
        }

        void enterUIState(UIState state)
        {
            currentUIState = state;

            switch (state)
            {
                case UIState.Detached:
                    Log.Info("UIState Detached");

                    this.SafeInvoke(delegate
                    {
                        disableProcessSelection(DisableProcessSelectionReason.NoInstances);
                        capturePauseButton.Enabled = false;

                        toolStripAttachButton.Text = "Attach";
                        toolStripAttachButton.Enabled = true;
                        // in the detached state, the scanner task and selection callback control
                        // the enabled state of the Attach/Detach button

                        // status bar
                        statusStrip1.BackColor = SystemColors.Highlight;
                        setStatus("Detached");

                        // start scanning for our target process
                        scanner.startScanning();
                    });
                    break;
                case UIState.Detaching:
                    Log.Info("UIState Detaching");

                    this.SafeInvoke(delegate
                    {
                        toolStripAttachButton.Text = "Detaching";
                        toolStripAttachButton.Enabled = false;
                    });
                    break;
                case UIState.Attached:
                    Log.Info("UIState Attached");

                    this.SafeInvoke(delegate
                    {
                        // no need to scan anymore
                        scanner.stopScanning();
                        disableProcessSelection(DisableProcessSelectionReason.Attached);

                        // save the last selected instance ID to come back to
                        lastSelectedInstanceIndex = toolStripInstance.SelectedIndex;

                        capturePauseButton.Image = Properties.Resources.StatusAnnotations_Play_16xLG_color;
                        capturePauseButton.Text = "Capture";
                        capturePauseButton.Enabled = true;
                        
                        toolStripAttachButton.Text = "Detach";
                        toolStripAttachButton.Enabled = true;

                        updateCaptureFileState();

                        openToolStripMenuItem.Enabled = true;

                        // status bar
                        statusStrip1.BackColor = Color.DarkGreen;
                        setStatus("Ready to capture");
                    });
                    break;
                case UIState.Attaching:
                    Log.Info("UIState Attaching");

                    this.SafeInvoke(delegate
                    {
                        toolStripAttachButton.Text = "Attaching";
                        toolStripAttachButton.Enabled = false;
                    });
                    break;
                case UIState.Capturing:
                    Log.Info("UIState Capturing");

                    this.SafeInvoke(delegate
                    {
                        capturePauseButton.Image = Properties.Resources.StatusAnnotations_Stop_16xLG_color;
                        capturePauseButton.Text = "Stop Capture";
                        capturePauseButton.Enabled = true;

                        toolStripAttachButton.Text = "Detach";
                        toolStripAttachButton.Enabled = false;

                        updateCaptureFileState();

                        // status bar
                        statusStrip1.BackColor = Color.DarkRed;
                        setStatus("Capturing...");
                    });
                    break;
                default:
                    Trace.Assert(false, "Unhandled UIState " + state.ToString());
                    break;
            }
        }

        private void scrollToEnd()
        {
            if(listView1.Items.Count > 0)
                this.SafeInvoke(delegate
                {
                    listView1.EnsureVisible(listView1.Items.Count - 1);
                });
        }
        
        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            RecordGame i = captureFile.getRecord(e.ItemIndex) as RecordGame;

            string[] row = new string[4];

            double time = i.getSecondsSinceStart((uint)captureFile.getStartTime());

            GameRecordPacket record = i.Record as GameRecordPacket;

            string bytes = "";
            bytes = ((PlanetSideMessageType)record.packet[0]).ToString();
            //foreach (byte b in record.packet)
            //    bytes += string.Format("{0:X2} ", b);

            string eventName = record.packetDestination == GameRecordPacket.PacketDestination.Client ? "Received Packet" : "Sent Packet";

            row[0] = eventName;
            row[1] = string.Format("{0:0.000000}", time);
            row[2] = bytes;
            row[3] = record.packet.Count.ToString();


            e.Item = new ListViewItem(row);
            e.Item.ImageIndex = record.packetDestination == GameRecordPacket.PacketDestination.Server ? 0 : 1;
        }

        private void listView1_OnScroll(object sender, ScrollEventArgs e)
        {
            int itemHeight;
            
            if (listView1.VirtualListSize == 0)
                itemHeight = 0;
            else
                itemHeight = listView1.GetItemRect(0).Height;

            if (itemHeight == 0) // bad!
                return;

            // mad hax
            int itemsDisplayed = listView1.DisplayRectangle.Height / itemHeight;
            
            if (e.NewValue + itemsDisplayed >= listView1.VirtualListSize)
                followLast = true;
            else
                followLast = false;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*foreach(var s in listView1.SelectedIndices)
            {
                Console.WriteLine("New selection " + s.ToString());
            }*/

            richTextBox1.Clear();
            richTextBox1.SelectionColor = Color.Green;
            richTextBox1.AppendText("  "); // dont remove this if you want colors

            if (listView1.SelectedIndices.Count == 0)
                richTextBox1.Text = ("No selection");
            else
            {
                RecordGame record = captureFile.getRecord(listView1.SelectedIndices[0]) as RecordGame;
                GameRecordPacket gameRecord = record.Record as GameRecordPacket;

                string bytes = "";
                string name = ((PlanetSideMessageType)gameRecord.packet[0]).ToString();
                foreach (byte b in gameRecord.packet)
                    bytes += string.Format("{0:X2} ", b);

                byteViewer1.SetBytes(gameRecord.packet.ToArray());

                richTextBox1.AppendText(name + "\n");
                richTextBox1.AppendText(bytes);

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void capturePauseButton_Click(object sender, EventArgs e)
        {
            if(captureLogic.isCapturing())
            {
                capturePauseButton.Enabled = false;
                captureLogic.stopCapture();
            }
            else
            {
                if(captureFile != null && captureFile.isModified())
                {
                    DialogResult result = MessageBox.Show("You have an unsaved capture file. Would you like to save it before starting a new capture?",
                        "Save capture file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                    if (result == DialogResult.Yes)
                    {
                        if (!saveCaptureFile())
                            return;
                    }
                    else if (result == DialogResult.Cancel)
                        return;
                }

                capturePauseButton.Enabled = false;

                // create a new capture file
                setCaptureFile(CaptureFile.Factory.New());
                captureLogic.capture();
            }
        }

        private void newUIEvent(EventNotification evt, bool timeout)
        {
            Log.Info("Got new UIEvent " + evt.ToString());

            if(timeout)
            {
                Log.Info("UIEvent timed out");
                return;
            }

            switch (evt)
            {
                case EventNotification.Attached:
                    enterUIState(UIState.Attached);
                    break;
                case EventNotification.Attaching:
                    enterUIState(UIState.Attaching);
                    break;
                case EventNotification.Detached:
                    enterUIState(UIState.Detached);
                    break;
                case EventNotification.Detaching:
                    enterUIState(UIState.Detaching);
                    break;
                case EventNotification.CaptureStarted:
                    enterUIState(UIState.Capturing);
                    break;
                case EventNotification.CaptureStarting:
                    break;
                case EventNotification.CaptureStopping:
                    break;
                case EventNotification.CaptureStopped:
                    this.SafeInvoke((asd) =>
                    {
                        captureFile.finalize();
                    });

                    enterUIState(UIState.Attached);
                    break;
            }
        }

        // guard against any strange behavior
        private bool isProcessSelected()
        {
#if !WITHOUT_GAME
            return toolStripInstance.SelectedItem != null &&
                toolStripInstance.Enabled &&
                toolStripInstance.SelectedItem.ToString() != NO_INSTANCE_PLACEHOLDER;
#else
            return true;
#endif
        }

        private bool saveCaptureFile()
        {
            bool cancelled;
            return saveCaptureFile(out cancelled);
        }

        private bool doSaveCaptureFile(string filename)
        {
            try
            {
                CaptureFile.Factory.ToFile(captureFile, filename);
                setCaptureFile(captureFile);

                return true;
            }
            catch (IOException e)
            {
                Log.Debug("Failed to save capture file: {0}", e.Message);
                MessageBox.Show(e.Message,
                    "Failed to save capture file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool saveCaptureFile(out bool canceled)
        {
            Log.Info("Save capture file");

            if (captureFile.isFirstSave())
                if (!showEditMetadata())
                {
                    canceled = true;
                    return false;
                }

            SaveFileDialog saveFile = new SaveFileDialog();

            saveFile.FileName = captureFile.getCaptureFilename();
            saveFile.Filter = "Game Capture Files (*.gcap) | *.gcap";
            saveFile.AddExtension = true;
            saveFile.DefaultExt = ".gcap";

            canceled = false;

            DialogResult result = saveFile.ShowDialog();

            if (result == DialogResult.OK)
            {
                return doSaveCaptureFile(saveFile.FileName);
            }
            else if(result == DialogResult.Cancel)
            {
                canceled = true;
            }

            return false;
        }

        private async void toolStripAttachButton_Click(object sender, EventArgs e)
        {
            if(captureLogic.isAttached())
            {
                enterUIState(UIState.Detaching);

                await Task.Factory.StartNew(() =>
                {
                    captureLogic.detach();
                });
            }
            else
            {
                if (!isProcessSelected())
                {
                    Trace.Assert(false, "Attemped to attach without first selecting a process");
                    return;
                }
#if !WITHOUT_GAME
                ProcessCollectable targetProcess = toolStripInstance.SelectedItem as ProcessCollectable;
#else
                ProcessCollectable targetProcess = new ProcessCollectable(Process.GetCurrentProcess());
#endif

                if (targetProcess == null)
                {
                    MessageBox.Show("Target process target was NULL", "Unknown Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                enterUIState(UIState.Attaching);

                captureLogic.attach(targetProcess,
                    (okay, attachResult, message) =>
                    {
                        if (okay)
                        {
                            enterUIState(UIState.Attached);
                            return;
                        }

                        enterUIState(UIState.Detached);

                        if (attachResult == AttachResult.PipeServerStartup)
                        {
                            DialogResult res = MessageBox.Show(message + Environment.NewLine + "Would you like to end the offending process?"
                                , "Failed to Attach", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                            if (res == DialogResult.Yes)
                            {
                                targetProcess.Process.Refresh();

                                if(!targetProcess.Process.HasExited)
                                {
                                    Log.Info("Sending close to process {0} (this may fail)", targetProcess);
                                    targetProcess.Process.CloseMainWindow();
                                }
                            }
                        }
                        else
                        {
                            MessageBox.Show(message, "Failed to Attach", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                    
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs evt)
        {
            if (captureFile.isFirstSave())
                saveCaptureFile();
            else
                doSaveCaptureFile(captureFile.getCaptureFilename());
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs evt)
        {
            if (captureFile != null && captureFile.isModified())
            {
                DialogResult result = MessageBox.Show("You have an unsaved capture file. Would you like to save it before opening capture?",
                    "Save capture file", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    if (!saveCaptureFile())
                        return;
                }
                else if (result == DialogResult.Cancel)
                    return;
            }

            Log.Info("Open capture");

            OpenFileDialog openFile = new OpenFileDialog();
            openFile.AddExtension = true;
            openFile.Filter = "Game Capture Files (*.gcap)|*.gcap|All Files (*.*)|*.*";

            if (openFile.ShowDialog() == DialogResult.OK)
            {
                BackgroundWorker worker = new BackgroundWorker();
                ProgressDialog progress = new ProgressDialog("Loading capture file");
                progress.ProgressTemplate("Loading records {value}/{max}...");

                // NOTE: hack alert. BeginInvoke merely posts this to the message pump
                this.BeginInvoke(new Action(() => progress.ShowDialog()));

                await Task.Run(delegate
                {
                    try
                    {
                        CaptureFile newCapFile = CaptureFile.Factory.FromFile(openFile.FileName, this, progress);
                        setCaptureFile(newCapFile);
                    }
                    catch (InvalidCaptureFileException e)
                    {
                        Log.Debug("Failed to open capture file: {0}", e.Message);
                        MessageBox.Show(e.Message, "Could not open capture file",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    this.SafeInvoke((a) => progress.Done());
                });
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveCaptureFile();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Trace.Assert(false, "test");
            AboutBox about = new AboutBox();
            about.ShowDialog();
        }

        private bool showEditMetadata()
        {
            Trace.Assert(captureFile != null, "Capture file is null");

            EditMetadata editMeta = new EditMetadata(captureFile);
            DialogResult result = editMeta.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                captureFile.setCaptureName(editMeta.CaptureNameResult);
                captureFile.setCaptureDescription(editMeta.DescriptionResult);

                updateCaptureFileState();

                return true;
            }
            
            return false;
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showEditMetadata();
        }

        private void hotkeysToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HotkeysDialog dialog = new HotkeysDialog();
            dialog.ShowDialog();
        }
    }
}
