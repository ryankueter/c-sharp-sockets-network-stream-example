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
using System.Xml.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;

namespace netTools
{
    public class Chat
    {
        [Serializable]
        public class Packet
        {
            public int ID { get; set; }
            public byte[] Content { get; set; }

            // A deserializer for this class
            public static Packet Deserialize(byte[] serializedType)
            {
                Packet deserializedObject = null;

                // This may trigger an exception because of malformed data
                try
                {
                    using (MemoryStream memoryStream = new MemoryStream(serializedType))
                    {
                        BinaryFormatter deserializer = new BinaryFormatter();
                        deserializedObject = (Packet)deserializer.Deserialize(memoryStream);
                    }
                }
                catch (Exception ex)
                { }

                return deserializedObject;
            }
        }

        public class Action
        {
            public const int Join = 0; //"Join"
            public const int ClientList = 1; //"Clients"
            public const int Message = 2; //"Message"
            public const int Disconnect = 3; //"Disconnect Client"
        }

        public class MGSType
        {
            public const int Chat = 0;
            public const int PowerShell = 1;
            public const int ComputerInfo = 2;
        }

        public class Message
        {
            public Client Sender { get; set; }
            public List<Client> Reciptients = new List<Client>();
            public string MSG { get; set; }
            public int Type { get; set; }

            // A deserializer for this class
            public static Message Deserialize(byte[] Content)
            {
                string jsonString = ASCIIEncoding.ASCII.GetString(Content);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<Message>(jsonString);
            }
        }

        public class Client
        {
            public string Name { get; set; }

            // A deserializer for this class
            public static Client Deserialize(byte[] Content)
            {
                string jsonString = ASCIIEncoding.ASCII.GetString(Content);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<Client>(jsonString);
            }
        }

        public class Clients
        {
            public List<Client> List = new List<Client>();

            // A deserializer for this class
            public static Clients Deserialize(byte[] Content)
            {
                string jsonString = ASCIIEncoding.ASCII.GetString(Content);

                JavaScriptSerializer serializer = new JavaScriptSerializer();
                return serializer.Deserialize<Clients>(jsonString);
            }
        }


        public static byte[] Serialize(int ID, object obj)
        {
            // Serialize the contents to Json
            // Type "object" cannot be serialized, you must use a defined type.

            var javaScriptSerializer = new JavaScriptSerializer();
            string jsonString = javaScriptSerializer.Serialize(obj);

            // Convert the Json to a byte[]
            byte[] Content = Encoding.ASCII.GetBytes(jsonString);

            // Serialize the packet
            Chat.Packet packet = new Chat.Packet();
            packet.ID = ID;
            packet.Content = Content;

            byte[] serializedObject;

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, packet);
                serializedObject = stream.ToArray();
            }

            return serializedObject;
        }

        public static string toJson(object obj)
        {
            var javaScriptSerializer = new JavaScriptSerializer();
            return javaScriptSerializer.Serialize(obj);
        }

        // If the class "Packet" will be converted to XML,
        // it must include any custom types it will contain.
        // [XmlInclude(typeof(Message))]
        // [XmlInclude(typeof(Client))]

        public static string toXML(object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            StringWriter stringwriter = new StringWriter();
            serializer.Serialize(stringwriter, obj);
            return stringwriter.ToString();
        }

        public static object fromXML(string xmlText, Type type)
        {
            XmlSerializer serializer = new XmlSerializer(type);
            StringReader stringReader = new StringReader(xmlText);
            return serializer.Deserialize(stringReader);
        }

        public static bool SerializeToFile(string path, object obj)
        {
            bool result = false;

            try
            {
                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                using (TextWriter WriteFileStream = new StreamWriter(path))
                {
                    serializer.Serialize(WriteFileStream, obj);
                }

                result = true;
            }
            catch (Exception ex)
            {

            }

            return result;
        }

        public static object DeserializeFromFile(string path, Type type)
        {
            XmlSerializer serializer = new XmlSerializer(type);

            // Create a new file stream for reading the XML file
            FileStream ReadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            return serializer.Deserialize(ReadFileStream);
        }

    }
}
