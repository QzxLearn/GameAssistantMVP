using GameAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameAssistant.Core.Data;

public class AppDbContext : DbContext
{
    public DbSet<GameSessionRecord> GameSessions { get; set; }

    private readonly string _dbPath;

    // 用于运行时（指定数据库路径）
    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    // 用于 EF 设计时工具（必须有无参构造函数或使用 IDesignTimeDbContextFactory）
    protected AppDbContext() { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameSessionRecord>(entity =>
        {
            entity.ToTable("game_sessions");
            entity.Property(e => e.GameStateJson).IsRequired();
        });
    }
}