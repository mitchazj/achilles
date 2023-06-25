using System.Net;
using System.Text;
using System.Collections.Specialized;
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
    public string? Title { get => htmlDocument.DocumentNode.SelectSingleNode("//head/title")?.InnerHtml; }
    public string Body { get => htmlDocument.DocumentNode.OuterHtml; }

    public byte[] FileData = new byte[0];
    public bool IsFile = false;

    private HtmlDocument htmlDocument = new HtmlDocument();

    public static Uri GetAbsoluteHref(Uri baseUrl, string link) {
        link = link.Replace("&#x3a;", ":").Replace("&#x2f;", "/");
        return new Uri(new Uri(baseUrl, link).AbsoluteUri);
    }

    private static string GetStringFromResponse(HttpWebResponse response) {
        using (StreamReader streamReader = new StreamReader(response.GetResponseStream())) {
            return streamReader.ReadToEnd().Replace("&#x27;", "'").Replace("&amp;", "&");
        }
    }

    private static byte[] GetBytesFromResponse(HttpWebResponse response) {
        MemoryStream ms = new MemoryStream();
        response.GetResponseStream().CopyTo(ms);
        return ms.ToArray();
    }

    private void HandleHttpWebResponse(HttpWebResponse httpWebResponse) {
        // Update Stuff
        Cookies.Add(httpWebResponse.Cookies);                              // Update Cookies
        Url = httpWebResponse.ResponseUri;                                 // Update URL
        if (httpWebResponse.ContentType.Contains("html")) {
            htmlDocument.LoadHtml(GetStringFromResponse(httpWebResponse)); // Update Document
            Assets = AssetCollection.FromDocument(htmlDocument);           // Refresh Assets
            IsFile = false;
        }
        else if (httpWebResponse.ContentType.Contains("application/xml")) {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(GetStringFromResponse(httpWebResponse));           // Unpack HTML from response
            htmlDocument.LoadHtml(doc.InnerText);                          // Update Document
            Assets = AssetCollection.FromDocument(htmlDocument);           // Refresh Assets
            IsFile = false;
        }
        else {
            FileData = GetBytesFromResponse(httpWebResponse);
            IsFile = true;
        }
        // Update History
        History.Add(new AchillesVisit(Url, DateTime.Now, Cookies.GetCookies(Url)));
    }

    public static HttpWebResponse HttpGet(Uri url, CookieContainer cookieContainer) {
        // Create the GET request
        HttpWebRequest request = WebRequest.CreateHttp(url);
        request.AutomaticDecompression = DecompressionMethods.GZip;
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0";
        request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
        request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
        request.Headers.Add(HttpRequestHeader.Upgrade, "1");
        request.CookieContainer = cookieContainer;

        // Return the response
        HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
        return httpWebResponse;
    }

    public static HttpWebResponse HttpPost(Uri url, CookieContainer cookieContainer, string referer, NameValueCollection postData) {
        // Create the POST request
        HttpWebRequest request = WebRequest.CreateHttp(url);
        request.Method = "POST";
        request.AutomaticDecompression = DecompressionMethods.GZip;
        request.ContentType = "application/x-www-form-urlencoded";
        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0";
        request.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
        request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br");
        request.Headers.Add(HttpRequestHeader.Upgrade, "1");
        request.Referer = referer;
        request.CookieContainer = cookieContainer;

        using (Stream postStream = request.GetRequestStream()) {
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = ascii.GetBytes(postData.ToString());
            request.ContentLength = postBytes.Length;

            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Flush();
        }

        // Return the response
        HttpWebResponse httpWebResponse = (HttpWebResponse)request.GetResponse();
        return httpWebResponse;
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
        Get(url);
        return this;
    }
    public Achilles Fetch(string url) {
        Fetch(new Uri(url));
        return this;
    }
    public Achilles PostValues(Uri url, NameValueCollection postData) {
        Post(url, postData);
        return this;
    }
    public Achilles PostValues(string url, NameValueCollection postData) {
        Post(new Uri(url), postData);
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
        Post(GetAbsoluteHref(this.Url, form.Action), outgoingQuery);
        return this;
    }


    private HttpWebResponse Get(Uri url) {
        int error_tries = 0;
        while (error_tries < 3) {
            try {
                HttpWebResponse httpWebResponse = HttpGet(url, this.Cookies);
                HandleHttpWebResponse(httpWebResponse); // Update state
                return httpWebResponse;
            }
            catch {
                ++error_tries;
            }
        }
        throw new Exception("Tries exceeded.");
    }

    private HttpWebResponse Post(Uri url, NameValueCollection postData) {
        HttpWebResponse httpWebResponse = HttpPost(url, this.Cookies, this.Url.ToString(), postData);
        HandleHttpWebResponse(httpWebResponse); // Update state
        return httpWebResponse;
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

}
