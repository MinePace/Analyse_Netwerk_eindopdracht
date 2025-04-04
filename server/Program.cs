﻿using System;
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
    static string dnsRecordsFile = @"../server/DNSrecords.json";  // Path to the DNS records JSON file
    static List<DNSRecord> dnsRecords;

    public static void start()
    {

        LoadDNSRecords();
        int count = 0;     

        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);

        using (Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
        {
            serverSocket.Bind(serverEndPoint);

            while (true)
            {
                byte[] buffer = new byte[1024];
                EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

                // TODO:[Receive and print Hello]
                // TODO:[Receive and print DNSLookup]
                int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
                string jsonString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                var receivedMessage = JsonSerializer.Deserialize<Message>(jsonString);
                Console.WriteLine($"Received from client: {Encoding.UTF8.GetString(buffer)}");

                // Process the message
                if (receivedMessage.MsgType == MessageType.Hello)
                {
                    // TODO:[Send Welcome to the client]
                    var welcomeMessage = new Message
                    {
                        MsgId = 4,
                        MsgType = MessageType.Welcome,
                        Content = "Welcome from server"
                    };
                    SendMessage(serverSocket, clientEndPoint, welcomeMessage);
                }
                else if (receivedMessage.MsgType == MessageType.DNSLookup)
                {
                    // TODO:[Query the DNSRecord in Json file]
                    var dnsLookupContent = JsonSerializer.Deserialize<DNSLookupContent>(receivedMessage.Content.ToString());

                    string domain = dnsLookupContent.Name;
                    string type = dnsLookupContent.Type;
                    Console.WriteLine($"Looking up domain: {domain}");

                    var dnsRecord = dnsRecords.Where(r => r.Name == domain).Where(r => r.Type == type).FirstOrDefault();

                    Message replyMessage;

                    // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                    if (dnsRecord != null)
                    {
                        Console.WriteLine($"Domain found: {dnsRecord.Value}");
                        replyMessage = new Message
                        {
                            MsgId = receivedMessage.MsgId,
                            MsgType = MessageType.DNSLookupReply,
                            Content = dnsRecord
                        };
                    }
                    // TODO:[If not found Send Error]
                    else
                    {
                        Console.WriteLine("No domain found");
                        replyMessage = new Message
                        {
                            MsgId = receivedMessage.MsgId,
                            MsgType = MessageType.Error,
                            Content = "Domain not found"
                        };
                    }

                    SendMessage(serverSocket, clientEndPoint, replyMessage);
                }
                else if (receivedMessage.MsgType == MessageType.Ack)
                {
                    // TODO:[Receive Ack about correct DNSLookupReply from the client]
                    Console.WriteLine($"Received ACK from client: {Encoding.UTF8.GetString(buffer)}\n");
                    count++;
                }

                // TODO:[If no further requests receieved send End to the client]
                if (count == 5) 
                {
                    var endMessage = new Message
                    {
                        MsgId = 91377,
                        MsgType = MessageType.End,
                        Content = "End of DNSLookup"
                    };
                    SendMessage(serverSocket, clientEndPoint, endMessage);
                    Console.WriteLine("Sent End message to client\n");
                    count = 0;
                }
            }
        }
    }

    private static void SendMessage(Socket socket, EndPoint endPoint, Message message)
    {
        byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
        socket.SendTo(messageBytes, endPoint);
    }

    private static void LoadDNSRecords()
    {
        // Read the DNS records from the JSON file and deserialize into a list of DNSRecord objects
        string dnsRecordsContent = File.ReadAllText(dnsRecordsFile);
        dnsRecords = JsonSerializer.Deserialize<List<DNSRecord>>(dnsRecordsContent);
    }

    public class DNSLookupContent
    {
        public string Type { get; set; }
        public string Name { get; set; }
    }
}