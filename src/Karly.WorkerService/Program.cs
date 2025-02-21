using Karly.Application.Extensions;
using Karly.Application.Options;
using Karly.WorkerService;
using Karly.WorkerService.Services;

var builder = Host.CreateApplicationBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.Services.AddPostgresDbContext(builder.Configuration);
builder.Services.AddServices(builder.Configuration);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConsumerService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IServiceScopeFactory>(provider => provider.GetRequiredService<IServiceScopeFactory>());

var host = builder.Build();
host.Run();