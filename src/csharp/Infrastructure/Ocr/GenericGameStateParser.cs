using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;

namespace GameAssistant.Infrastructure.Ocr;

public class GenericGameStateParser : IGameStateParser
{
    public string GameName => "Generic";

    public GameState Parse(string ocrText)
    {
        return new GenericGameState
        {
            GameName = GameName,
            RecognizedText = ocrText.Trim(),
            RawOcrText = ocrText.Trim()
        };
    }

    public T Parse<T>(string ocrText) where T : GameState
    {
        var state = Parse(ocrText);
        if (state is T typed)
            return typed;

        // 尝试从 RawOcrText 重建
        return Activator.CreateInstance<T>();
    }
}
