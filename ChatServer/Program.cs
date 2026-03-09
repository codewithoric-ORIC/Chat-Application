using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MySql.Data.MySqlClient;
using ChatServer.Data;
using ChatServer.Models;

namespace ChatServer
{
    class Program
    {
        private static Dictionary<string, TcpClient> clientMap = new Dictionary<string, TcpClient>();
        private static Dictionary<string, bool> userStatusMap = new Dictionary<string, bool>();


        static void Main(string[] args)
        {
            int port = 8888;
            TcpListener server = new TcpListener(IPAddress.Any, port);

            try
            {
                server.Start();
                Console.WriteLine($"[SERVER] Chat Server started on port {port}...");
                Console.WriteLine("[SERVER] Waiting for incoming connections...");

                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("[SERVER] A new client connected!");

                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR]: {ex.Message}");
            }
            finally
            {
                server.Stop();
            }
        }

        private static void HandleClient(object? obj)
        {
            TcpClient client = (TcpClient)obj!;
            // StreamReader/Writer ကို သုံးခြင်းဖြင့် စာကြောင်းလိုက် (Line by Line) ဖတ်လို့ရသွားပါမယ်
            using (StreamReader reader = new StreamReader(client.GetStream(), Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.UTF8) { AutoFlush = true })
            {
                string? currentUserName = "";

                try
                {
                    while (true)
                    {
                        // ReadLine() က '\n' (Line Feed) ရောက်တဲ့အထိ စောင့်ဖတ်ပေးမှာပါ
                        string? message = reader.ReadLine();
                        if (message == null) break;

                        string[] parts = message.Split('|');

                        if (parts[0] == "JOIN" && parts.Length > 1)
                        {
                            currentUserName = parts[1];
                            lock (clientMap)
                            {
                                clientMap[currentUserName] = client;
                                userStatusMap[currentUserName] = true;
                            }
                            BroadcastUserList();
                        }
                        else if (parts.Length >= 3)
                        {
                            string target = parts[0];
                            string sender = parts[1];
                            string content = parts[2];

                            SaveMessageToDb(sender, target, content);

                            if (target == "All")
                                BroadcastMessage($"{sender}: {content}", client); // အားလုံးကို ပို့ပေးမယ်
                            else if (clientMap.ContainsKey(target))
                                SendToSpecificClient(clientMap[target], $"[Private] {sender}: {content}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    if (!string.IsNullOrEmpty(currentUserName))
                    {
                        lock (clientMap)
                        {
                            clientMap.Remove(currentUserName);
                            userStatusMap.Remove(currentUserName);
                        }
                        BroadcastUserList();
                    }
                    client.Close();
                }
            }
        }

        private static void BroadcastUserList()
        {
            // ပုံစံ - USERLIST|Name1:True,Name2:False
            List<string> userList = new List<string>();
            lock (clientMap)
            {
                foreach (var user in clientMap.Keys)
                {
                    bool status = userStatusMap.ContainsKey(user) && userStatusMap[user];
                    userList.Add($"{user}:{status}");
                }
            }
            string userListMsg = "USERLIST|" + string.Join(",", userList);
            byte[] data = Encoding.UTF8.GetBytes(userListMsg);

            lock (clientMap)
            {
                foreach (var client in clientMap.Values)
                    client.GetStream().Write(data, 0, data.Length);
            }
        }

        private static void SendToSpecificClient(TcpClient client, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.GetStream().Write(data, 0, data.Length);
        }

        private static void BroadcastMessage(string message, TcpClient excludeClient)
        {
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            lock (clientMap)
            {
                foreach (var client in clientMap.Values) if (client != excludeClient) client.GetStream().Write(data, 0, data.Length);
            }
        }

        private static void SaveMessageToDb(string sender, string receiver, string content)
        {
            string connString = "server=localhost;database=ChatAppDB;user=root;password=admin@123";
            using (var connection = new MySqlConnection(connString))
            {
                connection.Open();
                string query = "INSERT INTO Messages (Sender, Receiver, Content) VALUES (@s, @r, @c)";
                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@s", sender);
                    cmd.Parameters.AddWithValue("@r", receiver);
                    cmd.Parameters.AddWithValue("@c", content);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}