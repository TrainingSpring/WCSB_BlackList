using Microsoft.EntityFrameworkCore;
namespace WCSB_Black_LIst_API.Models
{
    public class BlackListContext:DbContext
    {
        public BlackListContext(DbContextOptions<BlackListContext> options) : base(options)
        {
        }

        public DbSet<BlackList> BlackList { get; set; }
    }
}