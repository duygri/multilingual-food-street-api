namespace FoodStreet.Server.Services.Interfaces
{
    public interface ITtsService
    {
        /// <summary>
        /// Chuyển đổi text thành file audio và trả về URL để stream.
        /// </summary>
        Task<string?> TextToSpeechAsync(string text, string? language = "vi-VN");
    }
}
