using System.Net;
using System.Text;
using System.Collections.Specialized;
using System.IO.Compression;
using System.Web;
using System.Xml;
using HtmlAgilityPack;

namespace achilles;

public class Achilles {
    public int Timeout = 1500;
    public bool AllowAutoRedirect = true;

    public AchillesHistory History = new AchillesHistory();
    public CookieContainer Cookies = new CookieContainer();
    public AssetCollection Assets = new AssetCollection();
    public KeyCollection Keys = new KeyCollection();

    public Uri Url { get; private set; }
    public string Body {
        get => htmlDocument.DocumentNode.OuterHtml;
    }
    public string? Title {
        get => htmlDocument.DocumentNode.SelectSingleNode("//head/title")?.InnerHtml;
    }

    public bool IsFile = false;
    public byte[] FileData = new byte[0];

    private HtmlDocument htmlDocument = new HtmlDocument();

    // TODO: simplify this a bit?
    public static Uri GetAbsoluteHref(Uri baseUrl, string link) {
        // TODO: figure out what the purpose of this was
        // link = link.Replace("&#x3a;", ":").Replace("&#x2f;", "/");

        // Decode HTML entities
        link = WebUtility.HtmlDecode(link);

        // Remove leading/trailing whitespaces and line breaks
        link = link.Trim();

        // Remove any fragments (#) from the link
        int fragmentIndex = link.IndexOf('#');
        if (fragmentIndex >= 0) {
            link = link.Substring(0, fragmentIndex);
        }

        // Combine the base URL and the link, and ensure it's a valid absolute URI
        Uri absoluteUri;
        if (Uri.TryCreate(baseUrl, link, out absoluteUri)) {
            return absoluteUri;
        }
        else {
            // Handle invalid URIs gracefully (you can customize the behavior based on your requirements)
            throw new ArgumentException("Invalid URL", nameof(link));
        }
    }

    public static async Task<HttpResponseMessage> HttpGet(Uri url, CookieContainer cookieContainer) {
        // TODO: create a more modern and up-to-date version of these headers
        // also provide a mapping enum for more than one type
        // or maybe a builder

        // Create the HttpClient
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        client.DefaultRequestHeaders.Add("Upgrade", "1");

        // Convert CookieContainer to HttpClient cookies
        IEnumerable<Cookie> cookies = cookieContainer.GetCookies(url).Cast<Cookie>();
        foreach (Cookie cookie in cookies) {
            client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value}");
        }

