using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using project.ModelsDto;
using project.roles;
using project.users;
using project.users.Models;
using project.utils.catalogue;
namespace project.Models;

public class DBProyContext(DbContextOptions<DBProyContext> options) : IdentityDbContext<userEntity, rolEntity, string>(options)
{
    public DbSet<binnacleBody> BinnacleBodies => Set<binnacleBody>();
    public DbSet<binnacleHeader> BinnacleHeaders => Set<binnacleHeader>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Catalogue> Catalogues => Set<Catalogue>();
    public DbSet<catalogueType> catalogueTypes => Set<catalogueType>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Client>().HasIndex(c => c.userId).IsUnique();
        modelBuilder.Entity<Client>().HasOne(c => c.user).WithOne().HasForeignKey<Client>(c => c.userId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<catalogueType>().HasIndex(c => c.code).IsUnique();
        modelBuilder.Entity<Catalogue>().HasOne(c => c.catalogueParent).WithMany().HasForeignKey(c => c.catalogueParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
