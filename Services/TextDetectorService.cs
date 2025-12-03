using System.IO;
using Tesseract;

namespace Automatization.Services
{
    public class TextDetectorService : IDisposable
    {
        private readonly TesseractEngine? _tesseractEngine;
        private readonly bool _isInitialized;

        public TextDetectorService(string tessDataPath = "tessdata", string language = "eng")
        {
            try
            {
                _tesseractEngine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
                _isInitialized = true;
                LogService.LogInfo($"TextDetectorService initialized successfully with tessdata path: {tessDataPath}, language: {language}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"Failed to initialize TextDetectorService. Make sure tessdata is correctly configured.", ex);
                _isInitialized = false;
                _tesseractEngine = null;
            }
        }

        public string? GetTextFromBitmap(Bitmap bitmap)
        {
            if (!_isInitialized || _tesseractEngine == null)
            {
                LogService.LogError("TextDetectorService is not initialized. Cannot perform OCR.");
                return null;
            }

            if (bitmap == null)
            {
                LogService.LogWarning("Attempted to perform OCR on a null bitmap.");
                return null;
            }

            try
            {
                using MemoryStream stream = new();

                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] bytes = stream.ToArray();

                using Pix pix = Pix.LoadFromMemory(bytes);
                using Page page = _tesseractEngine.Process(pix);

                return page.GetText();
            }
            catch (Exception ex)
            {
                LogService.LogError($"Error during OCR: {ex.Message}", ex);
                return null;
            }
        }

        public void Dispose()
        {
            _tesseractEngine?.Dispose();
        }
    }
}