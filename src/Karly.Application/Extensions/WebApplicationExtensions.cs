// using Karly.Application.Database;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
//
// namespace Karly.Application.Extensions;
//
// public static class WebApplicationExtensions
// {
//     public static WebApplication SetupDatabase(this WebApplication app)
//     {
//         using var scope = app.Services.CreateScope();
//     
//         var services = scope.ServiceProvider;
//         var dbContext = services.GetService<KarlyDbContext>();
//     
//         if (dbContext == null)
//         {
//             return app;
//         }
//     
//         app.Logger.LogInformation("Executing migrations.");
//         dbContext.
//     
//         return app;
//     }
// }
