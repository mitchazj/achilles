using PuppeteerSharp;

namespace achilles;

public class AchillesAdvanced : Achilles {
    public AchillesAdvanced() {
    }

    public AchillesAdvanced(Achilles original) {
    }

    public async void Screenshot(string outputFile = "screenshot.png") {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        await using var page = await browser.NewPageAsync();
        await page.GoToAsync("http://www.google.com");
        await page.ScreenshotAsync(outputFile);
    }
}