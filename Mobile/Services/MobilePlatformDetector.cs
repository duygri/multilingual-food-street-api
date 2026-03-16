namespace FoodStreet.Mobile.Services
{
    /// <summary>
    /// Returns IsMobile = true for MAUI mobile app.
    /// Registered in MauiProgram.cs.
    /// </summary>
    public class MobilePlatformDetector : FoodStreet.Client.Services.IPlatformDetector
    {
        public bool IsMobile => true;
    }
}
