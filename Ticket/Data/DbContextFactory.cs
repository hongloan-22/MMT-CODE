using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ticket.Data;

/// <summary>Factory dùng khi tạo migration SQLite bằng dotnet ef.</summary>
public sealed class DbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=ticket-sqlite.db;Cache=Shared")
            .Options;

        return new ApplicationDbContext(options);
    }
}
