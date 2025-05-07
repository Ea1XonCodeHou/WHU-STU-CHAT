using backend.Hubs;
using backend.Services;
using MySql.Data.MySqlClient;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加SignalR服务
builder.Services.AddSignalR();

// 配置CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.SetIsOriginAllowed(_ => true) // 在开发阶段允许任何来源，生产环境应当限制
            .AllowAnyMethod() // 明确允许所有HTTP方法，包括PUT、POST、DELETE
            .AllowAnyHeader()
            .AllowCredentials());
});

// 注册服务
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>(); // 添加讨论区服务

// 为AI服务添加HttpClient
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    // 配置HttpClient超时时间
    client.Timeout = TimeSpan.FromSeconds(30);
});

// 测试数据库连接
using (var connection = new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    try
    {
        connection.Open();
        Console.WriteLine("数据库连接成功");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"数据库连接失败：{ex.Message}");
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 使用CORS
app.UseCors("CorsPolicy");

// 配置静态文件服务
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
// 映射SignalR Hub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<GroupChatHub>("/groupChatHub");
app.MapHub<PrivateChatHub>("/privateChatHub");

app.Run();
