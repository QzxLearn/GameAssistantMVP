using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace GameAssistant.Infrastructure.Storage.Data;

// 仅用于 EF Core 工具（dotnet ef migrations add ...）
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "game_memory.db"
        );

        return new AppDbContext(path);
    }
}
