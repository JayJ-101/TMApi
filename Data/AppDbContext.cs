using Microsoft.EntityFrameworkCore;
using TMApi.Models;

namespace TMApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
           : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }
    }
}
