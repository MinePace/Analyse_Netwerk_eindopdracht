using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]
    static List<DNSRecord> ReadDNSRecords()
    {
        string dnsRecordsFile = @"DNSrecords.json";
        string jsonContent = File.ReadAllText(dnsRecordsFile);
        return JsonSerializer.Deserialize<List<DNSRecord>>(jsonContent) ?? new List<DNSRecord>();
    }

    public static void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        UdpClient udpServer = new UdpClient(serverEndPoint);

        Console.WriteLine($"Server started on {setting.ServerIPAddress}:{setting.ServerPortNumber}");
        
        try
        {
            while (true) // Outer loop to keep accepting new clients
            {
                Console.WriteLine("Waiting for a new client...");
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // TODO:[Receive and print a received Message from the client]
                // TODO:[Receive and print Hello]
                byte[] receivedBytes = udpServer.Receive(ref clientEndPoint);
                string jsonMessage = Encoding.UTF8.GetString(receivedBytes);
                Message receivedMessage = JsonSerializer.Deserialize<Message>(jsonMessage);

                Console.WriteLine($"New client connected from {clientEndPoint}");
                Console.WriteLine($"Received {receivedMessage.MsgType} from client: {receivedMessage.Content}");

                // TODO:[Send Welcome to the client]
                Message welcomeMessage = new Message
                {
                    MsgId = 1,
                    MsgType = MessageType.Welcome,
                    Content = "Welcome to the DNS Server"
                };
                SendMessage(udpServer, clientEndPoint, welcomeMessage);
                Console.WriteLine("Sent WELCOME message to client");

                // Read DNS records
                List<DNSRecord> dnsRecords = ReadDNSRecords();
                int msgId = 2;

                // Process client requests
                bool clientActive = true;
                while (clientActive)
                {
                    // TODO:[Receive and print DNSLookup]
                    receivedBytes = udpServer.Receive(ref clientEndPoint);
                    jsonMessage = Encoding.UTF8.GetString(receivedBytes);
                    receivedMessage = JsonSerializer.Deserialize<Message>(jsonMessage);

                    if (receivedMessage.MsgType == MessageType.DNSLookup)
                    {
                        string domainName = receivedMessage.Content.ToString();
                        Console.WriteLine($"Received DNSLookup request for: {domainName}");

                        // TODO:[Query the DNSRecord in Json file]
                        DNSRecord foundRecord = dnsRecords.FirstOrDefault(r => r.Name == domainName);

                        Message responseMessage;
                        if (foundRecord != null)
                        {
                            // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                            responseMessage = new Message
                            {
                                MsgId = msgId++,
                                MsgType = MessageType.DNSLookupReply,
                                Content = foundRecord
                            };
                            Console.WriteLine($"Found record for {domainName}, sending reply");
                        }
                        else
                        {
                            // TODO:[If not found Send Error]
                            responseMessage = new Message
                            {
                                MsgId = msgId++,
                                MsgType = MessageType.Error,
                                Content = $"No DNS record found for {domainName}"
                            };
                            Console.WriteLine($"No record found for {domainName}, sending error");
                        }

                        SendMessage(udpServer, clientEndPoint, responseMessage);

                        // TODO:[Receive Ack about correct DNSLookupReply from the client]
                        receivedBytes = udpServer.Receive(ref clientEndPoint);
                        jsonMessage = Encoding.UTF8.GetString(receivedBytes);
                        receivedMessage = JsonSerializer.Deserialize<Message>(jsonMessage);

                        if (receivedMessage.MsgType == MessageType.Ack)
                        {
                            Console.WriteLine($"Received acknowledgment: {receivedMessage.Content}");
                        }
                        
                        // Check if there are more requests (wait a short time)
                        bool moreRequests = udpServer.Available > 0;
                        if (!moreRequests)
                        {
                            // TODO:[If no further requests receieved send End to the client]
                            Message endMessage = new Message
                            {
                                MsgId = msgId++,
                                MsgType = MessageType.End,
                                Content = "All DNS lookups completed"
                            };
                            SendMessage(udpServer, clientEndPoint, endMessage);
                            Console.WriteLine("Sent END message to client");
                            clientActive = false; // End communication with current client
                            Console.WriteLine("Client session completed");
                        }
                    }
                    else if (receivedMessage.MsgType == MessageType.End)
                    {
                        // Client wants to disconnect
                        Console.WriteLine("Client requested to end the session");
                        clientActive = false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            udpServer.Close();
            Console.WriteLine("Server closed");
        }
    }

    private static void SendMessage(UdpClient client, IPEndPoint endpoint, Message message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);
        client.Send(bytes, bytes.Length, endpoint);
    }
}