        // Send the GET request
        HttpResponseMessage response = await client.GetAsync(url);
        return response;
    }

    public static async Task<HttpResponseMessage> HttpPost(Uri url, CookieContainer cookieContainer, string referer,
        NameValueCollection postData) {
        // Create the HttpClient
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.5");
        client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        client.DefaultRequestHeaders.Add("Upgrade", "1");
        client.DefaultRequestHeaders.Referrer = new Uri(referer);

        // Convert CookieContainer to HttpClient cookies
        IEnumerable<Cookie> cookies = cookieContainer.GetCookies(url);
        foreach (Cookie cookie in cookies) {
            client.DefaultRequestHeaders.Add("Cookie", $"{cookie.Name}={cookie.Value}");
        }

        // Create the form content
        FormUrlEncodedContent formContent = new FormUrlEncodedContent(postData.AllKeys.Select(key =>
            new KeyValuePair<string, string>(key, postData[key])));

        // Send the POST request
        HttpResponseMessage response = await client.PostAsync(url, formContent);
        return response;
    }

    public Achilles FetchNoVisit(Uri url) {
        Achilles achilles = new Achilles();
        achilles.Cookies = Cookies;
        return achilles.Fetch(url);
    }

    public Achilles FetchNoVisit(string url) {
        return FetchNoVisit(new Uri(url));
    }

    public IEnumerable<Achilles> FetchAllNoVisit(List<string> urls) {
        foreach (string url in urls) {
            Achilles a = new Achilles(); // Still more to do, needs the same Cookie stash
            yield return a.Fetch(url);
        }
    }

    public Achilles PostNoVisit(Uri url, NameValueCollection postData) {
        Achilles achilles = new Achilles();
        achilles.Cookies = Cookies;
        return achilles.PostValues(url, postData);
    }

    public Achilles PostNoVisit(string url, NameValueCollection postData) {
        return PostNoVisit(new Uri(url), postData);
    }

    public Achilles Fetch(Uri url) {
        // TODO: is this a sketchy way to do this?
        var _ = Get(url).Result;
        Console.WriteLine(_);
        return this;
    }

    public Achilles Fetch(string url) {
        Fetch(new Uri(url));
        return this;
    }

    public Achilles PostValues(Uri url, NameValueCollection postData) {
        // TODO: is this a sketchy way to do this?
        var _ = Post(url, postData).Result;
        return this;
    }

    public Achilles PostValues(string url, NameValueCollection postData) {
        // TODO: is this a sketchy way to do this?
        var _ = Post(new Uri(url), postData).Result;
        return this;
    }

    public Achilles Click(LinkAsset link) {
        Fetch(GetAbsoluteHref(this.Url, link.Href));
        return this;
    }

    public Achilles Submit(FormAsset form) {
        // Collect all data in the form
        NameValueCollection outgoingQuery = HttpUtility.ParseQueryString(String.Empty);
        form.Fields.ForEach(f => {
            outgoingQuery.Add(f.GetAttributeValue("name", ""), f.GetAttributeValue("value", ""));
        });

        // Send the POST request
        // TODO: is this a sketchy way to do this?
        var _ = Post(GetAbsoluteHref(this.Url, form.Action), outgoingQuery).Result;
        return this;
    }

    public async void Download(Uri url, string filename) {
        await Task.Run(() => HttpGet(url, this.Cookies));
    }

    public async void Download(string url, string filename) {
        await Task.Run(() => Download(new Uri(url), filename));
    }

    // public async IEnumerable<Achilles> DownloadAll(List<string> urls, string folder) {
    //     foreach (string url in urls) {
    //         Achilles a = new Achilles(); // Still more to do, needs the same Cookie stash
    //         throw new NotImplementedException();
    //         yield return a.Fetch(url);
    //     }
    // }

    private async Task<HttpResponseMessage> Get(Uri url) {
        // TODO: make this better - why are there 3? shouldn't there be a config for this?
        int error_tries = 0;
        while (error_tries < 3) {
            try {
                HttpResponseMessage httpResponseMessage = await HttpGet(url, this.Cookies);
                await HandleHttpResponse(httpResponseMessage); // Update state
                return httpResponseMessage;
            }
            catch {
                ++error_tries;
            }
        }

        throw new Exception("Tries exceeded.");
    }

    private async Task<HttpResponseMessage> Post(Uri url, NameValueCollection postData) {
        HttpResponseMessage httpResponseMessage = await HttpPost(url, this.Cookies, this.Url.ToString(), postData);
        await HandleHttpResponse(httpResponseMessage); // Update state
        return httpResponseMessage;
    }

    // TODO: clean up
    private async Task HandleHttpResponse(HttpResponseMessage httpResponse) {
        // Update Stuff
        if (httpResponse.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues)) {
            foreach (string cookieValue in cookieValues) {
                Cookies.SetCookies(httpResponse.RequestMessage.RequestUri, cookieValue); // Update Cookies
            }
        }

        // TODO: not sure if this is right. was AI generated
        Url = httpResponse.RequestMessage.RequestUri; // Update URL

        // TODO: write unit tests for this and extract into a proper method
        string responseContent;
        if (httpResponse.Content.Headers.ContentEncoding.Contains("gzip")) {
            // Decompress the G-Zipped response content
            using (var decompressedStream =
                    new GZipStream(await httpResponse.Content.ReadAsStreamAsync(), CompressionMode.Decompress))
                using (var decompressedContent = new StreamReader(decompressedStream)) {
                    responseContent = await decompressedContent.ReadToEndAsync();
                }
        }
        else if (httpResponse.Content.Headers.ContentEncoding.Contains("br")) {
            // Decompress the Brotli-compressed response content
            using (var decompressedStream = new MemoryStream()) {
                using (var brotliStream = new BrotliStream(await httpResponse.Content.ReadAsStreamAsync(),
                        CompressionMode.Decompress)) {
                    await brotliStream.CopyToAsync(decompressedStream);
                }

                decompressedStream.Seek(0, SeekOrigin.Begin);
                using (var decompressedContent = new StreamReader(decompressedStream)) {
                    responseContent = await decompressedContent.ReadToEndAsync();
                }
            }
        }
        else if (httpResponse.Content.Headers.ContentEncoding.Contains("deflate")) {
            // Decompress the Deflate-compressed response content
            using (var decompressedStream = new DeflateStream(await httpResponse.Content.ReadAsStreamAsync(),
                    CompressionMode.Decompress))
                using (var decompressedContent = new StreamReader(decompressedStream)) {
                    responseContent = await decompressedContent.ReadToEndAsync();
                }
        }
        else if (httpResponse.Content.Headers.ContentEncoding.Contains("zlib")) {
            // TODO: not 100% sure that this is accurate. Write a unit-test to check
            // Decompress the Zlib-compressed response content
            using (var decompressedStream = new MemoryStream()) {
                using (var zlibStream = new DeflateStream(await httpResponse.Content.ReadAsStreamAsync(),
                        CompressionMode.Decompress)) {
                    zlibStream.CopyTo(decompressedStream);
                }

                decompressedStream.Seek(0, SeekOrigin.Begin);
                using (var decompressedContent = new StreamReader(decompressedStream)) {
                    responseContent = await decompressedContent.ReadToEndAsync();
                }
            }
        }
        else {
            responseContent = await httpResponse.Content.ReadAsStringAsync();
        }


        // TODO: make this more robust
        if (httpResponse.Content.Headers.ContentType.MediaType.Contains("html")) {
            htmlDocument.LoadHtml(responseContent); // Update Document
            Assets = AssetCollection.FromDocument(htmlDocument); // Refresh Assets
            IsFile = false;
        }
        else if (httpResponse.Content.Headers.ContentType.MediaType.Contains("application/xml")) {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseContent); // Unpack HTML from response
            htmlDocument.LoadHtml(doc.InnerText); // Update Document
            Assets = AssetCollection.FromDocument(htmlDocument); // Refresh Assets
            IsFile = false;
        }
        else {
            FileData = Encoding.UTF8.GetBytes(responseContent);
            IsFile = true;
        }

        // Update History
        History.Add(new AchillesVisit(Url, DateTime.Now, Cookies.GetCookies(Url)));
    }
}
