namespace ChatClient;

public class UserStatusItem
{
    // ဒီနာမည်က XAML က {Binding UserName} နဲ့ တူရပါမယ်
    public string UserName { get; set; } = ""; 
    public bool IsOnline { get; set; }
}