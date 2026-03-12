using System.Threading.Tasks;

namespace FoodStreet.Client.Services
{
    public interface ITtsService
    {
        Task PlayTextAsync(string text);
        Task PlayAudioFileAsync(string url);
        Task StopAllAsync();
    }
}
