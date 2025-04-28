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

// ����CORS����
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175", "http://localhost:5000", "http://127.0.0.1:5173", "http://127.0.0.1:5174") // �������ǰ�˵�ַ
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ע�����
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGroupService, GroupService>();

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
