using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Easy_Logger_Parser;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter([Intent.ActionOpenDocument, Intent.ActionCreateDocument])]
public class MainActivity : MauiAppCompatActivity { }
