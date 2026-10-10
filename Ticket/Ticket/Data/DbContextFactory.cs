using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;


 // Thay bằng namespace dự án của bạn

using Ticket.Data;
namespace Ticket.Data // Thay bằng namespace dự án của bạn

{
    // Thay 'ApplicationDbContext' bằng tên class DbContext thực tế của bạn
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> 
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Cấu hình Database Provider và chuỗi kết nối tạm cho EF CLI
            optionsBuilder.UseSqlite("Data Source=ticket.db");   // ✅

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}