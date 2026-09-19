namespace DCT
{
    partial class GetGMailToAddress
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
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxToAddress = new System.Windows.Forms.TextBox();
            this.buttonSendToAddress = new System.Windows.Forms.Button();
            this.buttonCancelSend = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.listBoxEmailAddresses = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 254);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(257, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter new email address (Press ENTER key to Save)";
            // 
            // textBoxToAddress
            // 
            this.textBoxToAddress.Location = new System.Drawing.Point(5, 270);
            this.textBoxToAddress.Name = "textBoxToAddress";
            this.textBoxToAddress.Size = new System.Drawing.Size(289, 20);
            this.textBoxToAddress.TabIndex = 1;
            this.textBoxToAddress.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxToAddress_KeyPress);
            // 
            // buttonSendToAddress
            // 
            this.buttonSendToAddress.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonSendToAddress.Location = new System.Drawing.Point(219, 307);
            this.buttonSendToAddress.Name = "buttonSendToAddress";
            this.buttonSendToAddress.Size = new System.Drawing.Size(75, 23);
            this.buttonSendToAddress.TabIndex = 2;
            this.buttonSendToAddress.Text = "Send";
            this.buttonSendToAddress.UseVisualStyleBackColor = true;
            this.buttonSendToAddress.Click += new System.EventHandler(this.buttonSendToAddress_Click);
            // 
            // buttonCancelSend
            // 
            this.buttonCancelSend.Location = new System.Drawing.Point(136, 307);
            this.buttonCancelSend.Name = "buttonCancelSend";
            this.buttonCancelSend.Size = new System.Drawing.Size(75, 23);
            this.buttonCancelSend.TabIndex = 3;
            this.buttonCancelSend.Text = "Cancel";
            this.buttonCancelSend.UseVisualStyleBackColor = true;
            this.buttonCancelSend.Click += new System.EventHandler(this.buttonCancelSend_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(154, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Select email address to send to";
            // 
            // listBoxEmailAddresses
            // 
            this.listBoxEmailAddresses.FormattingEnabled = true;
            this.listBoxEmailAddresses.Location = new System.Drawing.Point(5, 26);
            this.listBoxEmailAddresses.Name = "listBoxEmailAddresses";
            this.listBoxEmailAddresses.Size = new System.Drawing.Size(289, 225);
            this.listBoxEmailAddresses.TabIndex = 5;
            // 
            // GetGMailToAddress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(301, 337);
            this.ControlBox = false;
            this.Controls.Add(this.listBoxEmailAddresses);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonCancelSend);
            this.Controls.Add(this.buttonSendToAddress);
            this.Controls.Add(this.textBoxToAddress);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GetGMailToAddress";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Select Email Address ";
            this.Load += new System.EventHandler(this.GetGMailToAddress_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxToAddress;
        private System.Windows.Forms.Button buttonSendToAddress;
        private System.Windows.Forms.Button buttonCancelSend;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listBoxEmailAddresses;
    }
}