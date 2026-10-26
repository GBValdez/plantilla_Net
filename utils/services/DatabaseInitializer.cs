using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using project.Models;
using project.roles;
using project.users;
namespace project.utils.services;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider provider, IConfiguration configuration)
    {
        using var scope = provider.CreateScope();
        var email = configuration["Bootstrap:AdminEmail"];
        var password = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Configura Bootstrap:AdminEmail y Bootstrap:AdminPassword para inicializar el administrador.");
        var db = scope.ServiceProvider.GetRequiredService<DBProyContext>();
        // Explicit command for a NEW template database; existing production databases need migrations.
        await db.Database.EnsureCreatedAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<rolEntity>>();
        foreach (var name in new[] { "ADMINISTRATOR", "userNormal" })
        {
            if (!await roles.RoleExistsAsync(name)) Check(await roles.CreateAsync(new rolEntity { Name = name }));
        }
        var users = scope.ServiceProvider.GetRequiredService<UserManager<userEntity>>();
        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new userEntity { UserName = "admin", Email = email, EmailConfirmed = true };
            Check(await users.CreateAsync(admin, password));
            Check(await users.AddToRoleAsync(admin, "ADMINISTRATOR"));
        }
        else if (!await users.IsInRoleAsync(admin, "ADMINISTRATOR"))
        {
            throw new InvalidOperationException("El correo ya pertenece a una cuenta sin rol de administrador. No se modificaron sus permisos.");
        }
        await transaction.CommitAsync();
    }
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
