using Microsoft.EntityFrameworkCore;

namespace ReceiptScanner.Data
{
    public class DbConnection : DbContext
    {
    public DbConnection(DbContextOptions<DbConnection> options)
    : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
