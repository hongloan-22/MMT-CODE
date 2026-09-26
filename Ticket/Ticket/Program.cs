using Microsoft.EntityFrameworkCore;
using Ticket.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers + Views
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình Database InMemory phục vụ kiểm thử
bool useInMemory = builder.Configuration.GetValue<bool>("UseInMemory", true);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (useInMemory)
    {
        options.UseInMemoryDatabase("TicketDb");
    }
    else
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    }
});

var app = builder.Build();

// Khởi tạo Database và nạp Seed Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (useInMemory)
    {
        db.Database.EnsureCreated();
    }
    else
    {
        try
        {
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Warning] Bỏ qua lỗi Migrate CSDL Local: {ex.Message}");
        }
    }
}

// Kích hoạt Swagger UI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ticket API v1");
    c.RoutePrefix = "swagger";
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();