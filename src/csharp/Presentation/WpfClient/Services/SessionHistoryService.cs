using GameAssistant.Core.Models;
using GameAssistant.Infrastructure.Storage.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.WpfClient.Services
{
    public class SessionHistoryService
    {
        private readonly AppDbContext _dbContext;
        private readonly TrainingDataService _trainingDataService;

        public SessionHistoryService(AppDbContext dbContext, TrainingDataService trainingDataService)
        {
            _dbContext = dbContext;
            _trainingDataService = trainingDataService;
        }

        // 获取最近 N 条会话记录
        public async Task<List<GameSessionRecord>> GetRecentSessions(int count = 10)
        {
            return await _dbContext.GameSessions
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        // 获取所有标注数据
        public List<CardAnnotation> GetAllAnnotations()
        {
            var files = _trainingDataService.GetAllAnnotationFiles();
            var annotations = new List<CardAnnotation>();
            foreach (var file in files)
            {
                var timestamp = Path.GetFileNameWithoutExtension(file);
                var annotation = _trainingDataService.LoadCardAnnotation(timestamp);
                if (annotation != null)
                    annotations.Add(annotation);
            }
            return annotations.OrderByDescending(a => a.CreatedAt).ToList();
        }

        // 更新会话记录
        public async Task UpdateSessionAsync(GameSessionRecord record)
        {
            _dbContext.GameSessions.Update(record);
            await _dbContext.SaveChangesAsync();
        }

        // 删除会话记录
        public async Task DeleteSessionAsync(int id)
        {
            var record = await _dbContext.GameSessions.FindAsync(id);
            if (record != null)
            {
                _dbContext.GameSessions.Remove(record);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
