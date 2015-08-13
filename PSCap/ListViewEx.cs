using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Reflection;

namespace PSCap
{
    public class ListViewEx : ListView
    {
        // Windows messages
        private const int WM_PAINT = 0x000F;
        private const int WM_ERASEBKGND = 0x0014;
        private const int WM_HSCROLL = 0x0114;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_LBUTTONUP = 0x0202;

        // ScrollBar types
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;

        // ScrollBar interfaces
        private const int SIF_TRACKPOS = 0x10;
        private const int SIF_RANGE = 0x01;
        private const int SIF_POS = 0x04;
        private const int SIF_PAGE = 0x02;
        private const int SIF_ALL = SIF_RANGE | SIF_PAGE | SIF_POS | SIF_TRACKPOS;

        // ListView messages
        private const int LVM_FIRST = 0x1000;
        private const int LVM_SCROLL = LVM_FIRST + 0x20;
        private const int LVM_SETGROUPINFO = (LVM_FIRST + 147);

        public enum ListViewExtendedStyles
        {
            /// <summary>
            /// LVS_EX_GRIDLINES
            /// </summary>
            GridLines = 0x00000001,
            /// <summary>
            /// LVS_EX_SUBITEMIMAGES
            /// </summary>
            SubItemImages = 0x00000002,
            /// <summary>
            /// LVS_EX_CHECKBOXES
            /// </summary>
            CheckBoxes = 0x00000004,
            /// <summary>
            /// LVS_EX_TRACKSELECT
            /// </summary>
            TrackSelect = 0x00000008,
            /// <summary>
            /// LVS_EX_HEADERDRAGDROP
            /// </summary>
            HeaderDragDrop = 0x00000010,
            /// <summary>
            /// LVS_EX_FULLROWSELECT
            /// </summary>
            FullRowSelect = 0x00000020,
            /// <summary>
            /// LVS_EX_ONECLICKACTIVATE
            /// </summary>
            OneClickActivate = 0x00000040,
            /// <summary>
            /// LVS_EX_TWOCLICKACTIVATE
            /// </summary>
            TwoClickActivate = 0x00000080,
            /// <summary>
            /// LVS_EX_FLATSB
            /// </summary>
            FlatsB = 0x00000100,
            /// <summary>
            /// LVS_EX_REGIONAL
            /// </summary>
            Regional = 0x00000200,
            /// <summary>
            /// LVS_EX_INFOTIP
            /// </summary>
            InfoTip = 0x00000400,
            /// <summary>
            /// LVS_EX_UNDERLINEHOT
            /// </summary>
            UnderlineHot = 0x00000800,
            /// <summary>
            /// LVS_EX_UNDERLINECOLD
            /// </summary>
            UnderlineCold = 0x00001000,
            /// <summary>
            /// LVS_EX_MULTIWORKAREAS
            /// </summary>
            MultilWorkAreas = 0x00002000,
            /// <summary>
            /// LVS_EX_LABELTIP
            /// </summary>
            LabelTip = 0x00004000,
            /// <summary>
            /// LVS_EX_BORDERSELECT
            /// </summary>
            BorderSelect = 0x00008000,
            /// <summary>
            /// LVS_EX_DOUBLEBUFFER
            /// </summary>
            DoubleBuffer = 0x00010000,
            /// <summary>
            /// LVS_EX_HIDELABELS
            /// </summary>
            HideLabels = 0x00020000,
            /// <summary>
            /// LVS_EX_SINGLEROW
            /// </summary>
            SingleRow = 0x00040000,
            /// <summary>
            /// LVS_EX_SNAPTOGRID
            /// </summary>
            SnapToGrid = 0x00080000,
            /// <summary>
            /// LVS_EX_SIMPLESELECT
            /// </summary>
            SimpleSelect = 0x00100000
        }

        public enum ListViewMessages
        {
            SetExtendedStyle = (LVM_FIRST + 54),
            GetExtendedStyle = (LVM_FIRST + 55),
        }

