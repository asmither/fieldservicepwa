namespace ICS.Portal.Auth.Models
{
    public class RefreshToken
    {
        public RefreshToken(string Token, DateTime TokenIssuedUTC)
        {
            this.Token = Token;
            this.TokenIssuedUTC = TokenIssuedUTC;
        }

        public string Token { get; }
        public DateTime TokenIssuedUTC { get; }
    }
}
