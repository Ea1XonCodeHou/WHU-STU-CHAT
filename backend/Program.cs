using backend.Hubs;
using backend.Services;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ����SignalR����
builder.Services.AddSignalR();

// 配置CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.SetIsOriginAllowed(_ => true) // 在开发阶段允许任何来源，生产环境应当限制
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ע�����
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddHttpClient(); // 为AI服务添加HttpClient
builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
// ��֤���ݿ�����
using (var connection = new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    try
    {
        connection.Open();
        Console.WriteLine("���ݿ����ӳɹ���");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"���ݿ�����ʧ�ܣ�{ex.Message}");
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

// ʹ��CORS
app.UseCors("CorsPolicy");

app.UseAuthorization();

app.MapControllers();
// ӳ��SignalR Hub
app.MapHub<ChatHub>("/chatHub");
app.MapHub<GroupChatHub>("/groupChatHub");

app.Run();
