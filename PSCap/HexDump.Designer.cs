namespace PSCap
{
    partial class HexDump
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.hexLineNumbers = new PSCap.SyncBox();
            this.hexCharDisplay = new PSCap.SyncBox();
            this.hexDisplay = new PSCap.SyncBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.tableLayoutPanel1.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableLayoutPanel1.Controls.Add(this.hexLineNumbers, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.hexCharDisplay, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.hexDisplay, 1, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(1, 1);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(736, 231);
            this.tableLayoutPanel1.TabIndex = 10;
            // 
            // hexLineNumbers
            // 
            this.hexLineNumbers.BackColor = System.Drawing.SystemColors.Control;
            this.hexLineNumbers.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.hexLineNumbers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexLineNumbers.Enabled = false;
            this.hexLineNumbers.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexLineNumbers.Location = new System.Drawing.Point(1, 4);
            this.hexLineNumbers.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.hexLineNumbers.Multiline = true;
            this.hexLineNumbers.Name = "hexLineNumbers";
            this.hexLineNumbers.ReadOnly = true;
            this.hexLineNumbers.Size = new System.Drawing.Size(75, 223);
            this.hexLineNumbers.TabIndex = 6;
            this.hexLineNumbers.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.hexLineNumbers.WordWrap = false;
            // 
            // hexCharDisplay
            // 
            this.hexCharDisplay.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.hexCharDisplay.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.hexCharDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexCharDisplay.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexCharDisplay.Location = new System.Drawing.Point(508, 4);
            this.hexCharDisplay.Multiline = true;
            this.hexCharDisplay.Name = "hexCharDisplay";
            this.hexCharDisplay.ReadOnly = true;
            this.hexCharDisplay.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.hexCharDisplay.Size = new System.Drawing.Size(224, 223);
            this.hexCharDisplay.TabIndex = 7;
            this.hexCharDisplay.WordWrap = false;
            // 
            // hexDisplay
            // 
            this.hexDisplay.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.hexDisplay.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.hexDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hexDisplay.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hexDisplay.Location = new System.Drawing.Point(80, 4);
            this.hexDisplay.Multiline = true;
            this.hexDisplay.Name = "hexDisplay";
            this.hexDisplay.ReadOnly = true;
            this.hexDisplay.Size = new System.Drawing.Size(421, 223);
            this.hexDisplay.TabIndex = 8;
            this.hexDisplay.WordWrap = false;
            // 
            // HexDump
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "HexDump";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Size = new System.Drawing.Size(738, 233);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private SyncBox hexLineNumbers;
        private SyncBox hexDisplay;
        private SyncBox hexCharDisplay;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}
