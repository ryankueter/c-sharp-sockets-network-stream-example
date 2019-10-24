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
using System.Net;
using netTools;

namespace netClient
{
    public class ChatSession
    {
        //Network information
        public IPHostEntry ClientIP = new IPHostEntry();
        public string ServerIP;
        public int Port;
        public TcpClient TCP = new TcpClient();
        
        //Stores data sending and receiving
        //This may also be used to send files
        public byte[] Data;

        // Return an error
        public string Exception = null;

        public bool JoinClient(Chat.Client user)
        {
            Exception = null;
            bool result = false;

            try
            {
                // Serialize the message
                byte[] msg = Chat.Serialize(Chat.Action.Join, user);

                SendMessage(msg);

                result = true;
            }
            catch (Exception ex)
            {
                Exception = ex.Message;
            }

            return result;
        }


        public bool SendMessage(byte[] bytesToSend)
        {
            Exception = null;
            bool result = false;

            try
            {
                //Send the text
                if (TCP.Connected == true)
                {
                    System.Net.Sockets.NetworkStream ns = default(System.Net.Sockets.NetworkStream);
                    lock (TCP.GetStream())
                    {
                        ns = TCP.GetStream();

                        //byte[] bytesToSend = System.Text.Encoding.ASCII.GetBytes(msg);

                        //Sends the text
                        ns.Write(bytesToSend, 0, bytesToSend.Length);
                        ns.Flush();
                    }

                    result = true;
                }
                else
                {
                    //If the client is unable to connect to the IM server
                    //and it tries to send a message, it will display this message.
                    Exception = "Your connection to the server was lost.";
                }

            }
            catch (Exception ex)
            {
                Exception = ex.ToString();
            }

            return result;
        }

        
        public void Disconnect()
        {
            //Disconnect from the server
            try
            {
                if (TCP.Connected == true)
                {
                    TCP.GetStream().Close();
                    TCP.Close();
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
