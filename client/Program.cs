using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

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
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
        
        UdpClient udpClient = new UdpClient(clientEndPoint);
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        List<string> domainsToLookUp = new List<string>
        {
            "www.outlook.com",
            "www.test.com",
            "www.nonexistent.com",
            "www.sample.com",
            "example.com"
        };

        //TODO: [Create and send HELLO]

        Message helloMSG = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "HELO from Client"
        };
        
        SendMessage(clientSocket, serverEndPoint, helloMSG);
        System.Console.WriteLine("Sent HELLO message to Server");
        //TODO: [Receive and print Welcome from server]

        // TODO: [Create and send DNSLookup Message]


        //TODO: [Receive and print DNSLookupReply from server]


        //TODO: [Send Acknowledgment to Server]

        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

        //TODO: [Receive and print End from server]





    }
    
    
    
    private static void SendMessage(Socket socket, EndPoint endPoint, Message message)
    {
        byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
        socket.SendTo(messageBytes, endPoint);
    }

    
    private static Message ReceiveMessage(UdpClient client, ref IPEndPoint remoteEP)
    {
        string jsonMessage = Encoding.UTF8.GetString(client.Receive(ref remoteEP));
        return JsonSerializer.Deserialize<Message>(jsonMessage);
    }
}