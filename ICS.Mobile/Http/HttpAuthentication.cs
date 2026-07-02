using ICS.Portal.Auth.Models;

namespace ICS.Mobile.Http;

public class HttpAuthentication : HttpClientBase
{
    public HttpAuthentication(HttpClient client)
        : base(client, "api/v1/Authentication")
    {
    }

    public async Task<HttpResult<UserLoginOutput>> LoginAsync(UserLoginInput userLogin)
    {
        var result = new HttpResult<UserLoginOutput>();

        try
        {
            string urlFragment = UrlFragmentForService("Login");
            var response = await client.PostAsync(urlFragment, GetStringContent(userLogin));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<UserLoginOutput>> ForgotPasswordAsync(ForgotPasswordInput forgotPasswordInput)
    {
        var result = new HttpResult<UserLoginOutput>();

        try
        {
            string urlFragment = UrlFragmentForService("ForgotPassword");
            var response = await client.PostAsync(urlFragment, GetStringContent(forgotPasswordInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<OtpSendOutput>> ForgotPasswordOtpSend(OtpSendOutput otpSendInput)
    {
        var result = new HttpResult<OtpSendOutput>();

        try
        {
            string urlFragment = UrlFragmentForService("ForgotPasswordOtpSend");
            var response = await client.PostAsync(urlFragment, GetStringContent(otpSendInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<AuthorizedUser>> ForgotPasswordChangePassword(ForgotPasswordChangePasswordInput forgotPasswordChangePasswordInput)
    {
        var result = new HttpResult<AuthorizedUser>();

        try
        {
            string urlFragment = UrlFragmentForService("ForgotPasswordChangePassword");
            var response = await client.PostAsync(urlFragment, GetStringContent(forgotPasswordChangePasswordInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<OtpSendOutput>> OtpSend(OtpSendInput otpSendInput)
    {
        var result = new HttpResult<OtpSendOutput>();

        try
        {
            string urlFragment = UrlFragmentForService("OtpSend");
            var response = await client.PostAsync(urlFragment, GetStringContent(otpSendInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<AuthorizedUser>> OtpVerify(OtpVerifyInput otpVerifyInput)
    {
        var result = new HttpResult<AuthorizedUser>();

        try
        {
            string urlFragment = UrlFragmentForService("OtpVerify");
            var response = await client.PostAsync(urlFragment, GetStringContent(otpVerifyInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<AuthorizedUser>> ChangePassword(ChangePasswordInput changePasswordInput)
    {
        var result = new HttpResult<AuthorizedUser>();

        try
        {
            string urlFragment = UrlFragmentForService("ChangePassword");
            var response = await client.PostAsync(urlFragment, GetStringContent(changePasswordInput));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<AuthorizedUser>> RefreshTokenAsync()
    {
        var result = new HttpResult<AuthorizedUser>();

        try
        {
            string urlFragment = UrlFragmentForService("RefreshToken");
            var response = await client.PostAsync(urlFragment, GetStringContent(null));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<HttpResult<AuthorizedContractorLoginOutput>> SubContractorLogin(SubContractorLoginInput input)
    {
        var result = new HttpResult<AuthorizedContractorLoginOutput>();

        try
        {
            string urlFragment = UrlFragmentForService("SubContractorLogin");
            var response = await client.PostAsync(urlFragment, GetStringContent(input));
            await ProcessResult(response, result);
        }
        catch (Exception ex)
        {
            result.StatusCode = System.Net.HttpStatusCode.NotFound;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }
}
