using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using MiniCPQ.Infrastructure;
using MiniCPQ.Infrastructure.Data;
using MiniCPQ.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mini CPQ API",
        Version = "v1",
        Description = "服务器配置与报价系统。先调用 /api/auth/login，Swagger 会继续携带登录 Cookie。"
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("缺少 ConnectionStrings:DefaultConnection 配置。");
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

await DatabaseInitializer.InitializeAsync(app.Services);
await app.RunAsync();

public partial class Program;
