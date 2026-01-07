using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistantMVP.Services
{
    public interface IOcrService
    {
        string RecognizeText(Mat image, string dataPath = "./tessdata");
    }
}
