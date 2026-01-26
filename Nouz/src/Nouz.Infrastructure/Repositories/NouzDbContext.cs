using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using System.Text.Json;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NouzDbContext : DbContext
{
    public DbSet<Note> Notes { get; set; } = null!;
    public DbSet<Notebook> Notebooks { get; set; } = null!;
    public DbSet<NoteEmbedding> NoteEmbeddings { get; set; } = null!;
    public DbSet<NoteAttachment> NoteAttachments { get; set; } = null!;

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
            n.Property(x => x.Order);
            n.Property<Guid>("NoteId")
                .IsRequired();
            n.HasIndex("NoteId");
            n.HasOne<Note>()
                .WithMany()
                .HasForeignKey("NoteId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoteEmbedding>(e =>
        {
            e.HasKey(x => x.NoteId);
            e.Property(x => x.Embedding)
                .HasConversion(
                    v => FloatArrayToBytes(v),
                    v => BytesToFloatArray(v))
                .IsRequired();
            e.Property(x => x.LastUpdatedAt)
                .IsRequired();
            e.Ignore(x => x.Note);
            e.HasOne<Note>()
                .WithOne()
                .HasForeignKey<NoteEmbedding>(x => x.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NoteAttachment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FileName).IsRequired();
            e.Property(x => x.Extension).IsRequired();
            e.Property(x => x.FileSizeBytes).IsRequired();
            e.Property(x => x.ContentHash).IsRequired();
            e.Property(x => x.AddedAt).IsRequired();
            e.Ignore(x => x.Note);
            e.HasOne<Note>()
                .WithMany()
                .HasForeignKey(x => x.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.NoteId);
            e.HasIndex(x => new { x.NoteId, x.ContentHash });
        });
    }

    private static byte[] FloatArrayToBytes(float[] floats)
    {
        var bytes = new byte[floats.Length * sizeof(float)];
        for (var i = 0; i < floats.Length; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * sizeof(float)), floats[i]);
        }
        return bytes;
    }

    private static float[] BytesToFloatArray(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        for (var i = 0; i < floats.Length; i++)
        {
            floats[i] = BinaryPrimitives.ReadSingleLittleEndian(bytes.AsSpan(i * sizeof(float)));
        }
        return floats;
    }
}