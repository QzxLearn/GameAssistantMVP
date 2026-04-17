using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GameAssistant.Core.Enums;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;

namespace GameAssistant.Infrastructure.Ocr;

/// <summary>
/// Parses OCR text into structured game state
/// </summary>
public class GameStateParser : IGameStateParser
{
    private readonly IOcrService _ocrService;

    public string GameName => "GenericGame";

    public GameStateParser(IOcrService ocrService)
    {
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
    }

    public GameState Parse(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return CreateEmptyState();

        try
        {
            var state = new ParsedGameState
            {
                GameName = GameName,
                Timestamp = DateTime.UtcNow,
                RawOcrText = ocrText,
                RecognizedText = ocrText,
                PlayerHp = ParsePlayerHp(ocrText),
                Gold = ParseGold(ocrText),
                DeckCount = ParseDeckCount(ocrText),
                HandCards = ParseHandCards(ocrText),
                EnemyType = ParseEnemyType(ocrText)
            };
            return state;
        }
        catch (Exception ex)
        {
            var errorState = CreateEmptyState();
            errorState.RawOcrText = $"Parse error: {ex.Message}";
            return errorState;
        }
    }

    public T Parse<T>(string ocrText) where T : GameState => (T)Parse(ocrText);

    public GameState ParseFromImage(byte[] imageBytes, OcrMode mode = OcrMode.Generic)
    {
        try
        {
            var ocrText = _ocrService.RecognizeFromBytes(imageBytes, mode);
            return Parse(ocrText);
        }
        catch (Exception ex)
        {
            var errorState = CreateEmptyState();
            errorState.RawOcrText = $"OCR error: {ex.Message}";
            return errorState;
        }
    }

    private static ParsedGameState CreateEmptyState()
    {
        return new ParsedGameState
        {
            GameName = "GenericGame",
            Timestamp = DateTime.UtcNow
        };
    }

    private static int ParsePlayerHp(string text)
    {
        var match = Regex.Match(text, @"(?:HP|Health|Hit\s*Points)[:\s]*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out int hp) ? hp : 0;
    }

    private static int ParseGold(string text)
    {
        var match = Regex.Match(text, @"(?:Gold|G)[:\s]*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out int gold) ? gold : 0;
    }

    private static int ParseDeckCount(string text)
    {
        var match = Regex.Match(text, @"(?:Deck|D)[:\s]*(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out int deck) ? deck : 0;
    }

    private static List<string> ParseHandCards(string text)
    {
        var cards = new List<string>();
        var matches = Regex.Matches(text, @"(?:Card|Hand)[@:\s]+(.+?)(?=\n|$)", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
                cards.Add(match.Groups[1].Value.Trim());
        }
        return cards;
    }

    private static string ParseEnemyType(string text)
    {
        var match = Regex.Match(text, @"(?:Enemy|Target|Monster)[:\s]+(.+?)(?:\n|$)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : "Unknown";
    }
}

/// <summary>
/// Concrete game state with parsed values
/// </summary>
public class ParsedGameState : GameState
{
    public string RecognizedText { get; set; } = string.Empty;
    public int PlayerHp { get; set; }
    public int Gold { get; set; }
    public int DeckCount { get; set; }
    public List<string> HandCards { get; set; } = new();
    public string EnemyType { get; set; } = "Unknown";
}