using backend.Hubs;
using backend.Services;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 添加SignalR服务
builder.Services.AddSignalR();

// 添加CORS策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:5000", "http://127.0.0.1:5173", "http://127.0.0.1:5174") // 允许多个前端地址
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// 注册服务
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();

// 验证数据库连接
using (var connection = new MySqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    try
    {
        connection.Open();
        Console.WriteLine("数据库连接成功！");
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

app.UseAuthorization();

app.MapControllers();
// 映射SignalR Hub
app.MapHub<ChatHub>("/chatHub");

app.Run();
