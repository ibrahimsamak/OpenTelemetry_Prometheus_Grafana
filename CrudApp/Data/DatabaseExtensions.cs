using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CrudApp.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddAppDatabase(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default") ?? "Data Source=data/app.db";

        // SQLite does not create missing folders, so create the folder that holds the .db file.
        var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connectionString));
        return services;
    }
}
