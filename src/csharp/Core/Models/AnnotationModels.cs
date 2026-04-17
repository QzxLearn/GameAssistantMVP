// GameAssistant\src\csharp\Core\Models\AnnotationModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace GameAssistant.Core.Models
{
    /// <summary>
    /// 标注区域（Bounding Box）
    /// </summary>
    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        // ICDAR 格式：x1,y1,x2,y2,x3,y3,x4,y4
        public int[] ToIcdarFormat()
        {
            return new[]
            {
                X, Y,
                X + Width, Y,
                X + Width, Y + Height,
                X, Y + Height
            };
        }

        public string ToIcdarString()
        {
            var points = ToIcdarFormat();
            return string.Join(",", points);
        }
    }

    /// <summary>
    /// 文本标注项
    /// </summary>
    public class TextAnnotation : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N")[..8];
        private BoundingBox _box = new();
        private string _text = string.Empty;
        private int _confidence = 100;
        private bool _isVerified = false;
        private string _category = "text";
        private bool _isSelected = false;

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public BoundingBox Box
        {
            get => _box;
            set
            {
                _box = value;
                OnPropertyChanged(nameof(Box));
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public int Confidence
        {
            get => _confidence;
            set
            {
                _confidence = value;
                OnPropertyChanged(nameof(Confidence));
            }
        }

        public bool IsVerified
        {
            get => _isVerified;
            set
            {
                _isVerified = value;
                OnPropertyChanged(nameof(IsVerified));
            }
        }

        public string Category
        {
            get => _category;
            set
            {
                _category = value;
                OnPropertyChanged(nameof(Category));
            }
        }

        [JsonIgnore]
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /// <summary>
    /// 完整卡牌标注数据
    /// </summary>
    public class CardAnnotation
    {
        public string Timestamp { get; set; } = string.Empty;
        public string GameType { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int OverallConfidence { get; set; } = 100;
        public bool IsVerified { get; set; } = false;
        public List<TextAnnotation> TextRegions { get; set; } = new();
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 标注会话状态
    /// </summary>
    public class AnnotationSession
    {
        public string CurrentImagePath { get; set; } = string.Empty;
        public List<TextAnnotation> Annotations { get; set; } = new();
        public string SelectedAnnotationId { get; set; } = string.Empty;
        public bool IsDrawing { get; set; } = false;
        public BoundingBox? CurrentBox { get; set; }
    }
}