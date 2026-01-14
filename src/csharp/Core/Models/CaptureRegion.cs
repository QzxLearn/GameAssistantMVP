using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.Core.Models
{
    public readonly record struct CaptureRegion(double X, double Y, double Width, double Height)
    {
        public bool IsValid => Width > 0 && Height > 0;
    }
}