        public enum ScrollBarCommands : int
        {
            SB_LINEUP = 0,
            SB_LINELEFT = 0,
            SB_LINEDOWN = 1,
            SB_LINERIGHT = 1,
            SB_PAGEUP = 2,
            SB_PAGELEFT = 2,
            SB_PAGEDOWN = 3,
            SB_PAGERIGHT = 3,
            SB_THUMBPOSITION = 4,
            SB_THUMBTRACK = 5,
            SB_TOP = 6,
            SB_LEFT = 6,
            SB_BOTTOM = 7,
            SB_RIGHT = 7,
            SB_ENDSCROLL = 8
        }

        private int lastPos = 0;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (onScroll == null)
                return;

            switch (m.Msg)
            {
                case WM_VSCROLL:
                    ScrollEventArgs sargs = new ScrollEventArgs(ScrollEventType.EndScroll, lastPos, GetScrollPos(this.Handle, SB_VERT));
                    onScroll(this, sargs);
                    break;
                

                case WM_MOUSEWHEEL:
                    ScrollEventArgs sarg = new ScrollEventArgs(ScrollEventType.EndScroll, lastPos, GetScrollPos(this.Handle, SB_VERT));
                    onScroll(this, sarg);
                    break;

                case WM_KEYDOWN:
                    switch (m.WParam.ToInt32())
                    {
                        case (int)Keys.Down:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.SmallDecrement, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                        case (int)Keys.Up:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.SmallIncrement, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                        case (int)Keys.PageDown:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.LargeDecrement, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                        case (int)Keys.PageUp:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.LargeIncrement, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                        case (int)Keys.Home:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.First, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                        case (int)Keys.End:
                            onScroll(this, new ScrollEventArgs(ScrollEventType.Last, lastPos, GetScrollPos(this.Handle, SB_VERT)));
                            break;
                    }
                    break;
                case WM_PAINT:
                    if (this.View == View.Details && this.Columns.Count > 0)
                        this.Columns[this.Columns.Count - 1].Width = -2;
                    break;
            }

        }
        public void SetExtendedStyle(ListViewExtendedStyles exStyle)
        {
            ListViewExtendedStyles styles;
            styles = (ListViewExtendedStyles)SendMessage(this.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            styles |= exStyle;
            SendMessage(this.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }

        public void EnableDoubleBuffer()
        {
            ListViewExtendedStyles styles;
            // read current style
            styles = (ListViewExtendedStyles)SendMessage(this.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            // enable double buffer and border select
            styles |= ListViewExtendedStyles.DoubleBuffer | ListViewExtendedStyles.BorderSelect;
            
            // write new style
            SendMessage(this.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }
        public void DisableDoubleBuffer()
        {
            ListViewExtendedStyles styles;
            // read current style
            styles = (ListViewExtendedStyles)SendMessage(this.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            // disable double buffer and border select
            styles -= styles & ListViewExtendedStyles.DoubleBuffer;
            styles -= styles & ListViewExtendedStyles.BorderSelect;
            // write new style
            SendMessage(this.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }

        public int ScrollPosition
        {
            get
            {
                return GetScrollPos(this.Handle, SB_VERT);
            }
            set
            {
                int prevPos;
                int scrollVal = 0;

                if (ShowGroups == true)
                {
                    prevPos = GetScrollPos(this.Handle, SB_VERT);
                    scrollVal = -(prevPos - value);
                }
                else
                {
                    // TODO: Add setScrollPosition if ShowGroups == false
                }

                SendMessage(this.Handle, LVM_SCROLL, (IntPtr)0, (IntPtr)scrollVal);
            }
        }

        public event ScrollEventHandler onScroll;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetScrollInfo(IntPtr hwnd, int fnBar, ref SCROLLINFO lpsi);

        [DllImport("user32.dll")]
        public static extern int SendMessage(
                int hWnd,      // handle to destination window
                uint Msg,       // message
                long wParam,  // first message parameter
                long lParam   // second message parameter
                );

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int wMsg,
                                        int wParam, int lParam);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, uint wMsg,
                                        IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetScrollPos(IntPtr hWnd, int nBar);


        [StructLayout(LayoutKind.Sequential)]
        struct SCROLLINFO
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }
    }

}
