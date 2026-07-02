using System.ComponentModel;
using System.Text.Json;

namespace ICS.Mobile.Http;
public abstract class HttpClientBase
{
    protected readonly HttpClient client;
    protected readonly string servicePrefix;

    public HttpClientBase(HttpClient client, string servicePrefix)
    {
        this.client = client;
        this.servicePrefix = servicePrefix;
    }

    public async Task<HttpResult<T>> PostAsync<T>(object? input = null)
    {
        string serviceName = typeof(T).Name.Replace("Output", "");
        return await (PostAsync<T>(serviceName, input));
    }
    private async Task<HttpResult<T>> PostAsync<T>(string serviceName, object? obj)
    {
        var result = new HttpResult<T>();

        try
        {
            string urlFragment = UrlFragmentForService(serviceName);
            var response = await client.PostAsync(urlFragment, GetStringContent(obj));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            // Network-level failure (offline, DNS, unreachable API). Return a
            // failed HttpResult so callers hit their normal error path instead
            // of an unhandled exception crashing to the error boundary — this
            // app spends half its life in basements with no signal.
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    protected async Task ProcessResult<T>(HttpResponseMessage response, HttpResult<T> result)
    {
        result.StatusCode = response.StatusCode;
        
        if (result.IsSuccess)
        {
            string data = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(data))
            {
                result.Data = default;
            }
            else
            {
                try
                {
                    if (typeof(T).IsValueType || typeof(T) == typeof(string))
                    {
                        result.Data = Convert<T>(data);
                    }
                    else
                    {
                        result.Data = JsonSerializer.Deserialize<T>(data, options);
                    }
                }
                catch (Exception ex)
                {
                    result.ErrorMessage = $"The call succeed with an HttpStatusCode {result.StatusCode} but an error occured consuming the result with the following exception: {ex}";
                }
            }
        }
        else
        {
            if (result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                result.ErrorMessage = "Access Denied";
            }
            else
            {
                string data = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrEmpty(data))
                {
                    result.ErrorMessage = "The call results in an error status but no additional information is available.";
                }
                else
                {
                    result.ErrorMessage = data;
                }
            }
        }
    }

    protected StringContent GetStringContent(Object? obj)
    {
        if (obj is null)
        {
            return new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        }

        return new StringContent(JsonSerializer.Serialize(obj), System.Text.Encoding.UTF8, "application/json");
    }

    private T? Convert<T>(string input)
    {
        TypeConverter? typeConverter = TypeDescriptor.GetConverter(typeof(T));

        if (typeConverter != null)
        {
            return (T?)typeConverter.ConvertFromString(input);
        }

        return default;
    }

    protected string UrlFragmentForService(string serviceName)
    {
        return $"{servicePrefix}/{serviceName}";
    }

    private readonly JsonSerializerOptions options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

}
