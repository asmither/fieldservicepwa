using ICS.Mobile.Http;
using ICS.Mobile.Services;
using ICS.Mobile.Services.ServiceModels;
using ICS.Portal.Auth.Models;
using ICS.Portal.Data.Custom;

using Microsoft.AspNetCore.Components;

using System.Net;

namespace ICS.Mobile.Pages
{
    public partial class LoginPage
    {
        #region Inject

        [Inject]
        private NavigationManager navManager { set; get; } = default!;

        [Inject]
        private IWorkflowData dataService { set; get; } = default!;

        [Inject]
        private HTTPService httpService { set; get; }
        #endregion Inject

        #region Fields

        private UserLoginInput? userLoginInput = null;
        private OtpSendInput? otpSendInput = null;
        private OtpVerifyInput? otpVerifyInput = null;
        private ChangePasswordInput? changePasswordInput = null;
        private List<UserLoginOutput.Communication>? communicationOptions = null;
        private string? FormErrorMessage = null;
        private bool IsBusy = false;

        private string? UnauthorizedMessage;

        bool forgotPasswordOtpVerified = false;

        ForgotPasswordInput? forgotPasswordInput = null;
        OtpSendInput? forgotPasswordSendOtpInput = null;
        ForgotPasswordChangePasswordInput forgotPasswordChangePasswordInput = null;
        int? forgotPasswordUserId = null;

        [SupplyParameterFromQuery]
        public string? Token { set; get; }

        // Dev-only UI preview button. Disabled — flip to `#if DEBUG` true to
        // re-enable walking the shell without a real login.
        private static bool ShowDevPreview => false;

        // Never surface raw JSON / network internals to users (anti-slop).
        private static string FriendlyError(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Something went wrong. Please try again.";

            var t = raw.Trim();
            if (t.StartsWith("{") || t.StartsWith("[")
                || t.Contains("Network error", StringComparison.OrdinalIgnoreCase)
                || t.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase))
                return "Couldn't reach the server. Check your connection and try again.";

            return t;
        }

        private async Task DevPreviewLogin()
        {
#if DEBUG
            var previewUser = new AuthorizedUser(
                id: 1,
                entityTypeId: 1,
                entityId: 999999,
                UserName: "UIPREVIEW",
                RoleFlag: 0,
                Token: "dev-ui-preview",
                TokenIssuedUtc: DateTime.UtcNow,
                TokenExpireMinutes: 60 * 24,
                TokenRefreshMinutes: 60 * 24,
                PasswordExpiresUtc: DateTime.UtcNow.AddYears(1));

            await CaptureAppState(previewUser);
            navManager.NavigateTo("/");
#else
            await Task.CompletedTask;
#endif
        }

        #endregion Fields

        #region Login
        private void SetOtpToken(string token)
        {
            otpSendInput.OtpToken = token;
            StateHasChanged();
        }

