using Avalonia;
using Avalonia.Layout;

namespace ChatClient.Models;
public class ChatMessage
{
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;
    public string BubbleColor { get; set; } = "#FFFFFF";
    public Thickness Margin { get; set; }
}