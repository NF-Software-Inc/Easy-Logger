namespace Easy_Logger_Parser;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	/// <inheritdoc />
	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "Easy Logger Parser" };
	}
}
