using ICS.Portal.Data.Images;

using System.Security.Cryptography;
using System.Text.Json;

namespace ICS.Mobile.Http;

public class HttpImages : HttpClientBase
{
    public HttpImages(HttpClient client)
        : base(client, "api/v1/images")
    {
    }

    public async Task<HttpResult> PostImageForm(MultipartFormDataContent form)
    {
        var result = new HttpResult<SasTokenResponse>();
        
        string urlFragment = UrlFragmentForService("uploadForm");
        var response = await client.PostAsync(urlFragment, form);
        result.StatusCode = response.StatusCode;
        return result;
    }
   
    public async Task<HttpResult> Upload(ImageInsertInput imageUploadInput)
    {
        var result = new HttpResult();

        //try
        //{
            string urlFragment = UrlFragmentForService("Upload");
            var response = await client.PostAsync(urlFragment, GetStringContent(imageUploadInput));
            result.StatusCode = response.StatusCode;
        //}
        //catch (Exception ex)
        //{
            //result.StatusCode = System.Net.HttpStatusCode.InternalServerError;
            //result.ErrorMessage = ex.Message;
        //}
        return result;
    }
    public async Task<bool> TryHydrate(ImageInsertInput imageUploadInput)
    {
        try
        {
            string urlFragment = UrlFragmentForService("getImageData");
            var response = await client.PostAsync(urlFragment, GetStringContent(imageUploadInput));
            if (response.IsSuccessStatusCode)
            {
                byte[] content = await response.Content.ReadAsByteArrayAsync();
                imageUploadInput.Data = content;
                return true;
            }
            imageUploadInput.ContentType = null;
        }
        catch (Exception ex)
        {
            imageUploadInput.ContentType = null;
            Console.Write($"Error in {nameof(TryHydrate)} - {ex.Message}");
        }

        return false;
    }
    //public async Task<bool> TryHydrate(ImageInsertInput imageUploadInput)
    //{
    //    try
    //    {
    //        string urlFragment = UrlFragmentForService("getImageBytes");
    //        var response = await client.PostAsync(urlFragment, GetStringContent(imageUploadInput));
    //        if (response.IsSuccessStatusCode)
    //        {
    //            string responseContent = await response.Content.ReadAsStringAsync();
    //            if (!string.IsNullOrEmpty(responseContent))
    //            {
    //                byte[] bytes = JsonSerializer.Deserialize<byte[]>(responseContent)!;
    //                imageUploadInput.Data = bytes;
    //                return true;
    //            }
    //            else
    //            {
    //                imageUploadInput.ContentType = null;
    //            }
    //        }
    //        else
    //        {
    //            imageUploadInput.ContentType = null;
    //            Console.Write($"Image not found {nameof(TryHydrate)} status code: {response.StatusCode}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        imageUploadInput.ContentType = null;
    //        Console.Write($"Error in {nameof(TryHydrate)} - {ex.Message}");
    //    }

    //    return false;
    //}
}
