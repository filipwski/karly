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
#pragma warning disable SKEXP0010
builder.Services.AddServices(builder.Configuration);
#pragma warning restore SKEXP0010

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqConsumerService>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddSingleton<IServiceScopeFactory>(provider => provider.GetRequiredService<IServiceScopeFactory>());

var host = builder.Build();
host.Run();