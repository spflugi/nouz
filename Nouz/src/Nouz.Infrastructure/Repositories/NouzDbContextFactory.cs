using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NouzDbContextFactory : IDesignTimeDbContextFactory<NouzDbContext>
{
    public NouzDbContext CreateDbContext(string[] args)
    {
        // Only used at design-time for ef tools, e.g. when creating migrations
        var options = new DbContextOptionsBuilder<NouzDbContext>()
            .UseSqlite("Data Source=nouz.db")
            .Options;

        return new NouzDbContext(options);
    }
}