namespace NarrationApp.Mobile;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var languageCode = Microsoft.Maui.Storage.Preferences.Default.Get(
			Features.Home.VisitorBrand.PreferredAppLanguageCodeKey,
			"vi");
		return new Window(new MainPage()) { Title = Features.Home.VisitorBrand.DisplayName(languageCode) };
	}
}
