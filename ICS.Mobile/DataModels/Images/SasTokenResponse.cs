namespace ICS.Portal.Data.Images
{
    public class SasTokenResponse
    {
        public SasTokenResponse(string url, string token)
        {
            Url = url;
            Token = token;
        }

        public string Url { get; }
        public string Token { get; }
    }
}
