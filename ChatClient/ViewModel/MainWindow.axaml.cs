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
using Avalonia.Data.Converters;
using System.Globalization;

namespace ChatClient;

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
        ToggleModeButton.Content = isRegisterMode ? "Already have an account? Login" : "Don't have an account? Register";
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
                Alignment = isMine ? "Right" : "Left",
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
                    Dispatcher.UIThread.InvokeAsync(() => {
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
            byte[] buffer = new byte[1024];
            while (_isListening && _stream != null)
            {
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Dispatcher.UIThread.Post(() =>
                    {
                        if (message.Contains("USERLIST|"))
                        {
                            UpdateUserList(message);
                        }
                        else
                        {
                            var parts = message.Split(':', 2);
                            if (parts.Length == 2)
                                AddMessage(parts[0].Trim(), parts[1].Trim(), false);
                            else
                                AddMessage("Server", message, false);
                        }
                    });
                }
            }
        }
        catch { }
    }

    private void UpdateUserList(string message)
    {
        // USERLIST|Name1:True,Name2:False ပုံစံဖြင့် ရောက်လာသည်ဟု ယူဆပါသည်
        int index = message.IndexOf("USERLIST|");
        if (index == -1) return; 

        string listPart = message.Substring(index).Split('\n')[0].Replace("USERLIST|", "");
        string[] userEntries = listPart.Split(',');

        Dispatcher.UIThread.InvokeAsync(() => {
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
        if (UserList.SelectedItem != null)
        {
            _targetUser = UserList.SelectedItem.ToString() ?? "All";
        }
    }

    public void OnSendClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(MessageInput.Text))
        {
            string formattedMessage = $"{_targetUser}|{_userName}|{MessageInput.Text}";
            SendMessage(formattedMessage);

            string targetDisplay = (_targetUser == "All") ? "Everyone" : _targetUser;
            AddMessage($"Me (to {targetDisplay})", MessageInput.Text, true);

            MessageInput.Text = "";
        }
    }

    private void SendMessage(string message)
    {
        try
        {
            if (clientStream != null && clientStream.CanWrite)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                clientStream.Write(data, 0, data.Length);
            }
            else
            {
                // Server ပြတ်သွားရင် UI မှာ ပြပေးပါ
                Dispatcher.UIThread.InvokeAsync(() => {
                    // ဥပမာ - User ကို Server ပြတ်နေကြောင်း Notification ပေးပါ
                });
            }
        }
        catch (IOException)
        {
            // Broken pipe ဆိုတာ ဒီနေရာမှာ ဖမ်းမိပါလိမ့်မယ်
            Console.WriteLine("Connection to server was lost.");
        }
    }
}

 // Chat Bubble အတွက် လိုအပ်သော Data Class
    // Message Model
    public class ChatMessage
    {
        public string Sender { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Alignment { get; set; } = "Left";
        public string BubbleColor { get; set; } = "#FFFFFF";
        public Thickness Margin { get; set; }
    }
