using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CareFlow.Data;

public sealed class CareFlowDbContextFactory : IDesignTimeDbContextFactory<CareFlowDbContext>
{
    public CareFlowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CareFlowDbContext>();
        ConfigureSqlite(options);
        return new CareFlowDbContext(options.Options);
    }

    internal static void ConfigureSqlite(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(appData, "CareFlow.db"),
            ForeignKeys = true,
        };

        options.UseSqlite(connectionString.ToString());
    }
}
