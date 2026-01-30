using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameAssistant.Presentation.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly IOcrService _ocrService; // �?接口类型

        // 依赖注入（由 Program.cs 注册�?
        public OcrController(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        [HttpPost("recognize")]
        public IActionResult Recognize([FromBody] OcrRequest request)
        {
            if (string.IsNullOrEmpty(request.Base64Image))
                return BadRequest("Base64Image is required");

            try
            {
                byte[] imageBytes = Convert.FromBase64String(request.Base64Image);
                string text = _ocrService.RecognizeFromBytes(imageBytes); // �?调用统一服务
                return Ok(new { text });
            }
            catch (Exception ex)
            {
                return BadRequest($"OCR failed: {ex.Message}");
            }
        }
    }
    public class OcrRequest
    {
        public string Base64Image { get; set; } = string.Empty;
        public ImagePreprocessOptions? Preprocess { get; set; }
    }
}

