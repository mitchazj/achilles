using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using HtmlAgilityPack;
using Xunit.Abstractions;

namespace achilles.Tests;

public class SimpleUnit {
    private readonly ITestOutputHelper output;

    public SimpleUnit(ITestOutputHelper output) {
        this.output = output;
    }

    [Fact]
    public void AchillesDocumentExists() {
        Achilles achilles = new Achilles();
        var htmlDocumentRef = typeof(Achilles).GetField("htmlDocument", BindingFlags.NonPublic | BindingFlags.Instance);
        var htmlDocument = htmlDocumentRef.GetValue(achilles);
        htmlDocument.Should().BeOfType(typeof(HtmlDocument));
        htmlDocument.Should().NotBeNull();
    }

    [Fact]
    public void LoadCodeProject() {
        Achilles achilles = new Achilles();
        achilles.Fetch("https://codeproject.com/");
        Assert.Equal("CodeProject - For those who code", achilles.Title);
    }

    [Fact]
    public void LoadDuckDuckGo() {
        Achilles achilles = new Achilles();
        achilles.Fetch("https://duckduckgo.com/");
        Assert.Equal("DuckDuckGo — Privacy, simplified.", achilles.Title);
    }

    [Fact]
    public void SearchDuckDuckGo() {
        Achilles achilles = new Achilles();
        achilles.Fetch("https://html.duckduckgo.com")
            .Assets.Forms[0].Fill("q", "George Washington");
        achilles.Submit(achilles.Assets.Forms[0]);
        achilles.Assets.Links.FindAll(l => l.Class == "result__a").ForEach(link => { output.WriteLine(link.Text); });
        var links = achilles.Assets.Links.FindAll(l => l.Class == "result__a");
        links.Should().NotBeEmpty();
    }

    [Fact]
    public void TrendingRepos() {
        Achilles achilles = new Achilles();
        var links = achilles.Fetch("https://github.com/trending").Assets.Links
            .FindAll(a => a.Parent.GetAttributeValue("class", "") == "h3 lh-condensed");
        links.ForEach(a => { output.WriteLine("(" + Regex.Replace(a.Text, @"\s+", " ").Trim() + ") " + a.Href); });
        links.Should().NotBeEmpty();
    }

    [Fact]
    public void Screenshot() {
        var outputFile = Path.Combine(AppContext.BaseDirectory, "screenshot.png");
        var fileInfo = new FileInfo(outputFile);
        if (fileInfo.Exists) {
            fileInfo.Delete();
        }
        var achilles = new Achilles()
            .Fetch("https://www.google.com/")
            .MakeAdvanced();
        achilles.Screenshot(outputFile);
        output.WriteLine(outputFile);
        Assert.True(File.Exists(outputFile));
    }


    [Fact]
    public void QantasMoney() {
        var outputFile = Path.Combine(AppContext.BaseDirectory, "qantas.png");

        // TODO: make this and below work by supporting cookie passing
        // var achilles = new Achilles()
        //     .MakeAdvanced()
        //     .Fetch("https://www.qantasmoney.com/account/");
        // var achilles = new Achilles()
        //     .Fetch("https://www.qantasmoney.com/account/");
        //     .MakeAdvanced()

        var achilles = new AchillesAdvanced()
            .Fetch("https://www.qantasmoney.com/account/")
            .ExpectWait(WaitType.InputName("verifyCode"))
            .Assets.Forms[0].Fill("memberId", "")
            .Assets.Forms[0].Fill("lastName", "")
            .Assets.Forms[0].Fill("memberPin", "")
            .Assets.Forms[0].Submit()
            .Wait();
//            .ExpectWait(WaitType.NavigationComplete)
//            .Assets.Forms[0].Fill("verifyCode", getUserInput())
//            .Assets.Forms[0].Submit()
//            .Wait();

        achilles.Screenshot(outputFile);
        output.WriteLine(outputFile);

        true.Should().BeTrue();
    }
}