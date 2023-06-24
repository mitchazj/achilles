namespace achilles {
    public class Key {
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Domain { get; private set; }
        public Key() {
            Username = Password = Domain = "";
        }
        public Key(string username, string password, string domain) {
            Username = username; Password = password; Domain = domain.ToLower();
        }
        public bool MatchesUrl(string url) {
            return (new Uri(url)).Host.ToLower().Contains(Domain);
        }
        public bool MatchesUrl(Uri url) {
            return url.Host.ToLower().Contains(Domain);
        }
    }

    public class KeyCollection : List<Key> { }
}
