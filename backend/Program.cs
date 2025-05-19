using backend.Hubs;
using backend.Services;
using backend.Utils;
using MySql.Data.MySqlClient;
using System.IO;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加SignalR服务
builder.Services.AddSignalR();

// 添加身份验证服务
// builder.Services.AddAuthentication("Bearer")
//     .AddJwtBearer("Bearer", options =>
//     {
//         options.RequireHttpsMetadata = false;
//         options.SaveToken = true;
//         options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
//         {
//             ValidateIssuerSigningKey = true,
//             IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
//                 System.Text.Encoding.ASCII.GetBytes("whu-chat-super-secret-key-for-jwt-auth-2024")),
//             ValidateIssuer = false,
//             ValidateAudience = false,
//             ClockSkew = TimeSpan.Zero
//         };
//     });

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
builder.Services.AddScoped<IFriendshipService, FriendshipService>(); // 添加好友关系服务
builder.Services.AddScoped<IMembershipService, MembershipService>(); // 添加会员服务
builder.Services.AddScoped<AliOSSHelper>(); // 添加阿里云OSS助手服务

// 为AI服务添加HttpClient
builder.Services.AddHttpClient<IAIService, AIService>(client =>
{
    // 配置HttpClient超时时间，增加到300秒以处理大量聊天记录
    client.Timeout = TimeSpan.FromSeconds(300);
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

// 添加身份验证中间件
// app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
// 映射SignalR Hub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<GroupChatHub>("/groupChatHub");
app.MapHub<PrivateChatHub>("/privateChatHub");

app.Run();
