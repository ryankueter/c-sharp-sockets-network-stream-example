/**
 * Author: Ryan A. Kueter
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Xml;
using netTools;
using netClient;

namespace netClientGUI
{
    public partial class frmClient : Form
    {
        public frmClient()
        {
            InitializeComponent();
        }

        //Before performing an action try If (Session.TCP.Connected == true)
        // A new chat session to send and receive messages
        ChatSession Session = new ChatSession();

        // A new client object
        Chat.Client Client = new Chat.Client(); 

        private void frmClient_Load(object sender, EventArgs e)
        {
            cmbAction.SelectedIndex = 0;
            txtUserName.Text = Environment.UserName + DateTime.Now.Second.ToString();

            Session.ClientIP = Dns.GetHostEntry(Dns.GetHostName());
            Session.ServerIP = "127.0.0.1";
            Session.Port = 500;
        }

        private void btnSignIn_Click(object sender, EventArgs e)
        {
            

            if (btnSignIn.Text == "Sign In")
            {
                if (initializeConnection() == true)
                {
                    //The following sends a message which includes the name of the person joining and sends
                    //a message to have the server broadcast an updated list of the users.
                    Client.Name = txtUserName.Text;
                    if (Session.JoinClient(Client) == false)
                    {
                        // Show the exception
                        MessageBox.Show(Session.Exception);
                        return;
                    }

                    //Once signed on, change the controls
                    btnSignIn.Text = "Sign Out";
                    btnSend.Enabled = true;
                    txtUserName.Enabled = false;
                }
            }
            else
            {
                Disconnect();
            }
        }       
       

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                //Make sure a recipient is selected to chat with
                if (lstUsers.SelectedItems.Count < 1)
                {
                    MessageBox.Show("You must select who to chat with.");
                    return;
                }

                Chat.Message MSG = new Chat.Message();
                MSG.Sender = Client;
                MSG.MSG = txtMessage.Text;
                
                switch (cmbAction.SelectedIndex)
                {
                    case 0:
                        MSG.Type = Chat.MGSType.Chat;
                        break;
                    case 1:
                        MSG.Type = Chat.MGSType.PowerShell;
                        break;
                    case 2:
                        MSG.Type = Chat.MGSType.ComputerInfo;
                        break;
                }


                //Add the selected recipients to the table
                foreach (string user in lstUsers.SelectedItems)
                {
                    Chat.Client c = new Chat.Client();
                    c.Name = user;
                    MSG.Reciptients.Add(c);
                }

                //Update the sender's message history
                txtMessageHistory.Text += Client.Name + " - " + txtMessage.Text + Environment.NewLine;

                byte[] msg = Chat.Serialize(Chat.Action.Message, MSG);

                if (Session.SendMessage(msg) == false)
                {
                    // Show the exception
                    MessageBox.Show(Session.Exception);
                    return;
                }
                
                //Clear the message box
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        private void frmClient_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect();
        }

        private void Disconnect()
        { 
            //Sign off from the server
            Session.Disconnect();

            //Clear the list of users
            lstUsers.Items.Clear();

            //Unable to connect, change the controls
            btnSignIn.Text = "Sign In";
            btnSend.Enabled = false;
            txtUserName.Enabled = true;
        }

        public bool initializeConnection()
        {
            try
            {
                Session.TCP = new TcpClient();
                Session.TCP.NoDelay = true;

                //Connect to the server
                Session.TCP.Connect(Session.ServerIP, Session.Port);
                Session.Data = new byte[Session.TCP.ReceiveBufferSize];

                //Read the stream asynchronously from the server
                Session.TCP.GetStream().BeginRead(Session.Data, 0, Convert.ToInt32(Session.TCP.ReceiveBufferSize), new AsyncCallback(ReceiveMessage), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to connect to the server.");
                return false;
            }

            return true;
        }

        public async void ReceiveMessage(IAsyncResult ar)
        {
            try
            {
                //Check if the client is connected before you try to get the stream
                if (Session.TCP.Connected == true)
                {
                    int bytesRead = 0;
                    bytesRead = Session.TCP.GetStream().EndRead(ar);
                    if (bytesRead < 1)
                    {
                        return;
                    }
                    else
                    {

                        // string deserializer
                        //string messageReceived = System.Text.Encoding.ASCII.GetString(Session.Data, 0, bytesRead);
                        //Chat.Packet packet = Chat.Packet.Deserialize(messageReceived);

                        // you have to cast the deserialized object
                        Chat.Packet packet = Chat.Packet.Deserialize(Session.Data);
                        if (packet != null)
                        {
                            await Task.Run(() => receivedData(packet));
                        }


                        //receivedDataSets(packet);
                    }

                    //Continue reading for more data
                    Session.TCP.GetStream().BeginRead(Session.Data, 0, Convert.ToInt32(Session.TCP.ReceiveBufferSize), new AsyncCallback(ReceiveMessage), null);
                }
            }
            catch (Exception ex)
            {
                //As soon as the client is disconnected from the IM server,
                //it will trigger this exception. This would be a good place 
                //to close the client and begin attempting to reconnect.
                Disconnect();
                MessageBox.Show("Your connection to the server was lost.123");
            }
        }

        //Delegate and subroutine to update the textBox control
        //Friend Delegate Sub delUpdateHistory(ByVal str As String)
        internal void receivedData(Chat.Packet packet)
        {
           
            switch (packet.ID)
            {
                case Chat.Action.Join:

                    Chat.Client usr = Chat.Client.Deserialize(packet.Content);
                    lstUsers.Items.Add(usr.Name);

                    break;
                case Chat.Action.ClientList:
                    lstUsers.Items.Clear();

                    Chat.Clients usrs = Chat.Clients.Deserialize(packet.Content);
                    foreach (Chat.Client u in usrs.List)
                    {
                        if (Client.Name != u.Name)
                        {
                            lstUsers.Items.Add(u.Name);
                        }
                    }

                    break;
                case Chat.Action.Message:
                    //Read the message
                    Chat.Message msg = Chat.Message.Deserialize(packet.Content);

                    switch (msg.Type)
                    {
                        case Chat.MGSType.Chat:
                            
                            break;
                        case Chat.MGSType.PowerShell:
                            
                            break;
                        case Chat.MGSType.ComputerInfo:
                            
                            break;
                    }

                    //Read the recipients...

                    //Update the recipient's message history
                    txtMessageHistory.Text += msg.Sender.Name + " - " + msg.MSG + Environment.NewLine;
                    break;
                case Chat.Action.Disconnect:
                    Chat.Client user = Chat.Client.Deserialize(packet.Content);

                    //Remove the user from the listbox
                    lstUsers.Items.Remove(user.Name);

                    break;
                default:
                    break;
            }
        }

        //private void timerMain_Tick(System.Object sender, System.EventArgs e)
        //{
        //    if (signInAttempt == true)
        //    {
        //        //Attempt to sign the user in
        //        if (signInTime < System.DateTime.Now.AddSeconds(1))
        //        {
        //            signInAttempt = false;

        //            txtErrorMessage.Text = "Attempting to connect to the Nibble server...";
        //            txtErrorMessage.Refresh();

        //            logUserIn();
        //        }
        //        else
        //        {
        //            TimeSpan remainingTime = signInTime.Subtract(System.DateTime.Now);

        //            txtErrorMessage.Text = "Unable to connect! Attempting to reconnect in " + remainingTime.Seconds + " seconds...";
        //        }
        //    }
        //    else
        //    {
        //        //The user is signed in 

        //        //IsConnected checks to see if the client TCP stream is connected
        //        //If not, it attempts to reconnect.
        //        if (IsConnected(client) == false)
        //        {
        //            if (isOnline == true)
        //            {
        //                reconnectAttempt();
        //            }
        //        }

        //        //The size of the structure for the API call.
        //        info.structSize = Strings.Len(info);
        //        //
        //        //Call the API.
        //        GetLastInputInfo(info);
        //        //
        //        //Compare the tickcount values to determine if activity has occurred or not.
        //        if (firstTick != info.tickCount)
        //        {
        //            //Set the last active time
        //            lastActivity = Now;

        //            //Get the new tick value.
        //            firstTick = info.tickCount;

        //        }

        //        ElapsedTime = Now().Subtract(lastActivity);

        //        //************* SET STATUS ***************************

        //        if (Math.Round(ElapsedTime.TotalSeconds) > 30)
        //        {
        //            //If the user's status is not any of the following statuses, change the status to Away after 30 seconds.

        //            if (!(cmbStatus.Text == "On the Phone") & !(cmbStatus.Text == "In Meeting") & !(cmbStatus.Text == "On Break") & !(cmbStatus.Text == "Out to Lunch"))
        //            {
        //                if (!(cmbStatus.Text == "Away"))
        //                {
        //                    previousStatus = cmbStatus.Text;
        //                    autoSet = true;
        //                }
        //                cmbStatus.Text = "Away";

        //                //If the user is away for more than 10 minutes
        //                if (Math.Round(ElapsedTime.TotalSeconds) > 1800)
        //                {
        //                    autoLogoff = true;
        //                    cmbStatus.SelectedItem = "Offline";
        //                    refreshUserStatus();
        //                    isOnline = false;
        //                    SignInToolStripMenuItem.Text = "Sign In";
        //                    Disconnect();
        //                }
        //            }

        //        }
        //        else
        //        {
        //            if (cmbStatus.Text == "Away" & autoSet == true)
        //            {
        //                cmbStatus.SelectedItem = previousStatus;
        //            }
        //            else if (autoLogoff == true)
        //            {
        //                autoLogoff = false;
        //                logUserIn();
        //                cmbStatus.Enabled = true;
        //                cmbStatus.SelectedItem = "Online";
        //                refreshUserStatus();
        //            }
        //            autoSet = false;
        //        }

        //        //************* LOCK SESSION *************************
        //        double autoLockTime = autoLockValue * 60;
        //        autoLockTime = autoLockTime + autoLockValueSeconds;
        //        if (autoLockEnabled == true)
        //        {
        //            if (Math.Round(ElapsedTime.TotalSeconds) > autoLockTime)
        //            {
        //                lockScreen();
        //            }
        //        }
        //    }
        //}
        //internal void reconnectAttempt()
        //{
        //    wizardPagesMain.SelectedTab = tabSplash;
        //    isOnline = false;
        //    SignInToolStripMenuItem.Text = "Sign In";
        //    signInAttempt = true;
        //    signInTime = System.DateTime.Now.AddSeconds(10);
        //}
        //internal bool IsConnected(TcpClient client)
        //{
        //    try
        //    {
        //        bool connected = !(client.Client.Poll(1, SelectMode.SelectRead) && client.Client.Available == 0);
        //        return connected;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}
    }
}
