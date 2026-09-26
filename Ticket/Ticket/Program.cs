using Microsoft.EntityFrameworkCore;
using Ticket.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// 1. Đăng ký dịch vụ Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Đọc cấu hình UseInMemory từ appsettings (Mặc định là false nếu không khai báo)
bool useInMemory = builder.Configuration.GetValue<bool>("UseInMemory");

// Đăng ký ApplicationDbContext (Tự động chuyển đổi giữa InMemory và SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("TicketMockDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

var app = builder.Build();

// 2. Kích hoạt Middleware Swagger & Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket API v1");
    c.RoutePrefix = "swagger"; // Đường dẫn truy cập: /swagger
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Tự động cập nhật Database mỗi khi khởi chạy
if (!useInMemory)
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            // Nếu CSDL local bị trùng bảng hoặc lỗi migration, bỏ qua để app vẫn khởi chạy bình thường
            Console.WriteLine($"[Warning] Bỏ qua lỗi Migrate CSDL Local: {ex.Message}");
        }
    }
}

app.Run();