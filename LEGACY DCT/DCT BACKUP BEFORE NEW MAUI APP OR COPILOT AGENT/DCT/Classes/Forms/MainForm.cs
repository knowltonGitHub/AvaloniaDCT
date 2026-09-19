using DCT.Classes;
using DCT.Classes.Extensions;
using DCT.Properties;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace DCT
{
    public enum MouseActionEnum
    {
        Enter,
        Leave
    }

    public enum EditItemState
    {
        NewTag,
        NewItem,
        ExistingItem,
        ExistingTag,
        ExistingValue,
        None
    }

    public partial class FormMain : Form, ISupportsEvents
    {
        public NVM _nvm = new NVM();

        private EditItemState _eis;
        UDPSocket _s = new UDPSocket();
        UDPSocket _c = new UDPSocket();
        private CustomEvent cue = new CustomEvent();                
        public CustomEvents ce { get { return _c.ce; } set { _c.ce = value; } }

        //public NVM NVMCont
        //{
        //    get { return _nvm; }
        //}

        public FormMain()
        {
            InitializeComponent();
        }
        private void FillTagsListFromDatabase()
        {
            listBoxTags.Items.Clear();

            _nvm.FillTagItemValuesFromDatabase();

            Helper.CreateCopyOfCurrentDB(_nvm.TIV.Count, "AFTER_MEMORY_FILL");

            FillTagsListBoxFromInMemoryTagsList();
        }

        private void FillTagsListBoxFromInMemoryTagsList()
        {
            listBoxTags.Items.Clear();
            listBoxTags.Items.AddRange(_nvm.GetAllTags().Items);
        }

        private void FillInfoItemsBasedOnTagSelected(string lastTagSelected)
        {
            EnableControl(groupBoxItems, true);

            listBoxItems.Items.Clear();

            foreach(TagItemValue tempTIV in _nvm.TIV)
            {
                if(tempTIV.Tag == lastTagSelected)
                {
                    listBoxItems.Items.Add(tempTIV.Item);
                }
            }
            
            textBoxItemValue.Text = _nvm.EMPTYSTRING;
            EnableControl(groupBoxValue, false);
            EnableControl(buttonAddNewItem, true);
        }

        private void Ce_ApplicationEvent1(object sender, CustomEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.message.ToString() + System.Environment.NewLine);
        }
        private void AdjustMainFormControls()
        {
            //Resize Tag Controls
            groupBoxTags.Top = 10;
            groupBoxTags.Left = 10;
            groupBoxTags.Height = this.Height - 100;
            groupBoxItems.Top = groupBoxTags.Top;
            groupBoxValue.Top = groupBoxItems.Top;
            groupBoxItems.Height = groupBoxTags.Height;
            groupBoxValue.Height = groupBoxItems.Height;

            listBoxTags.Top = groupBoxTags.Top + 10;
            listBoxTags.Left = groupBoxTags.Left + 5;            
            listBoxTags.Height = groupBoxTags.Height - 100;
            
            listBoxItems.Top = listBoxTags.Top;
            textBoxItemValue.Top = listBoxItems.Top;
            listBoxItems.Height = listBoxTags.Height;
     
            textBoxItemValue.Height = listBoxItems.Height;

            buttonAddNewTag.Top = listBoxTags.Bottom - 100;
            buttonAddNewTag.Left = listBoxTags.Left + listBoxTags.Width + 2;
            buttonDeleteSelectedTag.Top = groupBoxTags.Top + listBoxTags.Top + listBoxTags.Height - buttonDeleteSelectedTag.Height - 8;
            buttonDeleteSelectedTag.Left = listBoxTags.Left + listBoxTags.Width + 2;
          
            buttonAddNewItem.Top = buttonAddNewTag.Top;
            buttonAddNewItem.Left = listBoxItems.Left + listBoxItems.Width + 2;
            buttonDeleteSelectedItem.Top = buttonDeleteSelectedTag.Top;
            buttonDeleteSelectedItem.Left = buttonAddNewItem.Left;
            buttonOpenWithChrome.Top = textBoxItemValue.Bottom + 10;
            buttonEmail.Top = textBoxItemValue.Bottom + 10;
            buttonExportFile.Top = textBoxItemValue.Bottom + 10;
        }
        private void FormMain_SizeChanged(object sender, EventArgs e)
        {
            AdjustMainFormControls();
        }      

        private void SetInitialButtonStates()
        {             EnableControl(buttonAddNewTag, true);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(buttonAddNewItem, false);
            EnableControl(buttonDeleteSelectedItem, false);
            EnableControl(buttonOpenWithChrome, false);
            EnableControl(buttonEmail, false);
            EnableControl(buttonExportFile, false);
        }
        private void LoadInitialData()
        {
            FillTagsListFromDatabase();
            EnableControl(buttonAddNewTag, true);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(groupBoxItems, false);
            EnableControl(groupBoxValue, false);
        }
        private void buttonAddNewTag_Click(object sender, EventArgs e)
        {
            panelEnterOrEditText.Visible = true;
            panelEnterOrEditText.Left = groupBoxTags.Left + listBoxTags.Left;
            panelEnterOrEditText.Top = listBoxTags.Top + 100;
            textBoxNewTagOrItem.Text = _nvm.EMPTYSTRING;
            textBoxNewItemValueCandidate.Visible = false;
            panelEnterOrEditText.Height = 40;

            EnableControl(buttonAddNewTag, false);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(groupBoxItems, false);
            EnableControl(groupBoxValue, false);

            _eis = EditItemState.NewTag;
        }              
        private void buttonDeleteSelectedTag_Click(object sender, EventArgs e)
        {            
            _nvm.DeleteExistingTagFromInMemory(listBoxTags.Text);
            textBox1.Text += "deleted tag:  " + Tag + System.Environment.NewLine;
            FillTagsListBoxFromInMemoryTagsList();
            EnableControl(buttonAddNewTag, false);         
        }
        private void listBoxTags_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(((ListBox)sender).SelectedIndex > -1)
            {                
                EnableControl(buttonDeleteSelectedTag, true);
                EnableControl(buttonDeleteSelectedItem, false);
                FillInfoItemsBasedOnTagSelected(((ListBox)sender).Text.Trim());
            }
        }
        private void textBoxNewTag_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void EnableControls(List<Control> cnts, bool enable)
        {
            foreach(Control c in cnts)
            {
                c.Enabled = enable;
            }
        }
        private void EnableControl(Control c, bool enable)
        {
            c.Enabled = enable;
        }
        private void listBoxItems_SelectedIndexChanged(object sender, EventArgs e)
        {

            bool listBoxItemValid = listBoxItems.NotNullAndNotEmpty();

            // if((sender) != null && (listBoxItems.SelectedItem != "") && (listBoxItems.SelectedItem != null))

            if (listBoxItemValid)
            {
                EnableControl(buttonDeleteSelectedItem, true);

                textBoxItemValue.Text = _nvm.GetValueForInfoItem(listBoxTags.Text, listBoxItems.Text);
                Clipboard.SetText(textBoxItemValue.Text);
                EnableControl(groupBoxValue, true);
                textBoxItemValue.ReadOnly = true;
                EnableControl(buttonEmail, true);
                EnableControl(buttonOpenWithChrome, false);

                if (_nvm.ItemValueIsAWebURL(textBoxItemValue.Text))
                {
                    EnableControl(buttonOpenWithChrome, true);
                }
            }
        }//end of listboxitems selected index changed

        private void ShowMessage(string tempmessage, string tempcaption)
        {
            MessageBox.Show(this, tempmessage, tempcaption);
        }
        private void EnterIntoEditMode()
        {
            //textBox1.Text += "entering edit mode..." + System.Environment.NewLine;

            //_eis = EditItemState.ExistingItem;
            //EnableControl(textBoxItemValue, true);
            //Tag = listBoxTags.Text;
            //ItemName = listBoxItems.Text;
            //EnableControl(listBoxItems, false);
            //EnableControl(buttonDeleteSelectedItem, false);
            //EnableControl(listBoxTags, false);
            //EnableControl(buttonAddNewTag, false);
            //EnableControl(buttonDeleteSelectedTag, false);
            //EnableControl(buttonAddNewItem, true);
            //EnableControl(buttonOpenWithChrome, false);
            //EnableControl(buttonEmail, false);         
        }
        private void buttonEditSelectedItem_Click(object sender, EventArgs e)
        {
            //EnterIntoEditMode();            
        }
        private void buttonDeleteSelectedItem_Click(object sender, EventArgs e)
        {
            if(_nvm.SelectedIndexIsValid(listBoxItems.SelectedIndex))
            {
                _nvm.DeleteInformationItemFromInMemory(listBoxTags.Text, listBoxItems.Text);
//                _nvm.ItemName = listBoxItems.Text;
//                _nvm.DeleteInformationItemFromDatabase();
//                _nvm.DeleteInformationItemFromTagMapping();
                textBox1.Text += "deleted existing name/value pair:  " + listBoxItems.Text + " " + textBoxItemValue.Text + System.Environment.NewLine;
                EnableControl(textBoxItemValue, true);
                textBoxItemValue.Text = _nvm.EMPTYSTRING;
                FillInfoItemsBasedOnTagSelected(listBoxTags.Text);
            }
        }
        private void textBoxNewItem_TextChanged(object sender, EventArgs e)
        {
           
        }
        private void EnterIntoNewItemMode()
        {
          
        }
        #region Mouse Hover Handling
        private System.Drawing.Bitmap GetMouseOverBitmap(string buttonName, MouseActionEnum mouseAction)
        {
            System.Drawing.Bitmap tempBitmap = null;


            switch(buttonName)
            {
                case "buttonAddNewTag":
                case "buttonAddNewItem":
                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap =  Properties.Resources.item_add_hover;
                    }
                    else
                    {
                        tempBitmap = Resources.item_add;
                    }
                    break;
                case "buttonDeleteSelectedTag":
                case "buttonDeleteSelectedItem":

                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap = Resources.item_delete_hover;
                    }
                    else
                    {
                        tempBitmap = Resources.item_delete;
                    }
                    break;
                case "buttonEditSelectedItem":
                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap = Resources.item_edit_hover;
                    }
                    else
                    {
                        tempBitmap = Resources.item_edit;
                    }
                    break;
                case "buttonOpenWithChrome":
                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap = Resources.chrome_hover;
                    }
                    else
                    {
                        tempBitmap = Resources.chrome_icon;
                    }
                    break;
                case "buttonEmail":
                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap = Resources.envelope_green;
                    }
                    else
                    {
                        tempBitmap = Resources.envelope;
                    }
                    break;
                case "buttonExportFile":
                    if (mouseAction == MouseActionEnum.Enter)
                    {
                        tempBitmap = Resources.fileexporthover;
                    }
                    else
                    {
                        tempBitmap = Resources.fileexport;
                    }
                    break;
            }

            return tempBitmap;
        }
        private void ButtonMouseEnter(object sender, EventArgs e)
        {
            _nvm.ChangeButtonImageResource((System.Windows.Forms.Button)sender, GetMouseOverBitmap(((System.Windows.Forms.Button)sender).Name, MouseActionEnum.Enter));
        }
        private void ButtonMouseLeave(object sender, EventArgs e)
        {
            _nvm.ChangeButtonImageResource((System.Windows.Forms.Button)sender, GetMouseOverBitmap(((System.Windows.Forms.Button)sender).Name, MouseActionEnum.Leave));
        }

        #endregion

        private void buttonAddNewItem_Click(object sender, EventArgs e)
        {
            bool tagHasValue = listBoxTags.Text.Length > 0 ? true : false;

            if (tagHasValue)
            {
                Tag = listBoxTags.Text;
                
                panelEnterOrEditText.Left = groupBoxItems.Left + listBoxItems.Left;
                panelEnterOrEditText.Top = listBoxItems.Top + 100;
                textBoxNewTagOrItem.Text = _nvm.EMPTYSTRING;                
                panelEnterOrEditText.Height = 200;
                textBoxNewItemValueCandidate.Top = textBoxNewTagOrItem.Top + textBoxNewTagOrItem.Height + 5;
                textBoxNewItemValueCandidate.Height = panelEnterOrEditText.Height - 50;
                textBoxNewItemValueCandidate.Left = textBoxNewTagOrItem.Left;
                textBoxNewItemValueCandidate.Width = textBoxNewTagOrItem.Width;
                panelEnterOrEditText.Visible = true;
                textBoxNewItemValueCandidate.Visible = true;

                EnableControl(buttonAddNewTag, false);
                EnableControl(buttonDeleteSelectedTag, false);
                EnableControl(groupBoxItems, true);
                EnableControl(groupBoxValue, false);

                _eis = EditItemState.NewItem;
            }             
        }       
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm af = new AboutForm();
            af.ShowDialog();
        }
        private void textBoxNewItem_Enter(object sender, EventArgs e)
        {
            EnterIntoNewItemMode();
        }        
        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Preferences pForm = new Preferences();
            pForm.NVMCont = _nvm;

            pForm.ShowDialog();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void buttonEmail_Click(object sender, EventArgs e)
        {
            GetGMailToAddress gmail = new GetGMailToAddress();
            gmail.ShowDialog();
        }
        private void textBoxSendMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                SendUDPClientMessage(textBoxSendMessage.Text);
            }
        }          
        private void FormMain_Load(object sender, EventArgs e)
        {
            UserLogin ul = new UserLogin();
            // ul.ShowDialog();
            _nvm = ul.NVMCont;
            _eis = EditItemState.None;
            AdjustMainFormControls();
            LoadInitialData();

            _c.ce.ApplicationEvent += this.Ce_ApplicationEvent;
            
            ce.ApplicationEvent += this.Ce_ApplicationEvent1;

            ce.AppEvent(new CustomEventArgs("_nvm online", EventType.tagclick));

            this.Text = this.Text + " - " + _nvm.UserName;
        }

        private void Ce_ApplicationEvent(object sender, CustomEventArgs e)
        {
           // textBoxChatMessages.Text += e.message.ToString() + System.Environment.NewLine;
        }
        private void SendUDPClientMessage(string message)
        {
            //_c.Client("127.0.0.1", 27000);
            //_c.Send(message);
            string[] a = new string[]{"affasd"};
            SendNewMessage(a);
        }
        private void RunUDPServer()
        {
            // _s.Server("127.0.0.1", 27000);

            StartListener();
        }
        private void StartListener()
        {
            using (var server = new ResponseSocket())
            {
                server.Bind("tcp://*:5556");
                string msg = server.ReceiveFrameString();
                Console.WriteLine("From Client: {0}", msg);
                server.SendFrame("World");
            }
        }
        private void SendNewMessage(string[] args)
        {
            using (var client = new RequestSocket())
            {
                client.Connect("tcp://127.0.0.1:5556");
                client.SendFrame("Hello");
                var msg = client.ReceiveFrameString();
                Console.WriteLine("From Server: {0}", msg);
            }
        }
        private void trymq()
        {
            using (var responseSocket = new ResponseSocket("@tcp://*:5555"))
            using (var requestSocket = new RequestSocket(">tcp://localhost:5555"))
            {
                Console.WriteLine("requestSocket : Sending 'Hello'");
                requestSocket.SendFrame("Hello");
                var message = responseSocket.ReceiveFrameString();
                Console.WriteLine("responseSocket : Server Received '{0}'", message);
                Console.WriteLine("responseSocket Sending 'World'");
                responseSocket.SendFrame("World");
                message = requestSocket.ReceiveFrameString();
                Console.WriteLine("requestSocket : Received '{0}'", message);
                Console.ReadLine();
            }
        }
        private void buttonServerMode_Click(object sender, EventArgs e)
        {
            //RunUDPServer();

            trymq();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }
        private void textBoxValue_DoubleClick(object sender, EventArgs e)
        {
            panelEnterOrEditText.Visible = true;
            textBoxNewItemValueCandidate.Visible = true;
            panelEnterOrEditText.Height = 40;
            panelEnterOrEditText.Left = groupBoxItems.Left + listBoxItems.Left;
            panelEnterOrEditText.Top = listBoxItems.Top + 100;
            textBoxNewItemValueCandidate.Text = textBoxItemValue.Text;
            textBoxNewTagOrItem.Text = listBoxItems.Text;
            EnableControl(textBoxNewTagOrItem, false);
            panelEnterOrEditText.Height = 200;
            textBoxNewItemValueCandidate.Top = textBoxNewTagOrItem.Top + textBoxNewTagOrItem.Height + 5;
            textBoxNewItemValueCandidate.Height = panelEnterOrEditText.Height - 50;
            textBoxNewItemValueCandidate.Left = textBoxNewTagOrItem.Left;
            textBoxNewItemValueCandidate.Width = textBoxNewTagOrItem.Width;

            EnableControl(buttonAddNewTag, false);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(groupBoxItems, false);
            EnableControl(groupBoxValue, false);

            _eis = EditItemState.ExistingValue;
        }

        //User clicked Cancel on pop up panel to edit existing / post new tag / item
        private void buttonCancelText_Click(object sender, EventArgs e)
        {
            List<Control> controlsToEnable = new List<Control>() { };
            List<Control> controlsToDisable = new List<Control>() { };

            bool existingtag = (_eis == EditItemState.ExistingTag);
            bool newtag = (_eis == EditItemState.NewTag);
            bool existingitem = (_eis == EditItemState.ExistingItem);
            bool newitem = (_eis == EditItemState.NewItem);
            bool existingvalue = (_eis == EditItemState.ExistingValue);
            bool istag = existingtag & newtag;
            bool isitem = existingitem & newitem;

            panelEnterOrEditText.Visible = false;

            controlsToEnable.Add(buttonAddNewTag);

            if (existingvalue)
            {
                controlsToEnable.Add(groupBoxItems);
                controlsToEnable.Add(listBoxItems);
                controlsToEnable.Add(textBoxItemValue);
            }

            if (istag)
            {
                controlsToDisable.Add(buttonDeleteSelectedTag);                              
            }

            if(isitem)
            {
                controlsToEnable.Add(groupBoxItems);
                controlsToEnable.Add(listBoxItems);
                controlsToEnable.Add(buttonAddNewItem);
                controlsToDisable.Add(buttonDeleteSelectedItem);
            }

            EnableControls(controlsToEnable, true);
            EnableControls(controlsToDisable, false);

            _eis = EditItemState.None;
        }
        private void buttonSaveText_Click(object sender, EventArgs e)
        {
            if (_eis == EditItemState.NewTag)
            {
                _nvm.AddNewTagToInMemoryList(textBoxNewTagOrItem.Text.Trim());
            }

            if (_eis == EditItemState.NewItem)
            {
                _nvm.SaveNewInformationItemToInMemoryList(listBoxTags.Text, 
                    textBoxNewTagOrItem.Text.Trim(), 
                    textBoxNewItemValueCandidate.Text.Trim());
            }

            if (_eis == EditItemState.ExistingItem)
            {
                _nvm.Tag = listBoxTags.Text;
                _nvm.ItemName = textBoxNewTagOrItem.Text.Trim();
                _nvm.OldItemName = listBoxItems.Text;
                _nvm.ItemValue = textBoxNewItemValueCandidate.Text.Trim();
                _nvm.UpdateExistingInformationItemInMemoryList(_nvm.Tag, _nvm.ItemName, _nvm.ItemValue);
            }

            //if (_eis == EditItemState.ExistingValue)
            //{
            //    _nvm.Tag = listBoxTags.Text;
            //    _nvm.ItemName = listBoxItems.Text;
            //    _nvm.ItemValue = textBoxNewItemValueCandidate.Text.Trim();
            //    _nvm.UpdateExistingInformationItemValue();
            //}

            //if (_eis == EditItemState.ExistingTag)
            //{
            //    _nvm.OldTagName = listBoxTags.Text;
            //    _nvm.Tag = textBoxNewTagOrItem.Text.Trim();
            //    _nvm.UpdateExistingTag();
            //}


            ////LoadInitialData();
            //FillTagsListBoxFromInMemoryTagsList();
            //EnableControl(buttonAddNewTag, true);
            //EnableControl(buttonDeleteSelectedTag, true);

            //textBoxNewTagOrItem.Text = _nvm.EMPTYSTRING;
            //textBoxNewItemValueCandidate.Text = _nvm.EMPTYSTRING; 
            //panelEnterOrEditText.Visible = false;
        }
        private void buttonOpenWithChrome_Click(object sender, EventArgs e)
        {
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                FileName = Environment.GetEnvironmentVariable("ProgramFiles(x86)") + @"\Google\Chrome\Application\chrome.exe",
                Arguments = textBoxItemValue.Text
            };

            process.Start();
            //this.Response = process.StandardOutput.ReadToEnd();
            //this.Completed = true;
        }
  
        private void listBoxTags_DoubleClick(object sender, EventArgs e)
        {
            panelEnterOrEditText.Visible = true;
            panelEnterOrEditText.Left = groupBoxTags.Left + listBoxTags.Left;
            panelEnterOrEditText.Top = listBoxTags.Top + 100;
            textBoxNewTagOrItem.Text = listBoxTags.Text;
            textBoxNewItemValueCandidate.Visible = false;
            panelEnterOrEditText.Height = 40;

            EnableControl(buttonAddNewTag, false);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(groupBoxItems, false);
            EnableControl(groupBoxValue, false);

            _eis = EditItemState.ExistingTag;
        }
        private void listBoxItems_DoubleClick(object sender, EventArgs e)
        {
            panelEnterOrEditText.Visible = true;
            textBoxNewItemValueCandidate.Visible = false;
            panelEnterOrEditText.Height = 40;
            panelEnterOrEditText.Left = groupBoxItems.Left + listBoxItems.Left;
            panelEnterOrEditText.Top = listBoxItems.Top + 100;
            textBoxNewTagOrItem.Text = listBoxItems.Text;
            textBoxNewItemValueCandidate.Visible = false;
            panelEnterOrEditText.Height = 200;
            textBoxNewItemValueCandidate.Top = textBoxNewTagOrItem.Top + textBoxNewTagOrItem.Height + 5;
            textBoxNewItemValueCandidate.Height = panelEnterOrEditText.Height - 50;
            textBoxNewItemValueCandidate.Left = textBoxNewTagOrItem.Left;
            textBoxNewItemValueCandidate.Width = textBoxNewTagOrItem.Width;

            EnableControl(buttonAddNewTag, false);
            EnableControl(buttonDeleteSelectedTag, false);
            EnableControl(groupBoxItems, false);
            EnableControl(groupBoxValue, false);

            _eis = EditItemState.ExistingItem;
        }

        private void buttonExportFile_Click(object sender, EventArgs e)
        {

        }

        private void buttonExportFile_MouseEnter(object sender, EventArgs e)
        {
            _nvm.ChangeButtonImageResource((System.Windows.Forms.Button)sender, GetMouseOverBitmap(((System.Windows.Forms.Button)sender).Name, MouseActionEnum.Enter));
        }

        private void buttonExportFile_MouseLeave(object sender, EventArgs e)
        {
            _nvm.ChangeButtonImageResource((System.Windows.Forms.Button)sender, GetMouseOverBitmap(((System.Windows.Forms.Button)sender).Name, MouseActionEnum.Leave));

        }      

        private void FormMain_FormClosed_1(object sender, FormClosedEventArgs e)
        {
            _nvm.OverwriteTagItemsValueTableInDatabase();  
        }
    }//end of class
}//end of namespace
