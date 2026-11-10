using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(ComanderTest.HeadlessSetup))]

namespace ComanderTest
{
    public class HeadlessSetup
    {
        // Skia rendering (not headless drawing) so windows can be captured as images.
        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<Comander.App>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}
