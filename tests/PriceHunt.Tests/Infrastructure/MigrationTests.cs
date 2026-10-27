using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace PriceHunt.Tests.Infrastructure;

public sealed class MigrationTests : DatabaseTestBase
{
    [Fact]
    public async Task A_new_database_is_brought_up_to_date_by_the_migrations()
    {
        var context = NewContext();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public void The_migrations_match_the_model_so_no_change_is_missing_a_migration()
    {
        var context = NewContext();
        var migrations = context.GetService<IMigrationsAssembly>();
        var snapshot = context.GetService<IModelRuntimeInitializer>().Initialize(migrations.ModelSnapshot!.Model);

        var differences = context.GetService<IMigrationsModelDiffer>().GetDifferences(
            snapshot.GetRelationalModel(),
            context.GetService<IDesignTimeModel>().Model.GetRelationalModel());

        Assert.Empty(differences);
    }
}
