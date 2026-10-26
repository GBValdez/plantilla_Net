using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using project.Models;
using project.roles;
using project.users;
using project.utils.dto;
using project.utils.services;
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace PanelBase.Api.Tests;

public class RecordingEmail(Microsoft.Extensions.Configuration.IConfiguration config) : emailService(config)
{
    public List<emailSendDto> Messages { get; } = [];
    public bool FailDelivery { get; set; }
    public override Task SendEmailAsync(emailSendDto data, CancellationToken cancellationToken = default) {
        if (FailDelivery) throw new InvalidOperationException("Simulated SMTP failure");
        Messages.Add(data); return Task.CompletedTask;
    }
}
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string Key = "test-only-random-signing-key-12345678901234567890";
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public ApiFactory() {
        Environment.SetEnvironmentVariable("Jwt__Key", Key);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "PanelBase.Api");
        Environment.SetEnvironmentVariable("Jwt__Audience", "PanelBase.Web");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=unused;Database=unused");
        Environment.SetEnvironmentVariable("FrontUrl", "http://localhost:4200");
        connection.Open();
    }
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment("Development");
        builder.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../")));
        builder.ConfigureServices(services => {
            services.RemoveAll<DbContextOptions<DBProyContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<DBProyContext>>();
            services.AddDbContext<DBProyContext>(o => o.UseSqlite(connection).AddInterceptors(new interceptorDb(new Microsoft.AspNetCore.Http.HttpContextAccessor())));
            services.RemoveAll<emailService>();
            services.AddSingleton<RecordingEmail>();
            services.AddSingleton<emailService>(sp => sp.GetRequiredService<RecordingEmail>());
        });
    }
    public async Task InitializeAsync() {
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DBProyContext>().Database.EnsureCreatedAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<rolEntity>>();
        foreach (var role in new[] { "ADMINISTRATOR", "userNormal" }) await roles.CreateAsync(new rolEntity { Name = role });
    }
    public async Task<userEntity> AddUserAsync(string name = "member", string role = "userNormal", bool confirmed = true) {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<userEntity>>();
        var user = new userEntity { UserName = name, Email = name + "@example.com", EmailConfirmed = confirmed };
        var result = await users.CreateAsync(user, "Strong!Pass123");
        Assert.True(result.Succeeded, string.Join(",", result.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);
        return user;
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
