using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GameAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace GameAssistant.Infrastructure.AI;

public class AdviceRequest
{
    [JsonPropertyName("game_state")]
    public GameStateDto GameState { get; set; } = null!;
}

public class GameStateDto
{
    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = "";

    [JsonPropertyName("floor")]
    public int Floor { get; set; }

    [JsonPropertyName("act")]
    public int Act { get; set; }

    [JsonPropertyName("player_hp")]
    public int PlayerHp { get; set; }

    [JsonPropertyName("player_max_hp")]
    public int PlayerMaxHp { get; set; }

    [JsonPropertyName("player_block")]
    public int PlayerBlock { get; set; }

    [JsonPropertyName("player_gold")]
    public int PlayerGold { get; set; }

    [JsonPropertyName("energy_current")]
    public int EnergyCurrent { get; set; }

    [JsonPropertyName("energy_max")]
    public int EnergyMax { get; set; }

    [JsonPropertyName("hand")]
    public List<CardDto> Hand { get; set; } = new();

    [JsonPropertyName("enemies")]
    public List<EnemyDto> Enemies { get; set; } = new();

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = "";
}

public class CardDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";
}

public class EnemyDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("hp")]
    public int CurrentHp { get; set; }

    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("intent_damage")]
    public int? IntentDamage { get; set; }
}

public class AdviceResponse
{
    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = "";

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = "";
}

public class AdviceClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly ILogger<AdviceClient> _logger;

    public AdviceClient(string baseUrl, ILogger<AdviceClient> logger)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _logger = logger;
    }

    public async Task<AdviceResponse?> GetAdviceAsync(SlayTheSpireGameState state, CancellationToken ct = default)
    {
        try
        {
            var dto = MapToDto(state);
            var request = new AdviceRequest { GameState = dto };

            var response = await _http.PostAsJsonAsync($"{_baseUrl}/advice", request, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<AdviceResponse>(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Python Brain HTTP call failed: {Message}", ex.Message);
            return null;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Python Brain request timed out: {Message}", ex.Message);
            return null;
        }
    }

    public void Dispose() => _http.Dispose();

    private static GameStateDto MapToDto(SlayTheSpireGameState state) => new()
    {
        GameName = state.GameName,
        Floor = state.Floor,
        Act = state.Act,
        PlayerHp = state.Player.CurrentHp,
        PlayerMaxHp = state.Player.MaxHp,
        PlayerBlock = state.Player.Block,
        PlayerGold = state.Player.Gold,
        EnergyCurrent = state.Energy.current,
        EnergyMax = state.Energy.max,
        Phase = state.Phase.ToString(),
        Hand = state.Hand.Select(c => new CardDto
        {
            Name = c.Name,
            Cost = c.Cost,
            Type = c.Type.ToString()
        }).ToList(),
        Enemies = state.Enemies.Select(e => new EnemyDto
        {
            Name = e.Name,
            CurrentHp = e.CurrentHp,
            Intent = e.Intent?.ToString(),
            IntentDamage = e.IntentDamage
        }).ToList()
    };
}
