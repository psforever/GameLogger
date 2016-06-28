namespace PSCap
{
    partial class Preferences
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.AbsoluteTimeStampCbx = new System.Windows.Forms.CheckBox();
            this.CancelButton = new System.Windows.Forms.Button();
            this.OkButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // AbsoluteTimeStampCbx
            // 
            this.AbsoluteTimeStampCbx.AutoSize = true;
            this.AbsoluteTimeStampCbx.Location = new System.Drawing.Point(12, 12);
            this.AbsoluteTimeStampCbx.Name = "AbsoluteTimeStampCbx";
            this.AbsoluteTimeStampCbx.Size = new System.Drawing.Size(121, 17);
            this.AbsoluteTimeStampCbx.TabIndex = 0;
            this.AbsoluteTimeStampCbx.Text = "Absolute Timestamp";
            this.AbsoluteTimeStampCbx.UseVisualStyleBackColor = true;
            this.AbsoluteTimeStampCbx.CheckedChanged += new System.EventHandler(this.AbsoluteTimeStampCbx_CheckedChanged);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(139, 153);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 7;
            this.CancelButton.Text = "&Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // OkButton
            // 
            this.OkButton.Location = new System.Drawing.Point(41, 153);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(75, 23);
            this.OkButton.TabIndex = 6;
            this.OkButton.Text = "&OK";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // Preferences
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(254, 191);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.AbsoluteTimeStampCbx);
            this.Name = "Preferences";
            this.Text = "Preferences";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox AbsoluteTimeStampCbx;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Button OkButton;
    }
}