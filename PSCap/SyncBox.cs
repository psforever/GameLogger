using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace PSCap
{
    class SyncBox : TextBox
    {
        public SyncBox()
        {
            this.Multiline = true;
            this.ScrollBars = ScrollBars.Vertical;
        }

        private List<Control> syncers = new List<Control>();

        private static bool scrolling;   // In case someone else tries to scroll us

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            // Trap WM_VSCROLL message and pass to syncers
            if ((m.Msg == 0x115 || m.Msg == 0x20a) && !scrolling && syncers.Count > 0)
            {
                scrolling = true;
                foreach (Control c in syncers)
                {
                    if (c.IsHandleCreated)
                    {
                        SendMessage(c.Handle, m.Msg, m.WParam, m.LParam);
                    }
                }
                scrolling = false;
            }
        }

        public void SyncWith(Control whom)
        {
            syncers.Add(whom);
        }

        public void ClearSync()
        {
            syncers.Clear();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }
}