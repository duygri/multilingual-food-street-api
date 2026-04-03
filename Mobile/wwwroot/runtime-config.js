window.FoodStreetMapConfig = Object.assign(window.FoodStreetMapConfig || {}, {
    // Browser key only for fallback WebView pages that still render JS/Static Maps.
    // Native Android browse map and native picker no longer read this value.
    // Restrict this key to your app/domain setup before putting the real value here.
    googleMapsApiKey: "YOUR_MOBILE_API_KEY_HERE"
});
