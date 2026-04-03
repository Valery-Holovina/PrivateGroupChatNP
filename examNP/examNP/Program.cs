using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Server
{
    static TcpListener listener;
    static Dictionary<TcpClient, string> clients = new();
    static Dictionary<string, List<string>> groups = new();
    static object locker = new();

    static void Main()
    {
        listener = new TcpListener(IPAddress.Any, 6000);
        listener.Start();
        Console.WriteLine("Server started on port 6000...");

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            new Thread(() => HandleClient(client)).Start();
        }
    }

    static void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

      
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string username = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        lock (locker) { clients.Add(client, username); }
        BroadcastSystem($"{username} joined the chat", client);
        Console.WriteLine($"{username} joined");

        try
        {
            while (true)
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                if (message.StartsWith("/msg "))
                {
                    var parts = message.Split(' ', 3);
                    if (parts.Length < 3) continue;
                    SendPrivate(parts[1], $"{username}: {parts[2]}");
                }
                else if (message.StartsWith("/creategroup "))
                {
                    var parts = message.Split(' ', 3);
                    if (parts.Length < 3) continue;
                    var members = new List<string>(parts[2].Split(',', StringSplitOptions.RemoveEmptyEntries));
                    members.Add(username); // add yourself
                    lock (locker) { groups[parts[1]] = members; }
                    BroadcastSystem($"Group {parts[1]} created with: {string.Join(", ", members)}", null);
                }
                else if (message.StartsWith("/group "))
                {
                    var parts = message.Split(' ', 3);
                    if (parts.Length < 3) continue;
                    SendGroup(parts[1], $"{username}: {parts[2]}");
                }
                else
                {
                    Broadcast($"{username}: {message}", client);
                }
            }
        }
        catch { }
        finally
        {
            lock (locker) { clients.Remove(client); }
            BroadcastSystem($"{username} left the chat", client);
            Console.WriteLine($"{username} left");
            client.Close();
        }
    }

    static void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (locker)
        {
            foreach (var c in clients.Keys)
            {
                if (c != sender)
                {
                    try { c.GetStream().Write(data, 0, data.Length); }
                    catch { }
                }
            }
        }
    }

    static void BroadcastSystem(string message, TcpClient exclude)
    {
        byte[] data = Encoding.UTF8.GetBytes("[SYSTEM] " + message);
        lock (locker)
        {
            foreach (var c in clients.Keys)
            {
                if (c != exclude)
                {
                    try { c.GetStream().Write(data, 0, data.Length); }
                    catch { }
                }
            }
        }
    }

    static void SendPrivate(string toUser, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes("[PRIVATE] " + message);
        lock (locker)
        {
            foreach (var c in clients.Keys)
            {
                if (clients[c] == toUser)
                {
                    try { c.GetStream().Write(data, 0, data.Length); }
                    catch { }
                }
            }
        }
    }

    static void SendGroup(string groupName, string message)
    {
        if (!groups.ContainsKey(groupName)) return;
        var members = groups[groupName];
        byte[] data = Encoding.UTF8.GetBytes("[GROUP " + groupName + "] " + message);
        lock (locker)
        {
            foreach (var c in clients.Keys)
            {
                if (members.Contains(clients[c]))
                {
                    try { c.GetStream().Write(data, 0, data.Length); }
                    catch { }
                }
            }
        }
    }
}