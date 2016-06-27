using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSCap
{
    public partial class HotkeysDialog : Form
    {
        public HotkeysDialog()
        {
            InitializeComponent();

            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }
    }
}
