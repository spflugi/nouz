using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using System.Text.Json;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NouzDbContext : DbContext
{
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<Notebook> Notebooks { get; set; } = null!;

    public NouzDbContext(DbContextOptions<NouzDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notebook>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Name)
                .IsRequired();
            n.Property(x => x.CreatedAt)
                .IsRequired();
            n.Property(x => x.LastModifiedAt)
                .IsRequired();
            n.Property(x => x.SortOrder);

            n.HasIndex(x => x.Name);
            n.HasIndex(x => x.SortOrder);
            n.HasMany(x => x.Notes)
                .WithOne(x => x.Notebook)
                .HasForeignKey(x => x.NotebookId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Note>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.NotebookId)
                .IsRequired();
            n.Property(x => x.CreatedAt)
                .IsRequired();
            n.Property(x => x.LastModifiedAt)
                .IsRequired();
            n.HasOne(x => x.Notebook)
                .WithMany(x => x.Notes)
                .HasForeignKey(x => x.NotebookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Blocks are stored as entities
            n.Ignore(x => x.Blocks);
        });

        modelBuilder.Entity<Block>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Type)
                .HasConversion<string>()
                .IsRequired();
            n.Property(x => x.Content);
            n.Property(x => x.Metadata)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonSerializerOptions.Default) ??
                         new Dictionary<string, object>());
            n.Property<Guid>("NoteId")
                .IsRequired();
            n.HasIndex("NoteId");
            n.HasOne<Note>()
                .WithMany()
                .HasForeignKey("NoteId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlockSearch>(n =>
        {
            n.ToTable("BlockSearch");
            n.HasNoKey();
        });
    }
}