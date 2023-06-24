using System.Net;

namespace achilles {
    public class AchillesVisit {
        public Uri Target { get; private set; }
        public DateTime Time { get; private set; }
        public CookieCollection Cookies { get; private set; }
        public AchillesVisit(Uri target, DateTime time, CookieCollection cookies) {
            Target = target; Time = time; Cookies = cookies;
        }
    }

    public class AchillesHistory : List<AchillesVisit> { }
}
