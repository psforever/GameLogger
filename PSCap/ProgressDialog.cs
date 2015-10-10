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
    public partial class ProgressDialog : Form
    {
        private string template = "";

        public ProgressDialog(string title)
        {
            InitializeComponent();
            Text = title;
            label1.Text = "";
        }

        /*protected override void OnFormClosing(FormClosingEventArgs e)
        {
            switch (e.CloseReason)
            {
                case CloseReason.UserClosing:
                    e.Cancel = true;
                    break;
            }
        }*/

        public void Done()
        {
            this.Close();
        }

        public void ProgressTemplate(string template)
        {
            this.template = template;
        }

        public void ProgressParams(int steps, int stride)
        {
            progressBar1.Maximum = steps;
            progressBar1.Step = stride;
            progressBar1.Value = 0;
            label1.Text = "";
        }

        public void Step()
        {
            if (string.Empty == template)
                template = "Loading ... {percent}%";

            Step(template);
        }

        public void Step(string template)
        {
            progressBar1.PerformStep();

            double percent = (double)progressBar1.Value / progressBar1.Maximum * 100;
            template = template.Replace("{percent}", ((int)Math.Round(percent)).ToString());
            template = template.Replace("{value}", progressBar1.Value.ToString());
            template = template.Replace("{max}", progressBar1.Maximum.ToString());

            label1.Text = template;
        }
    }
}
