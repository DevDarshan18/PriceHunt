using Microsoft.EntityFrameworkCore;
using PriceHunt.Infrastructure.Persistence.Entities;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// The database: places, searches, the suppliers each search selected, and what each supplier answered.
/// </summary>
public sealed class PriceHuntDbContext : DbContext
{
    public PriceHuntDbContext(DbContextOptions<PriceHuntDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// The suppliers a search can ask.
    /// </summary>
    public DbSet<SupplierRecord> Suppliers => Set<SupplierRecord>();

    /// <summary>
    /// The places that can be chosen.
    /// </summary>
    public DbSet<LocationRecord> Locations => Set<LocationRecord>();

    /// <summary>
    /// One row per search.
    /// </summary>
    public DbSet<SearchRecord> Searches => Set<SearchRecord>();

    /// <summary>
    /// The suppliers selected for each search.
    /// </summary>
    public DbSet<SelectedSupplierRecord> SelectedSuppliers => Set<SelectedSupplierRecord>();

    /// <summary>
    /// What each supplier did in each search.
    /// </summary>
    public DbSet<SupplierOutcomeRecord> SupplierOutcomes => Set<SupplierOutcomeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var supplier = modelBuilder.Entity<SupplierRecord>();
        supplier.ToTable("Suppliers");
        supplier.HasKey(record => record.Id);
        supplier.Property(record => record.Name).UseCollation("NOCASE").HasMaxLength(100).IsRequired();
        supplier.Property(record => record.Behavior).HasConversion<string>().HasMaxLength(32).IsRequired();
        supplier.HasIndex(record => record.Name).IsUnique();

        var location = modelBuilder.Entity<LocationRecord>();
        location.ToTable("Locations");
        location.HasKey(record => record.Id);
        location.Property(record => record.Name).UseCollation("NOCASE").IsRequired();
        location.HasIndex(record => record.Name).IsUnique();

        var search = modelBuilder.Entity<SearchRecord>();
        search.ToTable("Searches");
        search.HasKey(record => record.Id);
        search.Property(record => record.Origin).IsRequired();
        search.Property(record => record.Destination).IsRequired();
        search.Property(record => record.Status).HasMaxLength(32).IsRequired();
        search.Property(record => record.CreatedAtUtc).HasConversion(
            value => value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        search.HasIndex(record => record.CreatedAtUtc);
        search.HasMany(record => record.SelectedSuppliers)
            .WithOne(supplier => supplier.Search)
            .HasForeignKey(supplier => supplier.SearchId)
            .OnDelete(DeleteBehavior.Cascade);
        search.HasMany(record => record.Outcomes)
            .WithOne(outcome => outcome.Search)
            .HasForeignKey(outcome => outcome.SearchId)
            .OnDelete(DeleteBehavior.Cascade);

        var selected = modelBuilder.Entity<SelectedSupplierRecord>();
        selected.ToTable("SearchSuppliers");
        selected.HasKey(record => new { record.SearchId, record.SupplierId });
        selected.Property(record => record.SupplierName).IsRequired();

        var outcome = modelBuilder.Entity<SupplierOutcomeRecord>();
        outcome.ToTable("SupplierOutcomes");
        outcome.HasKey(record => new { record.SearchId, record.SupplierId });
        outcome.Property(record => record.SupplierName).IsRequired();
        outcome.Property(record => record.Status).HasMaxLength(32).IsRequired();
        outcome.Property(record => record.ErrorCode).HasMaxLength(64);
        outcome.Property(record => record.Currency).HasMaxLength(3);
        outcome.HasIndex(record => record.SupplierId);
        outcome.Property(record => record.RespondedAtUtc).HasConversion(
            value => value,
            value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
    }
}
