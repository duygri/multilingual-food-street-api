namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Returns IsMobile = false for web browser.
    /// Registered in Frontend Program.cs.
    /// </summary>
    public class WebPlatformDetector : IPlatformDetector
    {
        public bool IsMobile => false;
    }
}
