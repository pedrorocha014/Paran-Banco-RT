using Core.CustomerAggregate;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registrar o enum do PostgreSQL com valores em minúscula
        modelBuilder.HasPostgresEnum("proposal_status", new[] { "created", "approved", "denied", "completed" });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);

            entity.HasMany(e => e.Proposals)
                .WithOne(p => p.Customer)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Cards)
                .WithOne(c => c.Customer)
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Proposal>(entity =>
        {
            entity.ToTable("Proposals");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerId)
                .IsRequired();

            entity.Property(e => e.Status)
                .IsRequired()
                .HasColumnType("proposal_status");

            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => e.CustomerId);

            // Relacionamento 1:1 com Card
            entity.HasOne(p => p.Card)
                .WithOne(c => c.Proposal)
                .HasForeignKey<Card>(c => c.ProposalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("Cards");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerId)
                .IsRequired();

            entity.Property(e => e.ProposalId)
                .IsRequired();

            entity.Property(e => e.Limit)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.ProposalId)
                .IsUnique();
        });
    }
}


