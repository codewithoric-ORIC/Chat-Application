using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using ChatClient.Models;
using Avalonia.Data.Converters;
using System.Globalization;

namespace ChatClient.Views;

// partial keyword က အရေးကြီးပါတယ်။ UI နဲ့ ချိတ်ပေးတာပါ။
public partial class MainWindow : Window
{
    private NetworkStream? _stream;
    private string _userRole = "";
    private string _userName = "";
    private bool _isListening = true;
    private string _targetUser = "All";

    // private ObservableCollection<string> _onlineUsers = new ObservableCollection<string>();
    private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();
    private ObservableCollection<UserStatusItem> _onlineUsers = new ObservableCollection<UserStatusItem>();
    private TcpClient? client;
    private NetworkStream? clientStream;

    public MainWindow()
    {
        InitializeComponent();

        // UI Components တွေနဲ့ Data ချိတ်ဆက်ခြင်း
        UserList.ItemsSource = _onlineUsers;
        ChatDisplayList.ItemsSource = _chatMessages;

        _onlineUsers.Add(new UserStatusItem { UserName = "All", IsOnline = true });
    }

    public void OnSubmitClick(object sender, RoutedEventArgs e)
    {
        // ဒီမှာ Login/Register လုပ်မယ့် Code တွေ ရေးရပါမယ်
        Console.WriteLine("Button Clicked!");
    }
    private bool isRegisterMode = false;

    private void OnToggleModeClick(object sender, RoutedEventArgs e)
    {
        isRegisterMode = !isRegisterMode;
        TitleText.Text = isRegisterMode ? "Register" : "Login";
        SubmitButton.Content = isRegisterMode ? "Register" : "Login";
    }

    // မှတ်ချက်- InitializeComponent() ကို လက်နဲ့ ထပ်ရေးစရာ မလိုပါဘူး။
    // partial class ဖြစ်တဲ့အတွက် Avalonia က နောက်ကွယ်မှာ အလိုလို လုပ်ပေးပါတယ်။

    private void AddMessage(string sender, string text, bool isMine)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _chatMessages.Add(new ChatMessage
            {
                Sender = sender,
                Message = text,
                Time = DateTime.Now.ToString("HH:mm"),
                // UI အတွက် လိုအပ်သော Property များ
                Alignment = isMine ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                BubbleColor = isMine ? "#DCF8C6" : "#FFFFFF",
                Margin = isMine ? new Thickness(50, 5, 0, 5) : new Thickness(0, 5, 50, 5)
            });

