namespace achilles.Tests;

public class SimpleUnit {
    [Fact]
    public void AutoRedirectOnByDefault() {
        Achilles achilles = new Achilles();
        Assert.Equal(achilles.AllowAutoRedirect, true);
    }
}