        public async Task UserLoginSubmit()
        {
            ClearErrors();
            IsBusy = true;
            var httpResult = await httpService.GetHttpAuthenticationClient().LoginAsync(userLoginInput!);

            if (httpResult.StatusCode == HttpStatusCode.OK)
            {
                var output = httpResult.Data!;
                if (output.Communications is not null && output.Communications.Count != 0)
                {
                    var filteredOptions = output.Communications.FindAll(o => o.CommunicationTypeId == 2 || o.CommunicationTypeId == 4);
                    var distinctOptions = filteredOptions.DistinctBy(o => new { o.MaskedData, o.CommunicationTypeId }).ToList();
                    var companyPhone = distinctOptions.FirstOrDefault(o => o.CommunicationTypeId == 4);

                    if (companyPhone is not null)
                    {
                        distinctOptions.Remove(companyPhone);
                        distinctOptions.Insert(0, companyPhone);
                    }

                    communicationOptions = distinctOptions;

                    if (communicationOptions.Count == 0)
                    {
                        FormErrorMessage = "Unable to send verification code to user.";
                    }
                    else
                    {
                        otpSendInput = new OtpSendInput() { OtpToken = communicationOptions[0].Token! };
                    }

                    userLoginInput = null;
                }
                else
                {
                    FormErrorMessage = "Unable to send verification code to user.";
                }
            }
            else
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "Invalid User Name/Password Combination";
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        break;
                    default:
                        FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        break;
                }
            }

            IsBusy = false;
        }

        public async Task OtpSendSubmit()
        {
            ClearErrors();
            IsBusy = true;
            if (string.IsNullOrEmpty(otpSendInput!.OtpToken))
            {
                FormErrorMessage = "Choose one of the following options!";
                return;
            }

            var httpResult = await httpService.GetHttpAuthenticationClient().OtpSend(otpSendInput!);

            if (httpResult.StatusCode == HttpStatusCode.OK)
            {
                var output = httpResult.Data!;
                otpVerifyInput = new OtpVerifyInput { OtpToken = output.OtpToken, OtpValue = string.Empty };
                otpSendInput = null;
            }
            else
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "User not found";
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        break;
                    default:
                        FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        break;
                }
            }
            IsBusy = false;
        }

        public void ResendOtp()
        {
            ClearErrors();
            otpSendInput = new OtpSendInput()
            {
                OtpToken = otpVerifyInput!.OtpToken!
            };
            otpVerifyInput = null;
        }

        private async Task CaptureAppState(AuthorizedUser? authorizedUser)
        {
            await dataService.LoadAppStateAsync();
            dataService.AppState.AuthorizedUser = authorizedUser;
            await dataService.SaveAppStateInstance();
        }
        public async Task OtpVerifySubmit()
        {
            ClearErrors();

            IsBusy = true;
            var httpResult = await httpService.GetHttpAuthenticationClient().OtpVerify(otpVerifyInput!);

            if (httpResult.StatusCode == HttpStatusCode.OK)
            {
                otpVerifyInput = null;
                otpSendInput = null;
                var user = httpResult.Data!;

                await CaptureAppState(user);

                if (user.PasswordIsExpired())
                {
                    changePasswordInput = new() { AuthToken = user.Token };
                }
                else
                {
                    navManager.NavigateTo(dataService.AppState.LoginRedirect ?? "/");
                }
            }
            else
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "Unauthorized";
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        break;
                    default:
                        FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        break;
                }
            }
            IsBusy = false;
        }

        #endregion Login

        #region Forgot Password
        private void OnForgotPasswordClick()
        {
            forgotPasswordInput = new ForgotPasswordInput();
            userLoginInput = null;
            forgotPasswordOtpVerified = false;
        }

        // Return to the main login form from any sub-step (OTP, forgot/reset).
        private bool OnLoginStep => userLoginInput is not null;

        private void BackToLogin()
        {
            ClearErrors();
            otpSendInput = null;
            otpVerifyInput = null;
            changePasswordInput = null;
            forgotPasswordInput = null;
            forgotPasswordSendOtpInput = null;
            forgotPasswordChangePasswordInput = null;
            forgotPasswordOtpVerified = false;
            communicationOptions = null;
            userLoginInput = new UserLoginInput();
        }

        private async Task ForgotPasswordSubmit()
        {
            ClearErrors();
            IsBusy = true;
            var httpResult = await httpService.GetHttpAuthenticationClient(dataService.AppState.AuthorizedUser).ForgotPasswordAsync(forgotPasswordInput!);

            if (httpResult.IsSuccess)
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "User Not Found. Cannot reset password at this time";
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        break;
                    default:
                        if (!string.IsNullOrEmpty(httpResult.ErrorMessage))
                        {
                            FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        }
                        break;
                }

                if (FormErrorMessage is null)
                {
                    if (httpResult.Data is not null && httpResult.Data.Communications is not null && httpResult.Data.Communications.Count != 0)
                    {
                        var filteredOptions = httpResult.Data.Communications.FindAll(o => o.CommunicationTypeId == 2 || o.CommunicationTypeId == 4);
                        var distinctOptions = filteredOptions.DistinctBy(o => new { o.MaskedData, o.CommunicationTypeId }).ToList();
                        var companyPhone = distinctOptions.FirstOrDefault(o => o.CommunicationTypeId == 4);
                        if (companyPhone is not null)
                        {
                            distinctOptions.Remove(companyPhone);
                            distinctOptions.Insert(0, companyPhone);
                        }

                        communicationOptions = distinctOptions;

                        if (communicationOptions.Count == 0)
                        {
                            FormErrorMessage = "Phone Number(s) not found. Unable to send verification code to user.";
                        }
                        else
                        {
                            forgotPasswordInput = null;

                            forgotPasswordSendOtpInput = new OtpSendInput
                            {
                                OtpToken = communicationOptions[0].Token!
                            };
                        }
                    }
                    else
                    {
                        FormErrorMessage = "Phone Number(s) not found. Unable to send verification code to user.";
                    }
                }
            }
            IsBusy = false;
        }

        private void SetPasswordOtpToken(string token)
        {
            forgotPasswordSendOtpInput!.OtpToken = token;
            StateHasChanged();
        }

        private async Task ForgotPasswordSendOtpSubmit()
        {
            ClearErrors();
            IsBusy = true;
            if (string.IsNullOrEmpty(forgotPasswordSendOtpInput!.OtpToken))
            {
                FormErrorMessage = "Choose one of the following options!";
                return;
            }

            var httpResult = await httpService.GetHttpAuthenticationClient(dataService.AppState.AuthorizedUser).OtpSend(forgotPasswordSendOtpInput!);

            if (httpResult.IsSuccess)
            {
                var output = httpResult.Data!;
                forgotPasswordChangePasswordInput = new ForgotPasswordChangePasswordInput()
                {
                    OtpToken = output.OtpToken
                };

                forgotPasswordSendOtpInput = null;
            }
            else
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "User not found";
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        break;
                    default:
                        FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        break;
                }
            }
            IsBusy = false;
        }


        public async Task ForgotPasswordChangePasswordSubmit()
        {
            ClearErrors();
            IsBusy = true;
            var httpResult = await httpService.GetHttpAuthenticationClient(dataService.AppState.AuthorizedUser).ForgotPasswordChangePassword(forgotPasswordChangePasswordInput!);

            if (httpResult.IsSuccess && httpResult.Data != null)
            {
                var output = httpResult.Data!;
                forgotPasswordSendOtpInput = null;

                IsBusy = false;
                await CaptureAppState(output);
                Redirect();
            }
            else
            {
                switch (httpResult.StatusCode)
                {
                    case HttpStatusCode.Unauthorized:
                        FormErrorMessage = "Invalid code. Please try again.";
                        // Reset to OTP entry screen
                        forgotPasswordOtpVerified = false;
                        forgotPasswordChangePasswordInput.OtpCode = string.Empty;
                        forgotPasswordChangePasswordInput.NewPassword = string.Empty;
                        forgotPasswordChangePasswordInput.ConfirmPassword = string.Empty;
                        break;
                    case HttpStatusCode.Locked:
                        FormErrorMessage = "This account is locked. Please contact your administrator";
                        forgotPasswordOtpVerified = false;
                        break;
                    default:
                        FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
                        // Also go back to OTP screen for other errors
                        forgotPasswordOtpVerified = false;
                        forgotPasswordChangePasswordInput.OtpCode = string.Empty;
                        forgotPasswordChangePasswordInput.NewPassword = string.Empty;
                        forgotPasswordChangePasswordInput.ConfirmPassword = string.Empty;
                        break;
                }
            }

            IsBusy = false;
        }
        private void ForgotPasswordVerifyOtpSubmit()
        {
            ClearErrors();
            if (string.IsNullOrEmpty(forgotPasswordChangePasswordInput?.OtpCode) ||
                forgotPasswordChangePasswordInput.OtpCode.Length != 6)
            {
                FormErrorMessage = "Please enter a valid 6-digit code";
                StateHasChanged();
                return;
            }
            forgotPasswordOtpVerified = true;
            StateHasChanged();
        }

        #endregion

        protected override async Task OnInitializedAsync()
        {
            await dataService.LoadAppStateAsync();

            if (!string.IsNullOrEmpty(Token))
            {
                var httpResult = await httpService.GetHttpAuthenticationClient().SubContractorLogin(
                    new SubContractorLoginInput(Token)
                );
                if (httpResult.IsSuccess)
                {
                    if (httpResult.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        UnauthorizedMessage = " Your link has expired.  Please contact ICS to issue a new link for this job.";
                        return;
                    }

                    if (httpResult.Data is not null)
                    {
                        dataService.AppState.AuthorizedUser = httpResult.Data.User;
                        if (dataService.AppState.AuthorizedUser is not null)
                        {
                            if (dataService.AppState.AuthorizedUser.PasswordIsExpired() ||
                                dataService.AppState.AuthorizedUser.IsTokenExpired() ||
                                dataService.AppState.AuthorizedUser.EntityId == 0)
                            {

                                // Password expired or invalid user dont let them go anywhere
                                return;
                            }
                        }

                        if (dataService.AppState.IsContractor)
                        {
                            // get from initial token
                            dataService.AppState.LockedDispatchId = httpResult.Data.WorkOrderDispatchId;
                        }
                        else
                            dataService.AppState.LockedDispatchId = 0;

                        await dataService.SaveAppStateInstance();
                        navManager.NavigateTo("/dispatch-list");
                    }
                }
            }
            else
            {
                string userName = string.Empty;

                // Clear out Locked DispatchID immediately if it is in appstate
                if (dataService.AppState.LockedDispatchId.GetValueOrDefault(0) != 0)
                {
                    dataService.AppState.LockedDispatchId = 0;
                    await dataService.SaveAppStateInstance();
                }

                if (dataService.AppState.AuthorizedUser is not null)
                {
                    userName = dataService.AppState.AuthorizedUser.UserName;
                }
                userLoginInput = new UserLoginInput()
                {
                    UserName = userName
                };
            }
        }

        public async Task ChangePasswordSubmit()
        {
            ClearErrors();

            var httpResult = await httpService.GetHttpAuthenticationClient(dataService.AppState.AuthorizedUser).ChangePassword(changePasswordInput!);

            if (httpResult.IsSuccess)
            {
                var user = httpResult.Data!;
                await CaptureAppState(user);
                Redirect();
            }
            else
            {
                FormErrorMessage = FriendlyError(httpResult.ErrorMessage);
            }
        }

        private void Redirect()
        {
            navManager.NavigateTo(dataService.AppState.LoginRedirect ?? "/");
        }
        private void ClearErrors()
        {
            FormErrorMessage = null;
        }

        private string communicationType(byte type)
        {
            return channelMappings[type];
        }


        private Dictionary<byte, string> channelMappings = new Dictionary<byte, string>
        {
            { 1,"email" },
            { 2,"sms" },
            { 3,"voice" },
            { 4,"sms" },
            { 5,"N/A" },
            { 6,"voice" },
            { 7,"N/A" },
            { 8,"N/A" },
            { 9,"voice" },
            { 10,"N/A" }
        };
    }
}
