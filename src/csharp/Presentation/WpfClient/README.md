# GameAssistant MVP

A lightweight Windows desktop assistant for screen capture and OCR (Tesseract).

## Features
- Select screen region visually
- Capture and preprocess image (grayscale + Otsu + upscale)
- Recognize text with Tesseract OCR
- Local, no cloud dependency

## Requirements
- Windows 10/11
- .NET 8.0 Desktop Runtime
- Tesseract `eng.traineddata` in `tessdata/`

## Build
```powershell
dotnet build