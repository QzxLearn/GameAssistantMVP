using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace GameAssistant.Core.Data;

// 仅用于 EF Core 工具（dotnet ef migrations add ...）
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 使用本地开发路径（例如 bin/Debug/net8.0/game_memory.db）
        var path = Path.Combine(Directory.GetCurrentDirectory(), "game_memory.db");
        return new AppDbContext(path);
    }
}