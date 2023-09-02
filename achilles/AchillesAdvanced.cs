using System.Diagnostics;
using PuppeteerSharp;

namespace achilles;

public class AchillesAdvanced {
    public string debugOutput = "";

    public AdvancedAssetCollection Assets { get; private set; }
    public IBrowser browser;
    public IPage page;

    private object _navigationTask;

    public AchillesAdvanced() {
        Task.Run(Setup).Wait();
        Assets = new AdvancedAssetCollection(this);
    }

    public static AchillesAdvanced From(Achilles original) {
        AchillesAdvanced advanced = new AchillesAdvanced();
        if (original.Url != null) advanced.Fetch(original.Url);
        return advanced;
    }

    public AchillesAdvanced Wait() {
        (_navigationTask as Task)?.GetAwaiter().GetResult();
        return this;
    }

    public AchillesAdvanced ExpectWait(WaitType waitType) {
        if (waitType.Type == WaitType.FunctionType.Navigation) {
            _navigationTask = page.WaitForNavigationAsync();
        }
        else if (waitType.Type == WaitType.FunctionType.InputName) {
            _navigationTask = page.WaitForSelectorAsync(waitType.Data);
        }

        return this;
    }

    public AchillesAdvanced Fetch(Uri url) {
        return Fetch(url.ToString());
    }

    public AchillesAdvanced Fetch(string url) {
        IResponse response = page.GoToAsync(url).Result;
        return this;
    }

    public void Screenshot(string outputFile = "screenshot.png") {
        Task.Run(() => page.ScreenshotAsync(outputFile)).Wait();
    }

    private async Task<bool> Setup() {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1800, Height = 1300 });
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0");
        return true;
    }

    //    Stopwatch sw = new Stopwatch();
    //    sw.Start();
    //
    //    sw.Stop();
    //
    //    TimeSpan ts = sw.Elapsed;
    //    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
    //        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
    //    debugOutput = elapsedTime;
}

public class WaitType {
    public enum FunctionType {
        Navigation,
        InputName
    }

    public static WaitType NavigationComplete = new(FunctionType.Navigation, "navigation_complete");

    public static WaitType InputName(string inputName) {
        return new WaitType(FunctionType.InputName, inputName);
    }

    public string Data { get; private set; }
    public FunctionType Type { get; private set; }

    public WaitType(FunctionType functionType, string data) {
        Type = functionType;
        Data = data;
    }

    public override string ToString() {
        return Data;
    }
}

public class AdvancedAssetCollection {
    private AchillesAdvanced achilles;

    public AdvancedAssetCollection(AchillesAdvanced achilles) {
        this.achilles = achilles;
    }

    public List<AdvancedFormAsset> Forms {
        get {
            var forms = achilles.page.QuerySelectorAllAsync("form").Result;
            return forms.Select(x => new AdvancedFormAsset(achilles, x)).ToList();
        }
    }
}

public class AdvancedFormAsset {
    private AchillesAdvanced achilles;
    private IElementHandle element;

    public AdvancedFormAsset(AchillesAdvanced achilles, IElementHandle element) {
        this.achilles = achilles;
        this.element = element;
    }

    public AchillesAdvanced Fill(string name, string value) {
        var inputElement = element.QuerySelectorAsync($"input[name='{name}']").Result;
        if (inputElement != null) {
            Task.Run(() => inputElement.TypeAsync(value)).Wait();
        }

        return achilles;
    }

    public AchillesAdvanced Submit() {
        var submitBtn = element.QuerySelectorAsync("button[type='submit']").GetAwaiter().GetResult();
        if (submitBtn != null) {
            submitBtn.ClickAsync().GetAwaiter().GetResult();
        }
        else {
            element.EvaluateFunctionAsync("form => form.submit()").GetAwaiter().GetResult();
        }

        return achilles;
    }
}