using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        List<string> items = new List<string>();
        const string NO_INSTANCE_PLACEHOLDER = "No instances";
        ProcessScanner scanner = new ProcessScanner("PlanetSide");
        // manages the state of the logger and the transitions from detached attached etc
        CaptureLogic captureLogic = new CaptureLogic("PSLogServer" + Program.LoggerId, "pslog.dll"); 
        CaptureFile captureFile;
        int lastSelectedInstanceIndex = 0;
        //volatile bool killThread = false;
        bool followLast = true;
        int loggerId = 0;

        public PSCapMain(int loggerId)
        {
            this.loggerId = loggerId;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            scanner.ProcessListUpdate += new EventHandler<Process []>(processList_update);
            captureLogic.AttachedProcessExited += new EventHandler(attachedProcessExited);

            // set the logger ID
            this.toolStripLoggerID.Text = "Logger ID " + loggerId;

            // start off detached and with no open game instances
            enterUIState(UIState.Detached);
        }

        private void initListView()
        {
            listView1.VirtualMode = false;
            listView1.VirtualListSize = 0;
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.EnableDoubleBuffer();

            //Add column header
            listView1.Columns.Add("Time", 140);
            listView1.Columns.Add("Event", 100);
            listView1.Columns.Add("Data", 100);
        }

        private void attachedProcessExited(object o, EventArgs e)
        {
            if (!captureLogic.isAttached())
            {
                Log.Info("Process exited but we weren't attached. Don't care");
                return;
            }

            Log.Warning("attached process has exited. Detach forced...");
            Process p = captureLogic.getAttachedProcess().Process;
            captureLogic.detach();

            // XXX: this is shit. We have to "stop capture" then detach. Weird state
            // to be in, but it saves code at the expense of breaking some models
            enterUIState(UIState.Attached);
            enterUIState(UIState.Detaching);
            enterUIState(UIState.Detached);

            // TODO: add more messagebox types for capturing/not capturing
            if(p.ExitCode == 0)
                MessageBox.Show("The attached process has exited safely. Detach forced.", "Process Exited",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(string.Format("The attached process has crashed with exit code 0x{0:X}. Detach forced.", p.ExitCode),
                    "Process Crashed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void disableProcessSelection(DisableProcessSelectionReason reason)
        {
            switch(reason)
            {
                case DisableProcessSelectionReason.NoInstances:
                    Console.WriteLine("Disabling process selection due to no instances");
                    this.SafeInvoke(delegate
                    {
                        toolStripInstance.Items.Clear();
                        toolStripInstance.Enabled = false;
                        toolStripInstance.Items.Add(NO_INSTANCE_PLACEHOLDER);
                        toolStripInstance.SelectedIndex = 0;
                        toolStripAttachButton.Enabled = false;
                    });
                    break;
                case DisableProcessSelectionReason.Attached:
                    Console.WriteLine("Disabling process selection because we're attached");
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

        private void addItem(string item)
        {
            this.SafeInvoke(delegate 
            {
                //items.Add(item);
                string[] row = new string[2];

                row[0] = item;// items[items.Count-1].ToString();
                row[1] = "";

                listView1.Items.Add(new ListViewItem(row));
                //listView1.VirtualListSize++;

                if (followLast)
                    scrollToEnd();

                setStatus(listView1.Items.Count + " packets");
            });
        }

        private void setStatus(string status)
        {
            this.SafeInvoke(delegate
            {
                toolStripStatus.Text = status;
            });
        }

        void enterUIState(UIState state)
        {
            switch(state)
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
                        capturePauseButton.Text = "Capturing...";

                        toolStripAttachButton.Text = "Detach";
                        toolStripAttachButton.Enabled = false;

                        // status bar
                        statusStrip1.BackColor = Color.DarkRed;
                        setStatus("Capturing...");
                    });
                    break;
            }
        }

        private void scrollToEnd()
        {
            this.SafeInvoke(delegate
            {
                //Console.WriteLine("Visible " + listView1.VirtualListSize.ToString());
                listView1.EnsureVisible(listView1.Items.Count - 1);
            });
        }
        
        private void listView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            //A cache miss, so create a new ListViewItem and pass it back. 
            string[] row = new string[2];

            row[0] = items[e.ItemIndex].ToString();
            row[1] = row[0];
            e.Item = new ListViewItem(row);
        }

        private void listView1_OnScroll(object sender, ScrollEventArgs e)
        {
            Console.WriteLine("Scroll event old " + e.OldValue + ", new " + e.NewValue + ", target " + listView1.VirtualListSize);

            int itemHeight;

            if (listView1.Items.Count == 0)
                itemHeight = 0;
            else
                itemHeight = listView1.GetItemRect(0).Height;

            if (itemHeight == 0) // bad!
                return;

            // mad hax
            int itemsDisplayed = listView1.DisplayRectangle.Height / itemHeight;

            if (e.NewValue + itemsDisplayed >= listView1.Items.Count)
                followLast = true;
            else
                followLast = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //killThread = true;
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            /*foreach(var s in listView1.SelectedIndices)
            {
                Console.WriteLine("New selection " + s.ToString());
            }*/

            richTextBox1.SelectionColor = Color.Red;
            richTextBox1.AppendText("Wat"); // dont remove this if you want colors

            if (listView1.SelectedIndices.Count == 0)
                richTextBox1.Text = ("No selection");
            else
                richTextBox1.Text = "Item " + listView1.Items[listView1.SelectedIndices[0]].Text;
            
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //killThread = true;
        }

        private void capturePauseButton_Click(object sender, EventArgs e)
        {
            if(captureLogic.isCapturing())
            {
                captureLogic.stopCapture();
                enterUIState(UIState.Attached);
                //Console.WriteLine("Stopping capture " + captureFile.ToString());
                //enterState(CaptureState.Attached);
            }
            else
            {
                captureLogic.capture();
                enterUIState(UIState.Capturing);
                //Console.WriteLine("Starting capture for " + (ProcessCollectable)toolStripInstance.SelectedItem);
                //enterState(CaptureState.Capturing);
                //captureFile = new CaptureFile();
            }
        }

        // guard against any strange behavior
        private bool isProcessSelected()
        {
            return toolStripInstance.SelectedItem != null &&
                toolStripInstance.Enabled &&
                toolStripInstance.SelectedItem.ToString() != NO_INSTANCE_PLACEHOLDER;
        }

        private void toolStripAttachButton_Click(object sender, EventArgs e)
        {
            if(captureLogic.isAttached())
            {
                enterUIState(UIState.Detaching);

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(500);
                    captureLogic.detach();
                    enterUIState(UIState.Detached);
                });
            }
            else
            {
                if (isProcessSelected())
                {
                    enterUIState(UIState.Attaching);

                    captureLogic.attach((ProcessCollectable)toolStripInstance.SelectedItem,
                        (okay, attachResult, message) =>
                        {
                            if (okay)
                            {
                                enterUIState(UIState.Attached);
                                return;
                            }

                            enterUIState(UIState.Detached);
                            MessageBox.Show(message, "Failed to Attach", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        });
                }
                else
                    Debug.Assert(false, "Attemped to attach without first selecting a process");
            }
        }
    }

    public static class ISynchronizeInvokeExtensions
    {
        public static void SafeInvoke<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }
    }
}
