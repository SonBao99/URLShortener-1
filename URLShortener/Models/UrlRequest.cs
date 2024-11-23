namespace URLShortener.Models
{
    public class UrlRequest
    {
        public string OriginalUrl { get; set; }
        public string? CustomAlias { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
