using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections;
using netTools;

namespace netServer
{
    public class Server
    {
        static Hashtable AllClients = new Hashtable(); //---contains a list of all the clients
        public TcpClient _client; //---information about the client
        public string _clientIP;
        public string _clientName;
        public byte[] data; //---used for sending/receiving data
        public bool ReceiveNick = true; //---is the nick name being sent?

        public void Listen(TcpClient client)
        {
	        _client = client;

	        //Get the client IP address
	        _clientIP = client.Client.RemoteEndPoint.ToString();

	        //Add the current client to the AllUsers table
	        AllClients.Add(_clientIP, this);

	        //Start reading data from the client in a separate thread
            data = new byte[_client.ReceiveBufferSize];

	        _client.GetStream().BeginRead(data, 0, Convert.ToInt32(_client.ReceiveBufferSize), new AsyncCallback(ReceiveMessage), null);
        }

        public void ReceiveMessage(IAsyncResult ar)
        {
	        //Read from client
	        int bytesRead = 0;
	        try 
            {
		        if (_client.Connected == true) 
                {
			        lock (_client.GetStream()) 
                    {
				        bytesRead = _client.GetStream().EndRead(ar);
			        }

			        //Client disconnected
			        if (bytesRead < 1) 
                    {
				        sessionEnded(_clientIP);
				        return;
			        } 
                    else 
                    {
                        //The message received from the remote client
                        string messageReceived = System.Text.Encoding.ASCII.GetString(data, 0, bytesRead);
                        
                        // Deserialize the packet
                        Chat.Packet packet = new Chat.Packet();
                        packet = (Chat.Packet)Chat.Deserialize(messageReceived, packet.GetType());

                        Chat.Users recipients = new Chat.Users();
                        switch (packet.ID)
                        {
                            case Chat.MSGType.Join:


                                Chat.User usr = (Chat.User)packet.Content;
                                _clientName = usr.Name;

                                // Add the user set to receive a full list of user
                                recipients.List.Add(usr);

                                Chat.Users usrs = new Chat.Users();
                                foreach (DictionaryEntry c in AllClients)
                                {
                                    Chat.User u = new Chat.User();
                                    u.Name = ((Server)c.Value)._clientName;
                                    usrs.List.Add(u);
                                }

                                Chat.Packet usersListPacket = new Chat.Packet();
                                usersListPacket.ID = Chat.MSGType.ClientList;
                                usersListPacket.Content = usrs;


                                //Send the full user list to the user that just signed on
                                Broadcast(usersListPacket, recipients);

                                //Rebroadcast the original message to everyone that this user is online
                                Broadcast(packet, null);

                                break;
                            case Chat.MSGType.Message:
                                Chat.Message message = (Chat.Message)packet.Content;

                                foreach (string recipient in message.Reciptients)
                                {
                                    Chat.User u = new Chat.User();
                                    u.Name = recipient;
                                    recipients.List.Add(u);
                                }

                                //Send the message to the recipients
                                Broadcast(packet, recipients);
                                break;
                            default:

                                break;
                        }
			        }

			        //Continue reading from the client
			        lock (_client.GetStream()) {
				        _client.GetStream().BeginRead(data, 0, Convert.ToInt32(_client.ReceiveBufferSize), ReceiveMessage, null);
			        }
		        }
	        } catch (Exception ex) {
		        sessionEnded(_clientIP);
	        }
        }

        public void sessionEnded(string clientIP)
        {
	        AllClients.Remove(clientIP);

            Chat.User usr = new Chat.User();
            usr.Name = _clientName;

            Chat.Packet packet = new Chat.Packet();
            packet.ID = Chat.MSGType.Disconnect;
            packet.Content = usr;

            Broadcast(packet, null);
        }
        public void SendMessage(Chat.Packet msg)
        {
	        try {
		        //Send the text
		        System.Net.Sockets.NetworkStream ns = default(System.Net.Sockets.NetworkStream);
		        lock (_client.GetStream()) {
			        ns = _client.GetStream();

                    // Serialize the message
                    string message = "";
                    message = Chat.Serialize(msg);

                    byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(message);
                    ns.Write(bytesToSend, 0, bytesToSend.Length);
                    ns.Flush();
		        }
	        } catch (Exception ex) {
                //MessageBox.Show(ex.ToString);
	        }
        }

        //Broadcast the message to the recipients

        public void Broadcast(Chat.Packet packet, Chat.Users recipients)
        {
	        //If the dataset is Nothing, broadcast to everyone
	        if (recipients == null) {
		        foreach (DictionaryEntry c in AllClients) {
			        //Broadcast the message to all users
                    ((Server)c.Value).SendMessage(packet);
		        }
	        } else {
		        //Broadcast to selected recipients
                foreach (DictionaryEntry c in AllClients)
                {
                    foreach (Chat.User usr in recipients.List)
                    {
                        if (((Server)c.Value)._clientName == usr.Name)
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
