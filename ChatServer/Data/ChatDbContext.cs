using Microsoft.EntityFrameworkCore;
using ChatServer.Models; // သင့်ရဲ့ User, Message class တွေရှိတဲ့ folder ကို import လုပ်ပါ

namespace ChatServer.Data
{
    public class ChatDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Connection> Connections { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // MySQL သုံးရန်
            string connectionString = "server=localhost;database=ChatAppDB;user=root;password=admin@123";
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}