using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Globalization;

namespace PSCap
{
    public partial class HexDump : UserControl
    {
        private List<byte> _bytes;
        public List<byte> Bytes
        {
            get
            {
                return _bytes;
            }

            set
            {
                SetBytes(value);
            }
        }

        public HexDump()
        {
            InitializeComponent();

            this.hexCharDisplay.SyncWith(this.hexDisplay);
            this.hexCharDisplay.SyncWith(this.hexLineNumbers);

            this.hexDisplay.SyncWith(this.hexCharDisplay);
            this.hexDisplay.SyncWith(this.hexLineNumbers);

            this.hexLineNumbers.SyncWith(this.hexDisplay);
            this.hexLineNumbers.SyncWith(this.hexCharDisplay);

            foreach (Control c in this.hexLineNumbers.Controls)
            {
                Console.WriteLine(c.GetType().ToString());
            }
        }

        /// <summary>
        ///     Parse and display hexadecimal strings and character strings from the byte data.
        /// </summary>
        /// <param name="array">
        ///     The byte array.
        /// </param>
        /// <returns>
        ///     The hexadecimal data as a string
        /// </returns>
        public void SetBytes(List<byte> array)
        {
            _bytes = array;

            if(array == null)
            {
                this.hexLineNumbers.Text = "";
                this.hexDisplay.Text = "";
                this.hexCharDisplay.Text = "";

                return;
            }

            // "lineNumbers" keeps track of the number of lines in the "formatted" and "converted" data.
            // "normal" string is a simple spaced display of each byte turned into hex.
            // "formatted" string is the display form of the "normal" string.  It's mostly the "normal" string.
            // "converted" string is what the byte array looks like when converted to char data.
            StringBuilder lineNumbers = new StringBuilder();
            StringBuilder formatted = new StringBuilder();
            StringBuilder converted = new StringBuilder();
            int lineNo = 0;

            // Iterate over data bytes
            string newLine = System.Environment.NewLine;
            for (int entry = 0, byteIndex = 0, byteLength = array.Count; byteIndex < byteLength; byteIndex++)
            {
                byte b = array[byteIndex];
                string decoded = string.Format("{0:X2} ", b);

                formatted.Append(decoded);
                // Lifted directly from ByteViewer.DrawDump (source).
                char c = Convert.ToChar(b);
                if (CharIsPrintable(c))
                    converted.Append(c);
                else
                    converted.Append(".");

                entry++;
                // Once we have displayed sixteen values, start a new line.
                if (entry == 16)
                {
                    formatted.Append(newLine);
                    converted.Append(newLine);
                    lineNumbers.Append((lineNo.ToString().PadLeft(8, '0')) + newLine);
                    lineNo += 10;
                    entry = 0;
                }
                // An additional space between the first eight values and the latter eight values per line.
                else if (entry == 8)
                    formatted.Append(" ");
                // This is the last line.
                else if (byteIndex + 1 == byteLength)
                    lineNumbers.Append((lineNo.ToString().PadLeft(8, '0')));

            }

            // Display resulting data in appropriate fields.
            this.hexLineNumbers.Text = lineNumbers.ToString();
            this.hexDisplay.Text = formatted.ToString();
            this.hexCharDisplay.Text = converted.ToString();
        }

        /// <summary>
        ///     Check if char data meets a set of criteria which defines that it can be properly displayed.
        /// </summary>
        /// <param name="c">
        ///     A character to be tested.
        /// </param>
        /// <returns>
        ///     True, if the character can be displayed; false, otherwise.
        /// </returns>
        private static bool CharIsPrintable(char c)
        {
            return c >= ' ' && c <= '~';
        }
    }
}
