using GameAssistant.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameAssistant.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OcrController : ControllerBase
    {
        private readonly OcrService _ocrService;

        public OcrController(OcrService ocrService)
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
                string text = _ocrService.RecognizeFromBytes(imageBytes);
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
    }
}
