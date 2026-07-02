namespace ICS.Mobile.Http;

public class HttpCommands : HttpClientBase
{
    public HttpCommands(HttpClient client)
        : base(client, "api/v1/Commands")
    {
    }
}
