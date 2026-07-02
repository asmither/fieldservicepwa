namespace ICS.Mobile.Http;

public class HttpQueries : HttpClientBase
{
    public HttpQueries(HttpClient client)
        : base(client, "api/v1/Queries")
    {
    }
}