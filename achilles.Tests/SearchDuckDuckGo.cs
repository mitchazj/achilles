namespace achilles.Tests;

public class SearchDuckDuckGo {
    // start a local server to serve the test page
    // private static readonly SimpleServer server = new SimpleServer("https://duckduckgo.com/", "<html><head><title>DuckDuckGo — Privacy, simplified.</title></head><body></body></html>");

    [Fact]
    public void LoadDuckDuckGo() {
        Achilles achilles = new Achilles();
        achilles.Fetch("https://duckduckgo.com/");
        Assert.Equal("DuckDuckGo — Privacy, simplified.", achilles.Title);
    }
}