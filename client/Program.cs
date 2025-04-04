using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;
using Microsoft.Win32;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);


    public static void start()
    {

        //TODO: [Create endpoints and socket]
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
        
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientSocket.Bind(clientEndPoint);
        
        EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        
        
        List<string> domainsToLookUp = new List<string>
        {
            "www.outlook.com",
            "www.test.com",
            "www.customdomain.com",
            "hello.test.com",
            "this.domain.does.not.exist"
        };

        //TODO: [Create and send HELLO]

        Message helloMSG = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "HELO from Client"
        };
        
        SendMessage(clientSocket, (IPEndPoint)serverEndPoint, helloMSG);
        System.Console.WriteLine("Sent HELLO message to Server");
        //TODO: [Receive and print Welcome from server]

        Message welcomeMSG = ReceiveMessage(clientSocket, ref serverEndPoint);
        System.Console.WriteLine($"Received from server: {welcomeMSG.MsgType} with content: {welcomeMSG.Content}");

        int msgId = 2; // HELLO was al 1, dus nu starten we met 2.


        foreach(string domain in domainsToLookUp)
        {
            // TODO: [Create and send DNSLookup Message]
            Message dnsLookupMsg = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.DNSLookup,
                Content = domain
            };
            
            SendMessage(clientSocket, (IPEndPoint)serverEndPoint, dnsLookupMsg);
            System.Console.WriteLine($"\nSent DNSLookup for domain: {domain}");



            //TODO: [Receive and print DNSLookupReply from server]
            Message replyMSG = ReceiveMessage(clientSocket, ref serverEndPoint);
            
            if (replyMSG.MsgType == MessageType.DNSLookupReply)
            {
                string recordJson = JsonSerializer.Serialize(replyMSG.Content);
                DNSRecord? record = JsonSerializer.Deserialize<DNSRecord>(recordJson);
                
                System.Console.WriteLine($"Received DNSLookupReply for: {domain}");
                System.Console.WriteLine($"Type: {record?.Type}, Name: {record?.Name}, Value: {record?.Value}, " +
                                 $"TTL: {record?.TTL}" + 
                                 (record?.Priority != null ? $", Priority: {record?.Priority}" : ""));
            }
            else if (replyMSG.MsgType == MessageType.Error)
            {
                System.Console.WriteLine($"Error looking up {domain}: {replyMSG.Content}");
            }
            else
            {
                System.Console.WriteLine($"\n\n\n\n\n\n\nError. [I have no clue what happened or what I received, sorry :(]\n\n\n\n\n\n\n\n\n\n");
            }

        //TODO: [Send Acknowledgment to Server]
            Message ackMsg = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.Ack,
                Content = $"Received reply for {domain}"
            };
            
            SendMessage(clientSocket, (IPEndPoint)serverEndPoint, ackMsg);
            System.Console.WriteLine($"Sent acknowledgment for domain: {domain}\n");
        
        
        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply
        }

        //TODO: [Receive and print End from server]
        Message endMsg = ReceiveMessage(clientSocket, ref serverEndPoint);
        if (endMsg.MsgType == MessageType.End)
        {
            Console.WriteLine($"\n============================\nReceived END message from server: {endMsg.Content}\n============================\n\n");
        }
        
        clientSocket.Close();




    }
    
    
    
    private static void SendMessage(Socket socket, IPEndPoint endPoint, Message message)
    {
        byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
        socket.SendTo(messageBytes, endPoint);
    }

    
    private static Message ReceiveMessage(Socket socket, ref EndPoint serverEndPoint)
    {
        byte[] buffer = new byte[1024];
        
        int receivedBytes = socket.ReceiveFrom(buffer, ref serverEndPoint);
        string jsonString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        var receivedMessage = JsonSerializer.Deserialize<Message>(jsonString);

        return receivedMessage;
    }
}