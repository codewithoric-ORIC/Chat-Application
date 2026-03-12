using System.Collections.ObjectModel;

namespace ChatClient.Models;

public class UserStatusItem
{
    // ဒီနာမည်က XAML က {Binding UserName} နဲ့ တူရပါမယ်
    public string UserName { get; set; } = ""; 
    public bool IsOnline { get; set; }

    // private ObservableCollection<UserStatusItem> _onlineUsers;
}