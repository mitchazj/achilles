using System.Reflection;
using FluentAssertions;
using HtmlAgilityPack;

namespace achilles.Tests;

public class SimpleUnit {
    [Fact]
    public void AutoRedirectOnByDefault() {
        Achilles achilles = new Achilles();
        Assert.Equal(achilles.AllowAutoRedirect, true);
    }

    [Fact]
    public void Timeout1500ByDefault() {
        Achilles achilles = new Achilles();
        Assert.Equal(achilles.Timeout, 1500);
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
}