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
    partial class EditMetadata : Form
    {
        bool descriptionPlaceHolder = false;
        private string placeholder = "Please enter a description of what was captured.\n" +
            "The more detail you provide about what happened during the capture and what you did, the easier " +
            "the capture file is to process...";

        public string DescriptionResult = "";
        public string CaptureNameResult = "";

        public EditMetadata(CaptureFile capFile)
        {
            InitializeComponent();

            this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            captureDescriptionField.GotFocus += new EventHandler(RemoveText);
            captureDescriptionField.LostFocus += new EventHandler(AddText);

            captureNameField.Text = capFile.getCaptureName();
            captureDescriptionField.Text = capFile.getCaptureDescription();

            if(string.Empty == captureDescriptionField.Text)
            {
                captureDescriptionField.Lines = placeholder.Split('\n');
                captureDescriptionField.ForeColor = Color.Gray;
                descriptionPlaceHolder = true;
            }
        }

        public void RemoveText(object sender, EventArgs e)
        {
            if(descriptionPlaceHolder)
            {
                descriptionPlaceHolder = false;
                captureDescriptionField.ForeColor = Color.Black;
                captureDescriptionField.Text = "";
            }
        }

        public void AddText(object sender, EventArgs e)
        {
            if (captureDescriptionField.Text == "")
            {
                captureDescriptionField.Lines = placeholder.Split('\n');
                captureDescriptionField.ForeColor = Color.Gray;
                descriptionPlaceHolder = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (descriptionPlaceHolder)
                captureDescriptionField.Text = "";

            DescriptionResult = captureDescriptionField.Text;
            CaptureNameResult = captureNameField.Text;

            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
