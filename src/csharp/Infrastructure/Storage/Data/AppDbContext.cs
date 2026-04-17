using GameAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace GameAssistant.Infrastructure.Storage.Data;

public class AppDbContext : DbContext
{
    public DbSet<GameSessionRecord> GameSessions { get; set; }

    private readonly string _dbPath;

    // ��������ʱ��ָ�����ݿ�·����
    public AppDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }

    // ���� EF ���ʱ���ߣ��������޲ι��캯����ʹ�� IDesignTimeDbContextFactory��
    protected AppDbContext() { _dbPath = "game_assistant.db"; }

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
