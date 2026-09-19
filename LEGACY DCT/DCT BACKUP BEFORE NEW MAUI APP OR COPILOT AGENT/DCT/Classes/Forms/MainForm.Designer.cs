namespace DCT
{
    partial class FormMain
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
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPageClipboard = new System.Windows.Forms.TabPage();
            this.panelEnterOrEditText = new System.Windows.Forms.Panel();
            this.textBoxNewItemValueCandidate = new System.Windows.Forms.TextBox();
            this.textBoxNewTagOrItem = new System.Windows.Forms.TextBox();
            this.buttonSaveText = new System.Windows.Forms.Button();
            this.buttonCancelText = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.groupBoxValue = new System.Windows.Forms.GroupBox();
            this.buttonExportFile = new System.Windows.Forms.Button();
            this.buttonOpenWithChrome = new System.Windows.Forms.Button();
            this.buttonEmail = new System.Windows.Forms.Button();
            this.textBoxItemValue = new System.Windows.Forms.TextBox();
            this.groupBoxItems = new System.Windows.Forms.GroupBox();
            this.buttonDeleteSelectedItem = new System.Windows.Forms.Button();
            this.buttonAddNewItem = new System.Windows.Forms.Button();
            this.listBoxItems = new System.Windows.Forms.ListBox();
            this.groupBoxTags = new System.Windows.Forms.GroupBox();
            this.buttonDeleteSelectedTag = new System.Windows.Forms.Button();
            this.buttonAddNewTag = new System.Windows.Forms.Button();
            this.listBoxTags = new System.Windows.Forms.ListBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.buttonServerMode = new System.Windows.Forms.Button();
            this.textBoxSendMessage = new System.Windows.Forms.TextBox();
            this.textBoxChatMessages = new System.Windows.Forms.TextBox();
            this.menuStripMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.preferencesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControlMain.SuspendLayout();
            this.tabPageClipboard.SuspendLayout();
            this.panelEnterOrEditText.SuspendLayout();
            this.groupBoxValue.SuspendLayout();
            this.groupBoxItems.SuspendLayout();
            this.groupBoxTags.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.menuStripMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPageClipboard);
            this.tabControlMain.Controls.Add(this.tabPage2);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 24);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1078, 449);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPageClipboard
            // 
            this.tabPageClipboard.Controls.Add(this.panelEnterOrEditText);
            this.tabPageClipboard.Controls.Add(this.button1);
            this.tabPageClipboard.Controls.Add(this.textBox1);
            this.tabPageClipboard.Controls.Add(this.groupBoxValue);
            this.tabPageClipboard.Controls.Add(this.groupBoxItems);
            this.tabPageClipboard.Controls.Add(this.groupBoxTags);
            this.tabPageClipboard.ForeColor = System.Drawing.SystemColors.ControlText;
            this.tabPageClipboard.Location = new System.Drawing.Point(4, 22);
            this.tabPageClipboard.Name = "tabPageClipboard";
            this.tabPageClipboard.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageClipboard.Size = new System.Drawing.Size(1070, 423);
            this.tabPageClipboard.TabIndex = 0;
            this.tabPageClipboard.Text = "Clipboard";
            this.tabPageClipboard.UseVisualStyleBackColor = true;
            // 
            // panelEnterOrEditText
            // 
            this.panelEnterOrEditText.BackColor = System.Drawing.Color.Black;
            this.panelEnterOrEditText.Controls.Add(this.textBoxNewItemValueCandidate);
            this.panelEnterOrEditText.Controls.Add(this.textBoxNewTagOrItem);
            this.panelEnterOrEditText.Controls.Add(this.buttonSaveText);
            this.panelEnterOrEditText.Controls.Add(this.buttonCancelText);
            this.panelEnterOrEditText.Location = new System.Drawing.Point(720, 333);
            this.panelEnterOrEditText.Name = "panelEnterOrEditText";
            this.panelEnterOrEditText.Size = new System.Drawing.Size(318, 83);
            this.panelEnterOrEditText.TabIndex = 8;
            this.panelEnterOrEditText.Visible = false;
            // 
            // textBoxNewItemValueCandidate
            // 
            this.textBoxNewItemValueCandidate.Location = new System.Drawing.Point(6, 33);
            this.textBoxNewItemValueCandidate.Multiline = true;
            this.textBoxNewItemValueCandidate.Name = "textBoxNewItemValueCandidate";
            this.textBoxNewItemValueCandidate.Size = new System.Drawing.Size(198, 43);
            this.textBoxNewItemValueCandidate.TabIndex = 8;
            // 
            // textBoxNewTagOrItem
            // 
            this.textBoxNewTagOrItem.Location = new System.Drawing.Point(6, 7);
            this.textBoxNewTagOrItem.Name = "textBoxNewTagOrItem";
            this.textBoxNewTagOrItem.Size = new System.Drawing.Size(198, 20);
            this.textBoxNewTagOrItem.TabIndex = 7;
            // 
            // buttonSaveText
            // 
            this.buttonSaveText.Location = new System.Drawing.Point(210, 5);
            this.buttonSaveText.Name = "buttonSaveText";
            this.buttonSaveText.Size = new System.Drawing.Size(45, 23);
            this.buttonSaveText.TabIndex = 5;
            this.buttonSaveText.Text = "Save";
            this.buttonSaveText.UseVisualStyleBackColor = true;
            this.buttonSaveText.Click += new System.EventHandler(this.buttonSaveText_Click);
            // 
            // buttonCancelText
            // 
            this.buttonCancelText.Location = new System.Drawing.Point(261, 5);
            this.buttonCancelText.Name = "buttonCancelText";
            this.buttonCancelText.Size = new System.Drawing.Size(50, 23);
            this.buttonCancelText.TabIndex = 6;
            this.buttonCancelText.Text = "Cancel";
            this.buttonCancelText.UseVisualStyleBackColor = true;
            this.buttonCancelText.Click += new System.EventHandler(this.buttonCancelText_Click);
            // 
            // button1
            // 
            this.button1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.button1.Location = new System.Drawing.Point(720, 211);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "clear";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(720, 28);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(305, 177);
            this.textBox1.TabIndex = 3;
            // 
            // groupBoxValue
            // 
            this.groupBoxValue.Controls.Add(this.buttonExportFile);
            this.groupBoxValue.Controls.Add(this.buttonOpenWithChrome);
            this.groupBoxValue.Controls.Add(this.buttonEmail);
            this.groupBoxValue.Controls.Add(this.textBoxItemValue);
            this.groupBoxValue.Location = new System.Drawing.Point(497, 6);
            this.groupBoxValue.Name = "groupBoxValue";
            this.groupBoxValue.Size = new System.Drawing.Size(205, 411);
            this.groupBoxValue.TabIndex = 2;
            this.groupBoxValue.TabStop = false;
            this.groupBoxValue.Text = "Value";
            // 
            // buttonExportFile
            // 
            this.buttonExportFile.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonExportFile.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonExportFile.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonExportFile.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonExportFile.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonExportFile.ForeColor = System.Drawing.Color.Transparent;
            this.buttonExportFile.Image = global::DCT.Properties.Resources.fileexport;
            this.buttonExportFile.Location = new System.Drawing.Point(84, 366);
            this.buttonExportFile.Name = "buttonExportFile";
            this.buttonExportFile.Size = new System.Drawing.Size(29, 42);
            this.buttonExportFile.TabIndex = 6;
            this.buttonExportFile.UseVisualStyleBackColor = false;
            this.buttonExportFile.Click += new System.EventHandler(this.buttonExportFile_Click);
            this.buttonExportFile.MouseEnter += new System.EventHandler(this.buttonExportFile_MouseEnter);
            this.buttonExportFile.MouseLeave += new System.EventHandler(this.buttonExportFile_MouseLeave);
            // 
            // buttonOpenWithChrome
            // 
            this.buttonOpenWithChrome.FlatAppearance.BorderSize = 0;
            this.buttonOpenWithChrome.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonOpenWithChrome.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonOpenWithChrome.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonOpenWithChrome.Image = global::DCT.Properties.Resources.chrome_icon;
            this.buttonOpenWithChrome.Location = new System.Drawing.Point(7, 370);
            this.buttonOpenWithChrome.Name = "buttonOpenWithChrome";
            this.buttonOpenWithChrome.Size = new System.Drawing.Size(31, 26);
            this.buttonOpenWithChrome.TabIndex = 5;
            this.buttonOpenWithChrome.UseVisualStyleBackColor = true;
            this.buttonOpenWithChrome.Click += new System.EventHandler(this.buttonOpenWithChrome_Click);
            this.buttonOpenWithChrome.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonOpenWithChrome.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // buttonEmail
            // 
            this.buttonEmail.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonEmail.FlatAppearance.BorderSize = 0;
            this.buttonEmail.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonEmail.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonEmail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonEmail.ForeColor = System.Drawing.Color.Transparent;
            this.buttonEmail.Image = global::DCT.Properties.Resources.envelope;
            this.buttonEmail.Location = new System.Drawing.Point(43, 367);
            this.buttonEmail.Name = "buttonEmail";
            this.buttonEmail.Size = new System.Drawing.Size(35, 31);
            this.buttonEmail.TabIndex = 4;
            this.buttonEmail.UseVisualStyleBackColor = false;
            this.buttonEmail.Click += new System.EventHandler(this.buttonEmail_Click);
            this.buttonEmail.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonEmail.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // textBoxItemValue
            // 
            this.textBoxItemValue.Location = new System.Drawing.Point(6, 23);
            this.textBoxItemValue.Multiline = true;
            this.textBoxItemValue.Name = "textBoxItemValue";
            this.textBoxItemValue.Size = new System.Drawing.Size(187, 342);
            this.textBoxItemValue.TabIndex = 1;
            this.textBoxItemValue.DoubleClick += new System.EventHandler(this.textBoxValue_DoubleClick);
            // 
            // groupBoxItems
            // 
            this.groupBoxItems.Controls.Add(this.buttonDeleteSelectedItem);
            this.groupBoxItems.Controls.Add(this.buttonAddNewItem);
            this.groupBoxItems.Controls.Add(this.listBoxItems);
            this.groupBoxItems.Location = new System.Drawing.Point(253, 6);
            this.groupBoxItems.Name = "groupBoxItems";
            this.groupBoxItems.Size = new System.Drawing.Size(238, 410);
            this.groupBoxItems.TabIndex = 1;
            this.groupBoxItems.TabStop = false;
            this.groupBoxItems.Text = "Items";
            // 
            // buttonDeleteSelectedItem
            // 
            this.buttonDeleteSelectedItem.FlatAppearance.BorderSize = 0;
            this.buttonDeleteSelectedItem.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonDeleteSelectedItem.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonDeleteSelectedItem.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonDeleteSelectedItem.Image = global::DCT.Properties.Resources.item_delete;
            this.buttonDeleteSelectedItem.Location = new System.Drawing.Point(200, 326);
            this.buttonDeleteSelectedItem.Name = "buttonDeleteSelectedItem";
            this.buttonDeleteSelectedItem.Size = new System.Drawing.Size(30, 37);
            this.buttonDeleteSelectedItem.TabIndex = 3;
            this.buttonDeleteSelectedItem.UseVisualStyleBackColor = true;
            this.buttonDeleteSelectedItem.Click += new System.EventHandler(this.buttonDeleteSelectedItem_Click);
            this.buttonDeleteSelectedItem.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonDeleteSelectedItem.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // buttonAddNewItem
            // 
            this.buttonAddNewItem.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonAddNewItem.FlatAppearance.BorderSize = 0;
            this.buttonAddNewItem.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonAddNewItem.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonAddNewItem.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAddNewItem.ForeColor = System.Drawing.Color.Transparent;
            this.buttonAddNewItem.Image = global::DCT.Properties.Resources.item_add;
            this.buttonAddNewItem.Location = new System.Drawing.Point(200, 267);
            this.buttonAddNewItem.Name = "buttonAddNewItem";
            this.buttonAddNewItem.Size = new System.Drawing.Size(30, 41);
            this.buttonAddNewItem.TabIndex = 2;
            this.buttonAddNewItem.UseVisualStyleBackColor = false;
            this.buttonAddNewItem.Click += new System.EventHandler(this.buttonAddNewItem_Click);
            this.buttonAddNewItem.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonAddNewItem.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // listBoxItems
            // 
            this.listBoxItems.FormattingEnabled = true;
            this.listBoxItems.Location = new System.Drawing.Point(7, 21);
            this.listBoxItems.Name = "listBoxItems";
            this.listBoxItems.Size = new System.Drawing.Size(187, 342);
            this.listBoxItems.TabIndex = 0;
            this.listBoxItems.SelectedIndexChanged += new System.EventHandler(this.listBoxItems_SelectedIndexChanged);
            this.listBoxItems.DoubleClick += new System.EventHandler(this.listBoxItems_DoubleClick);
            // 
            // groupBoxTags
            // 
            this.groupBoxTags.Controls.Add(this.buttonDeleteSelectedTag);
            this.groupBoxTags.Controls.Add(this.buttonAddNewTag);
            this.groupBoxTags.Controls.Add(this.listBoxTags);
            this.groupBoxTags.Location = new System.Drawing.Point(8, 6);
            this.groupBoxTags.Name = "groupBoxTags";
            this.groupBoxTags.Size = new System.Drawing.Size(237, 409);
            this.groupBoxTags.TabIndex = 0;
            this.groupBoxTags.TabStop = false;
            this.groupBoxTags.Text = "Tags";
            // 
            // buttonDeleteSelectedTag
            // 
            this.buttonDeleteSelectedTag.FlatAppearance.BorderSize = 0;
            this.buttonDeleteSelectedTag.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonDeleteSelectedTag.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonDeleteSelectedTag.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonDeleteSelectedTag.Image = global::DCT.Properties.Resources.item_delete;
            this.buttonDeleteSelectedTag.Location = new System.Drawing.Point(199, 328);
            this.buttonDeleteSelectedTag.Name = "buttonDeleteSelectedTag";
            this.buttonDeleteSelectedTag.Size = new System.Drawing.Size(30, 37);
            this.buttonDeleteSelectedTag.TabIndex = 3;
            this.buttonDeleteSelectedTag.UseVisualStyleBackColor = true;
            this.buttonDeleteSelectedTag.Click += new System.EventHandler(this.buttonDeleteSelectedTag_Click);
            this.buttonDeleteSelectedTag.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonDeleteSelectedTag.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // buttonAddNewTag
            // 
            this.buttonAddNewTag.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.buttonAddNewTag.FlatAppearance.BorderSize = 0;
            this.buttonAddNewTag.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.buttonAddNewTag.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.buttonAddNewTag.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAddNewTag.ForeColor = System.Drawing.Color.Transparent;
            this.buttonAddNewTag.Image = global::DCT.Properties.Resources.item_add;
            this.buttonAddNewTag.Location = new System.Drawing.Point(199, 267);
            this.buttonAddNewTag.Name = "buttonAddNewTag";
            this.buttonAddNewTag.Size = new System.Drawing.Size(30, 41);
            this.buttonAddNewTag.TabIndex = 2;
            this.buttonAddNewTag.UseVisualStyleBackColor = false;
            this.buttonAddNewTag.Click += new System.EventHandler(this.buttonAddNewTag_Click);
            this.buttonAddNewTag.MouseEnter += new System.EventHandler(this.ButtonMouseEnter);
            this.buttonAddNewTag.MouseLeave += new System.EventHandler(this.ButtonMouseLeave);
            // 
            // listBoxTags
            // 
            this.listBoxTags.FormattingEnabled = true;
            this.listBoxTags.Location = new System.Drawing.Point(6, 23);
            this.listBoxTags.Name = "listBoxTags";
            this.listBoxTags.Size = new System.Drawing.Size(187, 342);
            this.listBoxTags.TabIndex = 0;
            this.listBoxTags.SelectedIndexChanged += new System.EventHandler(this.listBoxTags_SelectedIndexChanged);
            this.listBoxTags.DoubleClick += new System.EventHandler(this.listBoxTags_DoubleClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.buttonServerMode);
            this.tabPage2.Controls.Add(this.textBoxSendMessage);
            this.tabPage2.Controls.Add(this.textBoxChatMessages);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1070, 423);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "tabPage2";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // buttonServerMode
            // 
            this.buttonServerMode.Location = new System.Drawing.Point(391, 199);
            this.buttonServerMode.Name = "buttonServerMode";
            this.buttonServerMode.Size = new System.Drawing.Size(75, 23);
            this.buttonServerMode.TabIndex = 2;
            this.buttonServerMode.Text = "RunServer";
            this.buttonServerMode.UseVisualStyleBackColor = true;
            this.buttonServerMode.Click += new System.EventHandler(this.buttonServerMode_Click);
            // 
            // textBoxSendMessage
            // 
            this.textBoxSendMessage.Location = new System.Drawing.Point(58, 236);
            this.textBoxSendMessage.Name = "textBoxSendMessage";
            this.textBoxSendMessage.Size = new System.Drawing.Size(253, 20);
            this.textBoxSendMessage.TabIndex = 1;
            this.textBoxSendMessage.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxSendMessage_KeyPress);
            // 
            // textBoxChatMessages
            // 
            this.textBoxChatMessages.Location = new System.Drawing.Point(58, 50);
            this.textBoxChatMessages.Multiline = true;
            this.textBoxChatMessages.Name = "textBoxChatMessages";
            this.textBoxChatMessages.Size = new System.Drawing.Size(253, 180);
            this.textBoxChatMessages.TabIndex = 0;
            // 
            // menuStripMain
            // 
            this.menuStripMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStripMain.Location = new System.Drawing.Point(0, 0);
            this.menuStripMain.Name = "menuStripMain";
            this.menuStripMain.Size = new System.Drawing.Size(1078, 24);
            this.menuStripMain.TabIndex = 1;
            this.menuStripMain.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.preferencesToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // preferencesToolStripMenuItem
            // 
            this.preferencesToolStripMenuItem.Name = "preferencesToolStripMenuItem";
            this.preferencesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.preferencesToolStripMenuItem.Text = "&Preferences";
            this.preferencesToolStripMenuItem.Click += new System.EventHandler(this.preferencesToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1078, 473);
            this.Controls.Add(this.tabControlMain);
            this.Controls.Add(this.menuStripMain);
            this.MainMenuStrip = this.menuStripMain;
            this.Name = "FormMain";
            this.Text = "Desktop Clipboard Toolkit";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.FormMain_FormClosed_1);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.SizeChanged += new System.EventHandler(this.FormMain_SizeChanged);
            this.tabControlMain.ResumeLayout(false);
            this.tabPageClipboard.ResumeLayout(false);
            this.tabPageClipboard.PerformLayout();
            this.panelEnterOrEditText.ResumeLayout(false);
            this.panelEnterOrEditText.PerformLayout();
            this.groupBoxValue.ResumeLayout(false);
            this.groupBoxValue.PerformLayout();
            this.groupBoxItems.ResumeLayout(false);
            this.groupBoxTags.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.menuStripMain.ResumeLayout(false);
            this.menuStripMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageClipboard;
        private System.Windows.Forms.GroupBox groupBoxTags;
        private System.Windows.Forms.ListBox listBoxTags;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button buttonAddNewTag;
        private System.Windows.Forms.Button buttonDeleteSelectedTag;
        private System.Windows.Forms.GroupBox groupBoxItems;
        private System.Windows.Forms.Button buttonDeleteSelectedItem;
        private System.Windows.Forms.Button buttonAddNewItem;
        private System.Windows.Forms.ListBox listBoxItems;
        private System.Windows.Forms.GroupBox groupBoxValue;
        private System.Windows.Forms.TextBox textBoxItemValue;
        private System.Windows.Forms.Button buttonOpenWithChrome;
        private System.Windows.Forms.Button buttonEmail;
        private System.Windows.Forms.MenuStrip menuStripMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem preferencesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.TextBox textBoxSendMessage;
        private System.Windows.Forms.TextBox textBoxChatMessages;
        private System.Windows.Forms.Button buttonServerMode;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Panel panelEnterOrEditText;
        private System.Windows.Forms.TextBox textBoxNewTagOrItem;
        private System.Windows.Forms.Button buttonSaveText;
        private System.Windows.Forms.Button buttonCancelText;
        private System.Windows.Forms.TextBox textBoxNewItemValueCandidate;
        private System.Windows.Forms.Button buttonExportFile;
    }
}

