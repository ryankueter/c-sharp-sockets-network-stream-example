/**
 * Author: Ryan A. Kueter
 * For the full copyright and license information, please view the LICENSE
 * file that was distributed with this source code.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections;
using netTools;
using System.Windows.Forms;

namespace netServer
{
    public class Server
    {
        static Hashtable IPList = new Hashtable(); //---contains a list of all the clients
        public TcpClient Client; //---information about the client
        public string ClientIP;
        public string ClientName;
        public byte[] Data; //---used for sending/receiving data
        public bool ReceiveNIC = true; //---is the NIC name being sent?

        public void Listen(TcpClient client)
        {
	        Client = client;

	        //Get the client IP address
	        ClientIP = client.Client.RemoteEndPoint.ToString();

	        //Add the current client to the AllUsers table
	        IPList.Add(ClientIP, this);

	        //Start reading data from the client in a separate thread
            Data = new byte[Client.ReceiveBufferSize];

	        Client.GetStream().BeginRead(Data, 0, Convert.ToInt32(Client.ReceiveBufferSize), new AsyncCallback(ReceiveMessage), null);
        }

        public async void ReceiveMessage(IAsyncResult ar)
        {
	        //Read from client
	        int bytesRead = 0;
	        try 
            {
		        if (Client.Connected == true) 
                {
			        lock (Client.GetStream()) 
                    {
				        bytesRead = Client.GetStream().EndRead(ar);
			        }

			        //Client disconnected
			        if (bytesRead < 1) 
                    {
				        sessionEnded(ClientIP);
				        return;
			        } 
                    else 
                    {
                        // string deserializer
                        //string messageReceived = System.Text.Encoding.ASCII.GetString(Data, 0, bytesRead);
                        //Chat.Packet packet = Chat.Packet.Deserialize(messageReceived);

                        // you have to cast the deserialized object
                        Chat.Packet packet = Chat.Packet.Deserialize(Data);
                        if (packet != null)
                        {
                            await Task.Run(() => receivedData(packet));
                        }
			        }

			        //Continue reading from the client
			        lock (Client.GetStream()) {
				        Client.GetStream().BeginRead(Data, 0, Convert.ToInt32(Client.ReceiveBufferSize), ReceiveMessage, null);
			        }
		        }
	        } catch (Exception ex) {
		        sessionEnded(ClientIP);
	        }
        }

        internal void receivedData(Chat.Packet packet)
        {
            Chat.Clients recipients = new Chat.Clients();

            if (packet != null)
            {
                switch (packet.ID)
                {
                    case Chat.Action.Join:

                        // Deserialize the content
                        Chat.Client usr = Chat.Client.Deserialize(packet.Content);
                        ClientName = usr.Name;

                        // Add the user set to receive a full list of user
                        recipients.List.Add(usr);

                        Chat.Clients usrs = new Chat.Clients();
                        foreach (DictionaryEntry c in IPList)
                        {
                            Chat.Client u = new Chat.Client();
                            u.Name = ((Server)c.Value).ClientName;
                            usrs.List.Add(u);
                        }

                        byte[] msg = Chat.Serialize(Chat.Action.ClientList, usrs);

                        //Send the full user list to the user that just signed on
                        Broadcast(msg, recipients);

                        //Rebroadcast the original message to everyone that this user is online
                        Broadcast(Data, null);

                        break;
                    case Chat.Action.Message:
                        Chat.Message message = Chat.Message.Deserialize(packet.Content);

                        foreach (Chat.Client recipient in message.Reciptients)
                        {
                            recipients.List.Add(recipient);
                        }

                        //Send the message to the recipients
                        Broadcast(Data, recipients);
                        break;
                    default:

                        break;
                }
            }
        }

        public void sessionEnded(string clientIP)
        {
	        IPList.Remove(clientIP);

            Chat.Client usr = new Chat.Client();
            usr.Name = ClientName;

            
            byte[] msg = Chat.Serialize(Chat.Action.Disconnect, usr);

            Broadcast(msg, null);
        }
        public void SendMessage(byte[] bytesToSend)
        {
	        try {
		        //Send the text
		        System.Net.Sockets.NetworkStream ns = default(System.Net.Sockets.NetworkStream);
		        lock (Client.GetStream()) {
			        ns = Client.GetStream();
                    //byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(msg);
                    ns.Write(bytesToSend, 0, bytesToSend.Length);
                    ns.Flush();
		        }
	        } catch (Exception ex) {
                //MessageBox.Show(ex.ToString);
	        }
        }

        //Broadcast the message to the recipients

        public void Broadcast(byte[] packet, Chat.Clients recipients)
        {
	        //If the dataset is Nothing, broadcast to everyone
	        if (recipients == null) 
            {
		        foreach (DictionaryEntry c in IPList) 
                {
			        //Broadcast the message to all users
                    ((Server)c.Value).SendMessage(packet);
		        }
	        } 
            else 
            {
		        //Broadcast to selected recipients
                foreach (DictionaryEntry c in IPList)
                {
                    foreach (Chat.Client usr in recipients.List)
                    {
                        if (((Server)c.Value).ClientName == usr.Name)
                        {
                            //Send message to the recipient
                            ((Server)c.Value).SendMessage(packet);

                            //---log it locally
                            //Console.WriteLine("sending -----> " & message)
                            break; // TODO: might not be correct. Was : Exit For
                        }
                    }
		        }
	        }
        }
    }
}
