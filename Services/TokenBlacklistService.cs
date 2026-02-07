namespace WebApiInterviewStatus.Services
{
    public class TokenBlacklistService
    {
        private static readonly HashSet<string> _blacklist = new();

        public void Add(string token) => _blacklist.Add(token);
        public bool IsBlacklisted(string token) => _blacklist.Contains(token);
    }

}
