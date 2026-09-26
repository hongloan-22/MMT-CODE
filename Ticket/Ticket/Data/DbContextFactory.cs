using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ticket.Data // Thay bằng namespace dự án của bạn
{
    // Thay 'ApplicationDbContext' bằng tên class DbContext thực tế của bạn
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext> 
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Cấu hình Database Provider và chuỗi kết nối tạm cho EF CLI
            optionsBuilder.UseSqlServer("Server=localhost;Database=TicketDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}