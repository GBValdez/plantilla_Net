using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using project.utils.interfaces;
namespace project.utils.services;

public class interceptorDb(IHttpContextAccessor accessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData); return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData); return base.SavingChanges(eventData, result);
    }
    private void ApplyAudit(DbContextEventData data)
    {
        if (data.Context is null) return;
        var id = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        foreach (var entry in data.Context.ChangeTracker.Entries<ICommonModelHeader>())
        {
            if (entry.State == EntityState.Added) entry.Entity.createAt = DateTime.UtcNow;
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.userUpdateId = id; entry.Entity.updateAt = DateTime.UtcNow;
            }
        }
    }
}
