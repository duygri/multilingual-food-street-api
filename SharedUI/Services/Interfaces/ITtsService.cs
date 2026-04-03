using System.Threading.Tasks;

namespace FoodStreet.Client.Services
{
    public interface ITtsService
    {
        Task PlayTextAsync(string text, string? languageCode = null);
        Task PlayAudioFileAsync(string url);
        Task StopAllAsync();
    }
}
