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
}
