using ICS.Mobile.Helpers;

namespace ICS.Mobile.Http;

public class HttpPing : HttpClientBase
{
    public HttpPing(HttpClient client)
        : base(client, "api/v1/Ping")
    {
    }

    public async Task<bool> PingAsync()
    {
        try
        {
            string urlFragment = UrlFragmentForService("HelloWorld");
            var response = await client.PostAsync(urlFragment, null);
            return response.IsSuccessStatusCode;
        }
        catch(Exception ex)
        {
            Console.Write(ex.Message);
        }
        return false;
    }
}
