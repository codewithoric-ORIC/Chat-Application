namespace ChatServer.Models;
public class Connection {
    public int ConnectionID { get; set; }
    public int UserID { get; set; }
    public string IP_Address { get; set; } = string.Empty;
    public int PortNumber { get; set; }
}