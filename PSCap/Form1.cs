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
        List<string> items = new List<string>();
        ProcessScanner scanner = new ProcessScanner("PlanetSide");
        volatile bool killThread = false;
        bool followLast = true;
        int loggerId = 0;

        public PSCapMain(int loggerId)
        {
            this.loggerId = loggerId;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            disableProcessSelection();

            listView1.VirtualMode = false;
            listView1.VirtualListSize = 0;
            listView1.View = View.Details;
            //listView1.View = View.SmallIcon;
            //listView1.GridLines = true;
            listView1.FullRowSelect = true;
            listView1.EnableDoubleBuffer();

            //Add column header
            listView1.Columns.Add("Time", 140);
            listView1.Columns.Add("Event", 100);
            listView1.Columns.Add("Data", 100);

            this.toolStripLoggerID.Text = "Logger ID " + loggerId;

            Task.Factory.StartNew(updateList);
            scanner.ProcessListUpdate += new ProcessListUpdateHandler(processList_update);
            scanner.startScanning();

            /*                    this.SafeInvoke(delegate
                    {
                        toolStripComboBox1.Items.Clear();
                        toolStripComboBox1.Enabled = false;
                        toolStripComboBox1.Items.Add("No instances");
                        toolStripComboBox1.SelectedIndex = 0;
                    });
                }
                else
                {
                    this.SafeInvoke(delegate
                    {
                        toolStripComboBox1.Items.Clear();
                        toolStripComboBox1.Enabled = true;
                    });

                    foreach (Process p in psProcesses)
                    {
                        this.SafeInvoke(delegate
                        {
                            toolStripComboBox1.Items.Add(p.ProcessName + " (PID " + p.Id + ")");
                        */
        }

        void disableProcessSelection()
        {
            this.SafeInvoke(delegate
            {
                toolStripComboBox1.Items.Clear();
                toolStripComboBox1.Enabled = false;
                toolStripComboBox1.Items.Add("No instances");
                toolStripComboBox1.SelectedIndex = 0;

                capturePauseButton.Enabled = false;
            });
        }

        void enableProcessSelection()
        {
            this.SafeInvoke(delegate
            {
                toolStripComboBox1.Items.Clear();
                toolStripComboBox1.Enabled = true;
            });
        }

        private void processList_update(Process[] list)
        {
            if(list.Length == 0)
            {
                disableProcessSelection();
                return;
            }

            enableProcessSelection();

            foreach (Process p in list)
            {
                this.SafeInvoke(delegate
                {
                    toolStripComboBox1.Items.Add(new ProcessCollectable(p));
                });
            }

            // select the first item as a convienience
            this.SafeInvoke(f => f.toolStripComboBox1.SelectedIndex = 0);
        }
   
        private class ProcessCollectable
        {
            Process process;

            public ProcessCollectable(Process p)
            {
                process = p;
            }

            public override string ToString()
            {
                return process.ProcessName + " (PID " + process.Id + ")";
            }
        }

        private void addItem(string item)
        {
            this.SafeInvoke(delegate 
            {
                //items.Add(item);
                string[] row = new string[2];

                row[0] = item;// items[items.Count-1].ToString();
                row[1] = "";
                //row[1] = row[0];
                listView1.Items.Add(new ListViewItem(row));
                //listView1.VirtualListSize++;

                if (followLast)
                    scrollToEnd();

                toolStripItemCount.Text = "" + listView1.Items.Count + " packets";
            });
        }

        private void scrollToEnd()
        {
            this.SafeInvoke(delegate
            {
                //Console.WriteLine("Visible " + listView1.VirtualListSize.ToString());
                listView1.EnsureVisible(listView1.Items.Count - 1);
            });
        }

        private void updateList()
        {
            int i = 0;

            while (!killThread)
            {
                NamedPipeServerStream server = null;

                try
                {
                    server = new NamedPipeServerStream("PSLogServer"+loggerId);
                }
                catch(IOException e)
                {
                    MessageBox.Show("Failed to listen on named pipe", "Pipe Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    killThread = true;
                    Application.Exit();
                    continue;
                }
                
                server.WaitForConnection();
                //StreamReader reader = new StreamReader(server);
                Console.WriteLine("New connection to pipe server");
                StreamWriter writer = new StreamWriter(server);

                //reader.Read(buf,
                /*while (true)
                {
                    var line = reader.ReadLine();
                    writer.WriteLine(String.Join("", line.Reverse()));
                    writer.Flush();
                }*/


            writer.WriteLine("HEY BUD");
                writer.Close();

                /*addItem(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());
                i++;*/
                //Thread.Sleep(10);
            }
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
            killThread = true;
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
            killThread = true;
        }

        private void toolStripComboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ToolStripComboBox comboBox = (ToolStripComboBox)sender;

            if(comboBox.SelectedItem == null)
            {
                capturePauseButton.Enabled = false;
                return;
            }

            capturePauseButton.Enabled = true;
        }

        private void capturePauseButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Starting capture for " + (ProcessCollectable)toolStripComboBox1.SelectedItem);
            //capturePauseButton.Image = Properties.Resources.StatusAnnotations_Stop_16xLG_color;
            //capturePauseButton.Text = "Capturing...";
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
