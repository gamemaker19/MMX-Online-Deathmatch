
namespace BanTool
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.reportFileChooser = new System.Windows.Forms.Button();
            this.labelReportFile = new System.Windows.Forms.Label();
            this.labelBanStatus = new System.Windows.Forms.Label();
            this.banButton = new System.Windows.Forms.Button();
            this.unbanButton = new System.Windows.Forms.Button();
            this.labelSuccess = new System.Windows.Forms.Label();
            this.labelFail = new System.Windows.Forms.Label();
            this.textBoxBanReason = new System.Windows.Forms.TextBox();
            this.labelBanReason = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.banTypeComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.banLengthComboBox = new System.Windows.Forms.ComboBox();
            this.banGroupBox = new System.Windows.Forms.GroupBox();
            this.banStatusGroupBox = new System.Windows.Forms.GroupBox();
            this.labelBanStatusReason = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.notBannedGroupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.regionComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.removeMatchBtn = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label5 = new System.Windows.Forms.Label();
            this.banGroupBox.SuspendLayout();
            this.banStatusGroupBox.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.notBannedGroupBox.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // reportFileChooser
            // 
            this.reportFileChooser.Location = new System.Drawing.Point(40, 22);
            this.reportFileChooser.Name = "reportFileChooser";
            this.reportFileChooser.Size = new System.Drawing.Size(222, 48);
            this.reportFileChooser.TabIndex = 1;
            this.reportFileChooser.Text = "Choose Report File...";
            this.reportFileChooser.UseVisualStyleBackColor = true;
            this.reportFileChooser.Click += new System.EventHandler(this.reportFileChooser_Click);
            // 
            // labelReportFile
            // 
            this.labelReportFile.AutoSize = true;
            this.labelReportFile.Location = new System.Drawing.Point(43, 73);
            this.labelReportFile.Name = "labelReportFile";
            this.labelReportFile.Size = new System.Drawing.Size(219, 25);
            this.labelReportFile.TabIndex = 2;
            this.labelReportFile.Text = "Report File: report_blah.txt";
            this.labelReportFile.Visible = false;
            // 
            // labelBanStatus
            // 
            this.labelBanStatus.AutoSize = true;
            this.labelBanStatus.Location = new System.Drawing.Point(15, 36);
            this.labelBanStatus.Name = "labelBanStatus";
            this.labelBanStatus.Size = new System.Drawing.Size(170, 25);
            this.labelBanStatus.TabIndex = 3;
            this.labelBanStatus.Text = "BANNED (indefinite)";
            // 
            // banButton
            // 
            this.banButton.Location = new System.Drawing.Point(17, 159);
            this.banButton.Name = "banButton";
            this.banButton.Size = new System.Drawing.Size(112, 34);
            this.banButton.TabIndex = 4;
            this.banButton.Text = "Submit Ban";
            this.banButton.UseVisualStyleBackColor = true;
            this.banButton.Click += new System.EventHandler(this.banButton_Click);
            // 
            // unbanButton
            // 
            this.unbanButton.Location = new System.Drawing.Point(15, 110);
            this.unbanButton.Name = "unbanButton";
            this.unbanButton.Size = new System.Drawing.Size(112, 34);
            this.unbanButton.TabIndex = 5;
            this.unbanButton.Text = "Unban";
            this.unbanButton.UseVisualStyleBackColor = true;
            this.unbanButton.Click += new System.EventHandler(this.unbanButton_Click);
            // 
            // labelSuccess
            // 
            this.labelSuccess.AutoSize = true;
            this.labelSuccess.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.labelSuccess.Location = new System.Drawing.Point(3, 463);
            this.labelSuccess.Name = "labelSuccess";
            this.labelSuccess.Size = new System.Drawing.Size(147, 25);
            this.labelSuccess.TabIndex = 6;
            this.labelSuccess.Text = "Action successful";
            this.labelSuccess.Visible = false;
            // 
            // labelFail
            // 
            this.labelFail.AutoSize = true;
            this.labelFail.ForeColor = System.Drawing.Color.Red;
            this.labelFail.Location = new System.Drawing.Point(156, 463);
            this.labelFail.MaximumSize = new System.Drawing.Size(600, 300);
            this.labelFail.Name = "labelFail";
            this.labelFail.Size = new System.Drawing.Size(167, 25);
            this.labelFail.TabIndex = 7;
            this.labelFail.Text = "Action failed. Error: ";
            this.labelFail.Visible = false;
            // 
            // textBoxBanReason
            // 
            this.textBoxBanReason.Location = new System.Drawing.Point(125, 112);
            this.textBoxBanReason.MaxLength = 50;
            this.textBoxBanReason.Name = "textBoxBanReason";
            this.textBoxBanReason.Size = new System.Drawing.Size(289, 31);
            this.textBoxBanReason.TabIndex = 8;
            // 
            // labelBanReason
            // 
            this.labelBanReason.AutoSize = true;
            this.labelBanReason.Location = new System.Drawing.Point(16, 112);
            this.labelBanReason.Name = "labelBanReason";
            this.labelBanReason.Size = new System.Drawing.Size(103, 25);
            this.labelBanReason.TabIndex = 9;
            this.labelBanReason.Text = "Ban Reason";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 36);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 25);
            this.label1.TabIndex = 11;
            this.label1.Text = "Ban Type";
            // 
            // banTypeComboBox
            // 
            this.banTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.banTypeComboBox.FormattingEnabled = true;
            this.banTypeComboBox.Items.AddRange(new object[] {
            "Ban",
            "Chat/Vote Ban",
            "Warning"});
            this.banTypeComboBox.Location = new System.Drawing.Point(125, 33);
            this.banTypeComboBox.Name = "banTypeComboBox";
            this.banTypeComboBox.Size = new System.Drawing.Size(182, 33);
            this.banTypeComboBox.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(17, 73);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 25);
            this.label2.TabIndex = 13;
            this.label2.Text = "Ban Length";
            // 
            // banLengthComboBox
            // 
            this.banLengthComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.banLengthComboBox.FormattingEnabled = true;
            this.banLengthComboBox.Items.AddRange(new object[] {
            "Indefinite",
            "1 day",
            "3 days",
            "1 week",
            "2 weeks",
            "1 month"});
            this.banLengthComboBox.Location = new System.Drawing.Point(125, 73);
            this.banLengthComboBox.Name = "banLengthComboBox";
            this.banLengthComboBox.Size = new System.Drawing.Size(182, 33);
            this.banLengthComboBox.TabIndex = 14;
            // 
            // banGroupBox
            // 
            this.banGroupBox.Controls.Add(this.labelBanReason);
            this.banGroupBox.Controls.Add(this.banLengthComboBox);
            this.banGroupBox.Controls.Add(this.textBoxBanReason);
            this.banGroupBox.Controls.Add(this.label2);
            this.banGroupBox.Controls.Add(this.label1);
            this.banGroupBox.Controls.Add(this.banButton);
            this.banGroupBox.Controls.Add(this.banTypeComboBox);
            this.banGroupBox.Location = new System.Drawing.Point(3, 254);
            this.banGroupBox.Name = "banGroupBox";
            this.banGroupBox.Size = new System.Drawing.Size(861, 206);
            this.banGroupBox.TabIndex = 15;
            this.banGroupBox.TabStop = false;
            this.banGroupBox.Text = "Ban Player";
            this.banGroupBox.Visible = false;
            // 
            // banStatusGroupBox
            // 
            this.banStatusGroupBox.Controls.Add(this.labelBanStatusReason);
            this.banStatusGroupBox.Controls.Add(this.unbanButton);
            this.banStatusGroupBox.Controls.Add(this.labelBanStatus);
            this.banStatusGroupBox.Location = new System.Drawing.Point(3, 84);
            this.banStatusGroupBox.Name = "banStatusGroupBox";
            this.banStatusGroupBox.Size = new System.Drawing.Size(861, 164);
            this.banStatusGroupBox.TabIndex = 16;
            this.banStatusGroupBox.TabStop = false;
            this.banStatusGroupBox.Text = "Ban Status";
            this.banStatusGroupBox.Visible = false;
            this.banStatusGroupBox.Enter += new System.EventHandler(this.banStatusGroupBox_Enter);
            // 
            // labelBanStatusReason
            // 
            this.labelBanStatusReason.AutoSize = true;
            this.labelBanStatusReason.Location = new System.Drawing.Point(17, 73);
            this.labelBanStatusReason.Name = "labelBanStatusReason";
            this.labelBanStatusReason.Size = new System.Drawing.Size(78, 25);
            this.labelBanStatusReason.TabIndex = 6;
            this.labelBanStatusReason.Text = "Reason: ";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.notBannedGroupBox);
            this.flowLayoutPanel1.Controls.Add(this.banStatusGroupBox);
            this.flowLayoutPanel1.Controls.Add(this.banGroupBox);
            this.flowLayoutPanel1.Controls.Add(this.labelSuccess);
            this.flowLayoutPanel1.Controls.Add(this.labelFail);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(43, 113);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(880, 511);
            this.flowLayoutPanel1.TabIndex = 17;
            // 
            // notBannedGroupBox
            // 
            this.notBannedGroupBox.Controls.Add(this.label4);
            this.notBannedGroupBox.Location = new System.Drawing.Point(3, 3);
            this.notBannedGroupBox.Name = "notBannedGroupBox";
            this.notBannedGroupBox.Size = new System.Drawing.Size(861, 75);
            this.notBannedGroupBox.TabIndex = 17;
            this.notBannedGroupBox.TabStop = false;
            this.notBannedGroupBox.Text = "Ban Status";
            this.notBannedGroupBox.Visible = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 36);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(122, 25);
            this.label4.TabIndex = 3;
            this.label4.Text = "NOT BANNED";
            // 
            // regionComboBox
            // 
            this.regionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.regionComboBox.FormattingEnabled = true;
            this.regionComboBox.Items.AddRange(new object[] {
            "All",
            "East US",
            "West US",
            "Brazil"});
            this.regionComboBox.Location = new System.Drawing.Point(374, 31);
            this.regionComboBox.Name = "regionComboBox";
            this.regionComboBox.Size = new System.Drawing.Size(182, 33);
            this.regionComboBox.TabIndex = 15;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(288, 34);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 25);
            this.label3.TabIndex = 7;
            this.label3.Text = "Region:";
            // 
            // removeMatchBtn
            // 
            this.removeMatchBtn.Location = new System.Drawing.Point(3, 28);
            this.removeMatchBtn.Name = "removeMatchBtn";
            this.removeMatchBtn.Size = new System.Drawing.Size(183, 36);
            this.removeMatchBtn.TabIndex = 18;
            this.removeMatchBtn.Text = "Remove All Matches";
            this.removeMatchBtn.UseVisualStyleBackColor = true;
            this.removeMatchBtn.Click += new System.EventHandler(this.removeMatchBtn_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.removeMatchBtn);
            this.panel1.Controls.Add(this.label5);
            this.panel1.Location = new System.Drawing.Point(621, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(302, 73);
            this.panel1.TabIndex = 19;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(113, 25);
            this.label5.TabIndex = 0;
            this.label5.Text = "Special Tools";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(935, 651);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.regionComboBox);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.labelReportFile);
            this.Controls.Add(this.reportFileChooser);
            this.Name = "Form1";
            this.Text = "MMX Ban Tool";
            this.banGroupBox.ResumeLayout(false);
            this.banGroupBox.PerformLayout();
            this.banStatusGroupBox.ResumeLayout(false);
            this.banStatusGroupBox.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.notBannedGroupBox.ResumeLayout(false);
            this.notBannedGroupBox.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button reportFileChooser;
        private System.Windows.Forms.Label labelReportFile;
        private System.Windows.Forms.Label labelBanStatus;
        private System.Windows.Forms.Button banButton;
        private System.Windows.Forms.Button unbanButton;
        private System.Windows.Forms.Label labelSuccess;
        private System.Windows.Forms.Label labelFail;
        private System.Windows.Forms.TextBox textBoxBanReason;
        private System.Windows.Forms.Label labelBanReason;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox banTypeComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox banLengthComboBox;
        private System.Windows.Forms.GroupBox banGroupBox;
        private System.Windows.Forms.GroupBox banStatusGroupBox;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label labelBanStatusReason;
        private System.Windows.Forms.GroupBox notBannedGroupBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox regionComboBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button removeMatchBtn;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
    }
}

