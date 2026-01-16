using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;

namespace Nouz.Infrastructure.Repositories;

public class NouzDbContext : DbContext
{
    public DbSet<Notebook> Notebooks { get; set; } = null!;

    public NouzDbContext(DbContextOptions<NouzDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notebook>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Name).IsRequired();
            n.Property(x => x.CreatedAt).IsRequired();
            n.Property(x => x.LastModifiedAt).IsRequired();
            n.Property(x => x.SortOrder);
        });
    }
}