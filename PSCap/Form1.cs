using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PSCap
{
    public partial class Form1 : Form
    {
        List<string> items = new List<string>();
        Thread adderThread;
        volatile bool killThread = false;
        bool followLast = true;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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

            adderThread = new Thread(updateList);
            adderThread.Start();
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
                addItem(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());
                i++;
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
            //killThread = true;
            adderThread.Abort();
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
