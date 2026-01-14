using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameAssistant.Core.Models;

public class GameSessionRecord
{
    [Key]
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public string GameName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "TEXT")]
    public string GameStateJson { get; set; } = string.Empty;
}