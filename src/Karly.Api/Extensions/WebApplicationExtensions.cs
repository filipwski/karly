using Karly.Application.Database;
using Microsoft.EntityFrameworkCore;

namespace Karly.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication SetupDatabase(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
    
        var services = scope.ServiceProvider;
        var dbContext = services.GetService<KarlyDbContext>();
    
        if (dbContext == null)
        {
            return app;
        }
        
        app.Logger.LogInformation("Ensuring created.");
        dbContext.Database.EnsureCreated();
    
        app.Logger.LogInformation("Executing migrations.");
        dbContext.Database.Migrate();
    
        return app;
    }
}
