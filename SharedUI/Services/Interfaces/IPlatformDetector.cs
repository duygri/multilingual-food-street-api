namespace FoodStreet.Client.Services
{
    /// <summary>
    /// Interface to detect whether the app is running on a mobile device or web browser.
    /// Used by Layout components to choose between MobileLayout and AdminLayout.
    /// </summary>
    public interface IPlatformDetector
    {
        bool IsMobile { get; }
    }
}
