using System;
using GameAssistant.Core.Enums;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.Core.Interfaces;

public interface IOcrService
{
    string RecognizeFromBytes(byte[] imageBytes, OcrMode mode);
}
