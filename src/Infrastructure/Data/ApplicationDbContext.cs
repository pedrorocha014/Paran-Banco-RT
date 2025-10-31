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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Registrar o enum do PostgreSQL
        modelBuilder.HasPostgresEnum<ProposalStatus>("proposal_status");

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
        });
    }
}


