using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class Client
{
    static TcpClient client;
    static NetworkStream stream;

    static void Main()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 6000);
            stream = client.GetStream();

            Console.Write("Enter your name: ");
            string username = Console.ReadLine();
            byte[] nameData = Encoding.UTF8.GetBytes(username);
            stream.Write(nameData, 0, nameData.Length);

            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.IsBackground = true;
            receiveThread.Start();

            Console.WriteLine("Commands:");
            Console.WriteLine("/msg username text - private message");
            Console.WriteLine("/creategroup GroupName user1,user2,... - create group");
            Console.WriteLine("/group GroupName text - group message\n");

            while (true)
            {
                Console.Write("> ");
                string message = Console.ReadLine();
                if (string.IsNullOrEmpty(message)) continue;
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error: " + e.Message);
        }
        finally { client?.Close(); }
    }

    static void ReceiveMessages()
    {
        try
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("\n" + message);
                Console.Write("> ");
            }
        }
        catch { Console.WriteLine("Disconnected from server"); }
    }
}