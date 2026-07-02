using ICS.Portal.Data.Queries.Models;
using System.Net;

namespace ICS.Mobile.Http;

/// <summary>
/// Wrapper class for returning status code with T result
/// </summary>
/// <typeparam name="T"></typeparam>
public class HttpResult<T> : HttpResult
{
    public T? Data { set; get; }

    public static implicit operator HttpResult<T>(HttpResult<TechNewsListOutput> v)
    {
        throw new NotImplementedException();
    }
}

public class HttpResult
{
    public HttpStatusCode StatusCode { set; get; }

    public string? ErrorMessage { set; get; }

    public bool IsSuccess
    {
        get
        {
            int code = (int)StatusCode;
            int[] acceptValues = new int[] { 200, 400, 401, 423 };

            return acceptValues.Contains(code);
        }
    }
}
