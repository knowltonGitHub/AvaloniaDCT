namespace DCT
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
            this.tabControlPreferences = new System.Windows.Forms.TabControl();
            this.tabPageEmailPreferences = new System.Windows.Forms.TabPage();
            this.buttonSavePreferences = new System.Windows.Forms.Button();
            this.textBoxHost = new System.Windows.Forms.TextBox();
            this.label1EmailHost = new System.Windows.Forms.Label();
            this.textBoxUserName = new System.Windows.Forms.TextBox();
            this.labelUserName = new System.Windows.Forms.Label();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.label1Password = new System.Windows.Forms.Label();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.textBoxFromAddress = new System.Windows.Forms.TextBox();
            this.labelFromAddress = new System.Windows.Forms.Label();
            this.labelExplainEmailSettings = new System.Windows.Forms.Label();
            this.tabPageLANSettings = new System.Windows.Forms.TabPage();
            this.groupBoxDestination = new System.Windows.Forms.GroupBox();
            this.textBoxDestinationIP = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxDestinationPort = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBoxSource = new System.Windows.Forms.GroupBox();
            this.textBoxSourceIP = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxSourcePort = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabControlPreferences.SuspendLayout();
            this.tabPageEmailPreferences.SuspendLayout();
            this.tabPageLANSettings.SuspendLayout();
            this.groupBoxDestination.SuspendLayout();
            this.groupBoxSource.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlPreferences
            // 
            this.tabControlPreferences.Controls.Add(this.tabPageEmailPreferences);
            this.tabControlPreferences.Controls.Add(this.tabPageLANSettings);
            this.tabControlPreferences.Location = new System.Drawing.Point(0, 0);
            this.tabControlPreferences.Name = "tabControlPreferences";
            this.tabControlPreferences.SelectedIndex = 0;
            this.tabControlPreferences.Size = new System.Drawing.Size(647, 211);
            this.tabControlPreferences.TabIndex = 0;
            // 
            // tabPageEmailPreferences
            // 
            this.tabPageEmailPreferences.Controls.Add(this.textBoxHost);
            this.tabPageEmailPreferences.Controls.Add(this.label1EmailHost);
            this.tabPageEmailPreferences.Controls.Add(this.textBoxUserName);
            this.tabPageEmailPreferences.Controls.Add(this.labelUserName);
            this.tabPageEmailPreferences.Controls.Add(this.textBoxPassword);
            this.tabPageEmailPreferences.Controls.Add(this.label1Password);
            this.tabPageEmailPreferences.Controls.Add(this.textBoxPort);
            this.tabPageEmailPreferences.Controls.Add(this.labelPort);
            this.tabPageEmailPreferences.Controls.Add(this.textBoxFromAddress);
            this.tabPageEmailPreferences.Controls.Add(this.labelFromAddress);
            this.tabPageEmailPreferences.Controls.Add(this.labelExplainEmailSettings);
            this.tabPageEmailPreferences.Location = new System.Drawing.Point(4, 22);
            this.tabPageEmailPreferences.Name = "tabPageEmailPreferences";
            this.tabPageEmailPreferences.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageEmailPreferences.Size = new System.Drawing.Size(639, 185);
            this.tabPageEmailPreferences.TabIndex = 0;
            this.tabPageEmailPreferences.Text = "Email";
            this.tabPageEmailPreferences.UseVisualStyleBackColor = true;
            // 
            // buttonSavePreferences
            // 
            this.buttonSavePreferences.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSavePreferences.Location = new System.Drawing.Point(560, 217);
            this.buttonSavePreferences.Name = "buttonSavePreferences";
            this.buttonSavePreferences.Size = new System.Drawing.Size(75, 23);
            this.buttonSavePreferences.TabIndex = 26;
            this.buttonSavePreferences.Text = "Save";
            this.buttonSavePreferences.UseVisualStyleBackColor = true;
            this.buttonSavePreferences.Click += new System.EventHandler(this.buttonSavePreferences_Click);
            // 
            // textBoxHost
            // 
            this.textBoxHost.Location = new System.Drawing.Point(102, 80);
            this.textBoxHost.Name = "textBoxHost";
            this.textBoxHost.Size = new System.Drawing.Size(160, 20);
            this.textBoxHost.TabIndex = 24;
            // 
            // label1EmailHost
            // 
            this.label1EmailHost.AutoSize = true;
            this.label1EmailHost.Location = new System.Drawing.Point(67, 81);
            this.label1EmailHost.Name = "label1EmailHost";
            this.label1EmailHost.Size = new System.Drawing.Size(29, 13);
            this.label1EmailHost.TabIndex = 23;
            this.label1EmailHost.Text = "Host";
            // 
            // textBoxUserName
            // 
            this.textBoxUserName.Location = new System.Drawing.Point(102, 130);
            this.textBoxUserName.Name = "textBoxUserName";
            this.textBoxUserName.Size = new System.Drawing.Size(160, 20);
            this.textBoxUserName.TabIndex = 22;
            // 
            // labelUserName
            // 
            this.labelUserName.AutoSize = true;
            this.labelUserName.Location = new System.Drawing.Point(41, 133);
            this.labelUserName.Name = "labelUserName";
            this.labelUserName.Size = new System.Drawing.Size(55, 13);
            this.labelUserName.TabIndex = 21;
            this.labelUserName.Text = "UserName";
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Location = new System.Drawing.Point(102, 155);
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.Size = new System.Drawing.Size(160, 20);
            this.textBoxPassword.TabIndex = 20;
            // 
            // label1Password
            // 
            this.label1Password.AutoSize = true;
            this.label1Password.Location = new System.Drawing.Point(43, 159);
            this.label1Password.Name = "label1Password";
            this.label1Password.Size = new System.Drawing.Size(53, 13);
            this.label1Password.TabIndex = 19;
            this.label1Password.Text = "Password";
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(102, 105);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(160, 20);
            this.textBoxPort.TabIndex = 18;
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Location = new System.Drawing.Point(70, 107);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(26, 13);
            this.labelPort.TabIndex = 17;
            this.labelPort.Text = "Port";
            // 
            // textBoxFromAddress
            // 
            this.textBoxFromAddress.Location = new System.Drawing.Point(102, 55);
            this.textBoxFromAddress.Name = "textBoxFromAddress";
            this.textBoxFromAddress.Size = new System.Drawing.Size(160, 20);
            this.textBoxFromAddress.TabIndex = 2;
            // 
            // labelFromAddress
            // 
            this.labelFromAddress.AutoSize = true;
            this.labelFromAddress.Location = new System.Drawing.Point(25, 55);
            this.labelFromAddress.Name = "labelFromAddress";
            this.labelFromAddress.Size = new System.Drawing.Size(71, 13);
            this.labelFromAddress.TabIndex = 1;
            this.labelFromAddress.Text = "From Address";
            // 
            // labelExplainEmailSettings
            // 
            this.labelExplainEmailSettings.AutoSize = true;
            this.labelExplainEmailSettings.Location = new System.Drawing.Point(23, 25);
            this.labelExplainEmailSettings.Name = "labelExplainEmailSettings";
            this.labelExplainEmailSettings.Size = new System.Drawing.Size(609, 13);
            this.labelExplainEmailSettings.TabIndex = 0;
            this.labelExplainEmailSettings.Text = "Enter your online web settings for G Mail.  As of the date of this application\'s " +
    "publication, the settings can be located by following";
            // 
            // tabPageLANSettings
            // 
            this.tabPageLANSettings.Controls.Add(this.groupBoxDestination);
            this.tabPageLANSettings.Controls.Add(this.groupBoxSource);
            this.tabPageLANSettings.Location = new System.Drawing.Point(4, 22);
            this.tabPageLANSettings.Name = "tabPageLANSettings";
            this.tabPageLANSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageLANSettings.Size = new System.Drawing.Size(639, 185);
            this.tabPageLANSettings.TabIndex = 1;
            this.tabPageLANSettings.Text = "Home Network Settings";
            this.tabPageLANSettings.UseVisualStyleBackColor = true;
            // 
            // groupBoxDestination
            // 
            this.groupBoxDestination.Controls.Add(this.textBoxDestinationIP);
            this.groupBoxDestination.Controls.Add(this.label3);
            this.groupBoxDestination.Controls.Add(this.textBoxDestinationPort);
            this.groupBoxDestination.Controls.Add(this.label4);
            this.groupBoxDestination.Location = new System.Drawing.Point(8, 89);
            this.groupBoxDestination.Name = "groupBoxDestination";
            this.groupBoxDestination.Size = new System.Drawing.Size(217, 77);
            this.groupBoxDestination.TabIndex = 34;
            this.groupBoxDestination.TabStop = false;
            this.groupBoxDestination.Text = "Destination";
            // 
            // textBoxDestinationIP
            // 
            this.textBoxDestinationIP.Location = new System.Drawing.Point(43, 19);
            this.textBoxDestinationIP.Name = "textBoxDestinationIP";
            this.textBoxDestinationIP.Size = new System.Drawing.Size(160, 20);
            this.textBoxDestinationIP.TabIndex = 28;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 46);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(26, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Port";
            // 
            // textBoxDestinationPort
            // 
            this.textBoxDestinationPort.Location = new System.Drawing.Point(43, 44);
            this.textBoxDestinationPort.Name = "textBoxDestinationPort";
            this.textBoxDestinationPort.Size = new System.Drawing.Size(160, 20);
            this.textBoxDestinationPort.TabIndex = 26;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 19);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(17, 13);
            this.label4.TabIndex = 27;
            this.label4.Text = "IP";
            // 
            // groupBoxSource
            // 
            this.groupBoxSource.Controls.Add(this.textBoxSourceIP);
            this.groupBoxSource.Controls.Add(this.label2);
            this.groupBoxSource.Controls.Add(this.textBoxSourcePort);
            this.groupBoxSource.Controls.Add(this.label1);
            this.groupBoxSource.Location = new System.Drawing.Point(8, 6);
            this.groupBoxSource.Name = "groupBoxSource";
            this.groupBoxSource.Size = new System.Drawing.Size(217, 77);
            this.groupBoxSource.TabIndex = 33;
            this.groupBoxSource.TabStop = false;
            this.groupBoxSource.Text = "Source (your PC)";
            // 
            // textBoxSourceIP
            // 
            this.textBoxSourceIP.Location = new System.Drawing.Point(43, 19);
            this.textBoxSourceIP.Name = "textBoxSourceIP";
            this.textBoxSourceIP.Size = new System.Drawing.Size(160, 20);
            this.textBoxSourceIP.TabIndex = 28;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 25;
            this.label2.Text = "Port";
            // 
            // textBoxSourcePort
            // 
            this.textBoxSourcePort.Location = new System.Drawing.Point(43, 44);
            this.textBoxSourcePort.Name = "textBoxSourcePort";
            this.textBoxSourcePort.Size = new System.Drawing.Size(160, 20);
            this.textBoxSourcePort.TabIndex = 26;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(17, 13);
            this.label1.TabIndex = 27;
            this.label1.Text = "IP";
            // 
            // Preferences
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(647, 246);
            this.Controls.Add(this.buttonSavePreferences);
            this.Controls.Add(this.tabControlPreferences);
            this.Name = "Preferences";
            this.Text = "Preferences";
            this.Load += new System.EventHandler(this.Preferences_Load);
            this.tabControlPreferences.ResumeLayout(false);
            this.tabPageEmailPreferences.ResumeLayout(false);
            this.tabPageEmailPreferences.PerformLayout();
            this.tabPageLANSettings.ResumeLayout(false);
            this.groupBoxDestination.ResumeLayout(false);
            this.groupBoxDestination.PerformLayout();
            this.groupBoxSource.ResumeLayout(false);
            this.groupBoxSource.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlPreferences;
        private System.Windows.Forms.TabPage tabPageEmailPreferences;
        private System.Windows.Forms.TabPage tabPageLANSettings;
        private System.Windows.Forms.Label labelExplainEmailSettings;
        private System.Windows.Forms.TextBox textBoxHost;
        private System.Windows.Forms.Label label1EmailHost;
        private System.Windows.Forms.TextBox textBoxUserName;
        private System.Windows.Forms.Label labelUserName;
        private System.Windows.Forms.TextBox textBoxPassword;
        private System.Windows.Forms.Label label1Password;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.TextBox textBoxFromAddress;
        private System.Windows.Forms.Label labelFromAddress;
        private System.Windows.Forms.Button buttonSavePreferences;
        private System.Windows.Forms.GroupBox groupBoxDestination;
        private System.Windows.Forms.TextBox textBoxDestinationIP;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxDestinationPort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBoxSource;
        private System.Windows.Forms.TextBox textBoxSourceIP;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxSourcePort;
        private System.Windows.Forms.Label label1;
    }
}