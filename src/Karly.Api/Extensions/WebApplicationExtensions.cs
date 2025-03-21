using Karly.Application.Database;
using Karly.Application.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Karly.Api.Extensions;

public static class WebApplicationExtensions
{
    public static void SetupDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
    
        var services = scope.ServiceProvider;
        var dbContext = services.GetService<KarlyDbContext>();
    
        if (dbContext == null)
        {
            return;
        }
        
        app.Logger.LogInformation("Executing migrations.");
        dbContext.Database.Migrate();
    
        app.Logger.LogInformation("Ensuring created.");
        dbContext.Database.EnsureCreated();

        if (app.Environment.IsDevelopment())
        {
            app.Logger.LogInformation("Seeding database.");
            dbContext.EnsureSeedData(app.Logger);
        }
    }
}
