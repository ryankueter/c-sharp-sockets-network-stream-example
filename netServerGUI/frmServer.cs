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
using System.IO;
using System.Xml;
using netServer;

namespace netServerGUI
{
    public partial class frmServer : Form
    {
        public frmServer()
        {
            InitializeComponent();
        }

        //Net Socket Variables
        static int portNo = 500;
        static System.Net.IPAddress localAdd = System.Net.IPAddress.Parse("127.0.0.1");
        internal TcpListener listener = new TcpListener(localAdd, portNo);

        //Thread Variables
        internal int actions;
        internal string ListText = "Server Stopped";
        internal int foundItem = 0;
        internal System.ComponentModel.BackgroundWorker ChatWorker;

        private void frmServer_Load(object sender, EventArgs e)
        {
            //Start the background processes to display the progress bar
            lblServiceStatus.Text = "Server Started";
            btnStart.Enabled = false;
            ChatWorker = new System.ComponentModel.BackgroundWorker();
            ChatWorker.WorkerReportsProgress = true;
            ChatWorker.WorkerSupportsCancellation = true;
            ChatWorker.DoWork += ChatWorker_DoWork;
            ChatWorker.ProgressChanged += ChatWorker_ProgressChanged;
            ChatWorker.RunWorkerCompleted += ChatWorker_RunWorkerCompleted;
            ChatWorker.RunWorkerAsync();
        }

        private void ChatWorker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            try
            {
                ListText = "Server Started";

                listener.Start();
                while (true)
                {
                    Server c = new Server();
                    c.Listen(listener.AcceptTcpClient());
                    
                    if (ChatWorker.CancellationPending)
                    {
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                listener.Stop();
                ListText = "Server Stopped";
            }
        }
        private void ChatWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            lblServiceStatus.Text = e.UserState.ToString();
            Refresh();
        }
        private void ChatWorker_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {

        }
        
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (btnStart.Enabled == false)
            {
                DialogResult answer = MessageBox.Show("Are you sure you would like to stop the server?", "Stop Server?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer == DialogResult.Yes)
                {
                    ChatWorker.CancelAsync();
                    listener.Stop();
                    Close();
                }
                else if (answer == DialogResult.No)
                {
                    return;
                }
            }
            else
            {
                Close();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {

        }

    }
}