            // Scroll လုပ်ခြင်း
            ChatDisplayList.ScrollIntoView(_chatMessages[_chatMessages.Count - 1]);
        });
    }

    public void OnRoleChanged(object sender, RoutedEventArgs e)
    {
        // TeacherCodeInput ကို UI ကနေ တိုက်ရိုက်ယူသုံးပါမယ်
        if (TeacherCodeInput != null)
        {
            TeacherCodeInput.IsVisible = TeacherRole.IsChecked == true;
        }
    }

    public void OnLoginClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UserNameInput.Text)) return;

        if (TeacherRole.IsChecked == true)
        {
            string secretKey = "admin123";
            if (TeacherCodeInput.Text != secretKey)
            {
                AddMessage("System", "Access Denied: Invalid Teacher Key!", false);
                return;
            }
            _userRole = "Teacher";
        }
        else
        {
            _userRole = "Student";
        }

        _userName = UserNameInput.Text;
        LoginSection.IsVisible = false;
        ChatSection.IsVisible = true;
        UserWelcomeText.Text = $"{_userName} ({_userRole})";

        ConnectToServer();
    }

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient("127.0.0.1", 8888);
            clientStream = client.GetStream();
            _stream = clientStream; // သင့်ကုဒ်ထဲက _stream ကို assign လုပ်ပေးပါ

            // ဝင်ဝင်ချင်း JOIN message ပို့ပါ (အရေးကြီးဆုံး)
            SendMessage($"JOIN|{_userName}");

            // စာလက်ခံဖို့ Thread ဖွင့်ပါ
            Thread listenerThread = new Thread(ReceiveMessages); // ListenForMessages အစား ReceiveMessages သုံးပါ
            listenerThread.IsBackground = true;
            listenerThread.Start();

            Console.WriteLine("Connected to server!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Connection Error: " + ex.Message);
        }
    }

    private void ListenForMessages()
    {
        byte[] buffer = new byte[1024];
        while (clientStream != null && clientStream.CanRead)
        {
            try
            {
                int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                // UI ပေါ်မှာ ပြဖို့ Dispatcher သုံးပါ
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    // ဤနေရာတွင် UserList သို့မဟုတ် Message ကို UI ထဲထည့်ပါ
                    Console.WriteLine("Received: " + message);
                });
            }
            catch { break; }
        }
    }

    private void ReceiveMessages()
    {
        try
        {
            byte[] buffer = new byte[4096]; // Buffer size ကို အနည်းငယ်တိုးထားပါ
            string incompleteMessage = "";  // အပိုင်းပိုင်းရောက်လာတဲ့စာကို စုထားဖို့

            while (_isListening && _stream != null)
            {
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break; // Connection ပြတ်သွားရင် ရပ်ပါ

                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                incompleteMessage += receivedData;

                // Server က စာတိုင်းကို \n နဲ့ အဆုံးသတ်ပြီး ပို့တယ်လို့ ယူဆပါတယ်
                while (incompleteMessage.Contains("\n"))
                {
                    int newlineIndex = incompleteMessage.IndexOf("\n");
                    string completeMessage = incompleteMessage.Substring(0, newlineIndex).Trim();
                    incompleteMessage = incompleteMessage.Substring(newlineIndex + 1);

                    if (string.IsNullOrEmpty(completeMessage)) continue;

                    Dispatcher.UIThread.Post(() =>
                    {
                        Console.WriteLine($"Processing: {completeMessage}");

                        if (completeMessage.StartsWith("USERLIST|"))
                        {
                            UpdateUserList(completeMessage);
                        }
                        else
                        {
                            // လက်ခံရရှိတဲ့ စာသား (ဥပမာ: "Su Su: Hi")
                            string msg = completeMessage.Trim();
                            
                            // အခြေအနေ ၁: Format မှန်လျှင် (Target|Sender|Message)
                            if (msg.Contains("|"))
                            {
                                var parts = msg.Split('|');
                                if (parts.Length >= 3)
                                    AddMessage(parts[1], parts[2], false);
                            }
                            // အခြေအနေ ၂: Format ":" နဲ့လာလျှင် (ဥပမာ: "Su Su: Hi")
                            else if (msg.Contains(":"))
                            {
                                int colonIndex = msg.IndexOf(":");
                                string sender = msg.Substring(0, colonIndex).Trim();
                                string text = msg.Substring(colonIndex + 1).Trim();
                                AddMessage(sender, text, false);
                            }
                            // အခြေအနေ ၃: Format မပါလျှင် (Server စာသားသက်သက်)
                            else
                            {
                                AddMessage("System", msg, false);
                            }
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Receive Error: " + ex.Message);
        }
    }

    private void UpdateUserList(string message)
    {
        // USERLIST|Name1:True,Name2:False ပုံစံဖြင့် ရောက်လာသည်ဟု ယူဆပါသည်
        int index = message.IndexOf("USERLIST|");
        if (index == -1) return;

        string listPart = message.Substring(index).Split('\n')[0].Replace("USERLIST|", "");
        string[] userEntries = listPart.Split(',');

        Dispatcher.UIThread.InvokeAsync(() =>
        {
            _onlineUsers.Clear();

            // "All" ကို Object အနေဖြင့် ထည့်ပါ
            _onlineUsers.Add(new UserStatusItem { UserName = "All", IsOnline = true });

            foreach (var entry in userEntries)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;

                string[] parts = entry.Split(':'); // Name:Status ပုံစံ ခွဲထုတ်ခြင်း
                string name = parts[0].Trim();
                bool isOnline = parts.Length > 1 && parts[1].Trim().ToLower() == "true";

                if (name != _userName)
                {
                    // string အစား Object အသစ်ကို ထည့်ခြင်း (CS1503 Error ပျောက်သွားပါမည်)
                    _onlineUsers.Add(new UserStatusItem { UserName = name, IsOnline = isOnline });
                }
            }
        });
    }

    public void OnUserSelected(object sender, SelectionChangedEventArgs e)
    {
        if (UserList.SelectedItem is UserStatusItem selectedUser)
        {
            _targetUser = selectedUser.UserName; // Object အစစ်ကနေ နာမည်ကို ယူပါ
            Console.WriteLine("Target changed to: " + _targetUser);
        }
    }

    public void OnSendClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(MessageInput.Text))
        {
            // ဥပမာ - All|YourName|Hello
            string formattedMessage = $"{_targetUser}|{_userName}|{MessageInput.Text}";
            SendMessage(formattedMessage);

            AddMessage("Me", MessageInput.Text, true);
            MessageInput.Text = "";
        }
    }

    private void SendMessage(string message)
    {
        try
        {
            if (clientStream != null && clientStream.CanWrite)
            {
                // ဒီနေရာမှာ + "\n" ထည့်ပေးလိုက်ပါ
                byte[] data = Encoding.UTF8.GetBytes(message + "\n");
                clientStream.Write(data, 0, data.Length);
                clientStream.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Send Error: " + ex.Message);
        }
    }
    
}

