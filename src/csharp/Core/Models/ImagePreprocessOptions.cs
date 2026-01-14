using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.Core.Models
{
    public class ImagePreprocessOptions
    {
        public bool Grayscale { get; set; } = true;
        public bool Invert { get; set; } = true;
        public double ScaleFactor { get; set; } = 2.0;
        public ThresholdMethod Threshold { get; set; } = ThresholdMethod.Otsu;
    }
}
