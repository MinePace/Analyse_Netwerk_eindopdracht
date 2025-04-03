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
        
        // A list of domain names to look up (includes both valid and invalid ones)
        List<string> domainsToLookup = new List<string>
        {
            "www.outlook.com",
            "www.test.com",
            "www.nonexistent.com",  // This one should not exist in DNSrecords.json
            "www.sample.com",
            "example.com"  // For MX record
        };

        //TODO: [Create and send HELLO]
        Message helloMsg = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello from Client"
        };
        
        SendMessage(udpClient, serverEndPoint, helloMsg);
        Console.WriteLine("Sent HELLO message to server");

        //TODO: [Receive and print Welcome from server]
        Message welcomeMsg = ReceiveMessage(udpClient, ref serverEndPoint);
        Console.WriteLine($"Received from server: {welcomeMsg.MsgType} with content: {welcomeMsg.Content}");
        
        int msgId = 2;  // Start with ID 2 since we already used 1 for HELLO

        foreach (string domain in domainsToLookup)
        {
            // TODO: [Create and send DNSLookup Message]
            Message dnsLookupMsg = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.DNSLookup,
                Content = domain
            };
            
            SendMessage(udpClient, serverEndPoint, dnsLookupMsg);
            Console.WriteLine($"Sent DNSLookup for domain: {domain}");
            
            //TODO: [Receive and print DNSLookupReply from server]
            Message replyMsg = ReceiveMessage(udpClient, ref serverEndPoint);
            
            if (replyMsg.MsgType == MessageType.DNSLookupReply)
            {
                string recordJson = JsonSerializer.Serialize(replyMsg.Content);
                DNSRecord? record = JsonSerializer.Deserialize<DNSRecord>(recordJson);
                
                Console.WriteLine($"Received DNSLookupReply for: {domain}");
                Console.WriteLine($"Type: {record?.Type}, Name: {record?.Name}, Value: {record?.Value}, " +
                                 $"TTL: {record?.TTL}" + 
                                 (record?.Priority != null ? $", Priority: {record?.Priority}" : ""));
            }
            else if (replyMsg.MsgType == MessageType.Error)
            {
                Console.WriteLine($"Error looking up {domain}: {replyMsg.Content}");
            }
            
            //TODO: [Send Acknowledgment to Server]
            Message ackMsg = new Message
            {
                MsgId = msgId++,
                MsgType = MessageType.Ack,
                Content = $"Received reply for {domain}"
            };
            
            SendMessage(udpClient, serverEndPoint, ackMsg);
            Console.WriteLine($"Sent acknowledgment for domain: {domain}");
        }
        
        //TODO: [Receive and print End from server]
        Message endMsg = ReceiveMessage(udpClient, ref serverEndPoint);
        if (endMsg.MsgType == MessageType.End)
        {
            Console.WriteLine($"Received END message from server: {endMsg.Content}");
        }
        
        udpClient.Close();
    }
    
    private static void SendMessage(UdpClient client, IPEndPoint endpoint, Message message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
        client.Send(bytes, bytes.Length, endpoint);
    }
    
    private static Message ReceiveMessage(UdpClient client, ref IPEndPoint remoteEP)
    {
        byte[] receivedBytes = client.Receive(ref remoteEP);
        string jsonMessage = Encoding.UTF8.GetString(receivedBytes);
        return JsonSerializer.Deserialize<Message>(jsonMessage);
    }
}