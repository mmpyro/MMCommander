using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(CommanderTest.HeadlessSetup))]

namespace CommanderTest
{
    public class HeadlessSetup
    {
        // Skia rendering (not headless drawing) so windows can be captured as images.
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<Commander.App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
