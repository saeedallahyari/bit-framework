﻿using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bit.IdentityServer.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using IdentityServer3.Core.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultViewService : IViewService
    {
        public virtual ISsoPageHtmlProvider SsoHtmlPageProvider { get; set; }

        public virtual async Task<Stream> ClientPermissions(ClientPermissionsViewModel model)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>ClientPermissions >> Not Implemented</title>
                                </head>
                                <body>ClientPermissions >> Not Implemented</body>
                            </html>";

            return await ReturnHtmlAsync(model, content, CancellationToken.None).ConfigureAwait(false);
        }

        public virtual async Task<Stream> Consent(ConsentViewModel model, ValidatedAuthorizeRequest authorizeRequest)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>Consent >> Not Implemented</title>
                                </head>
                                <body>Consent >> Not Implemented</body>
                            </html>";

            return await ReturnHtmlAsync(model, content, CancellationToken.None).ConfigureAwait(false);
        }

        public virtual async Task<Stream> Error(ErrorViewModel model)
        {
            string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>{model.ErrorMessage}</title>
                                </head>
                                <body>{model.ErrorMessage} <br /> RequestId: {model.RequestId}</body>
                            </html>";

            return await ReturnHtmlAsync(model, content, CancellationToken.None).ConfigureAwait(false);
        }

        public virtual async Task<Stream> LoggedOut(LoggedOutViewModel model, SignOutMessage message)
        {
            string content = null;

            if (!string.IsNullOrEmpty(model.RedirectUrl))
            {
                content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{model.RedirectUrl}'>
                                </head>
                                <body></body>
                            </html>";
            }
            else
            {
                content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>No redirect url on logout</title>
                                </head>
                                <body>
                                    No redirect url on logout
                                </body>
                            </html>";
            }

            return await ReturnHtmlAsync(model, content, CancellationToken.None).ConfigureAwait(false);
        }

        public virtual async Task<Stream> Login(LoginViewModel model, SignInMessage message)
        {
            JsonSerializerSettings jsonSerSettings = new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            if (model.Custom == null && message.ReturnUrl != null)
            {
                string state = new Uri(message.ReturnUrl).ParseQueryString()["state"] ?? "{}";
                model.Custom = JsonConvert.DeserializeObject<dynamic>(state, jsonSerSettings);
            }

            string json = JsonConvert.SerializeObject(new
            {
                model.AdditionalLinks,
                model.AllowRememberMe,
                model.AntiForgery,
                model.ClientLogoUrl,
                model.ClientName,
                model.ClientUrl,
                model.CurrentUser,
                model.Custom,
                model.ErrorMessage,
                model.ExternalProviders,
                model.LoginUrl,
                model.LogoutUrl,
                model.RememberMe,
                model.RequestId,
                model.SiteName,
                model.SiteUrl,
                model.Username,
                ReturnUrl = message.ReturnUrl == null ? "" : new Uri(message.ReturnUrl).ParseQueryString()["redirect_uri"]
            }, Formatting.None, jsonSerSettings);

            string loginPageHtml = (await SsoHtmlPageProvider.GetSsoPageAsync(CancellationToken.None).ConfigureAwait(false))
                .Replace("{model}", Microsoft.Security.Application.Encoder.HtmlEncode(json));

            return await ReturnHtmlAsync(model, loginPageHtml, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<Stream> ReturnHtmlAsync(CommonViewModel model, string html, CancellationToken cancellationToken)
        {
            MemoryStream viewStream = new MemoryStream();

            StreamWriter writter = new StreamWriter(viewStream);

            await writter.WriteAsync(html).ConfigureAwait(false);

            await writter.FlushAsync().ConfigureAwait(false);

            viewStream.Seek(0, SeekOrigin.Begin);

            return viewStream;
        }

        public virtual async Task<Stream> Logout(LogoutViewModel model, SignOutMessage message)
        {
            // Based on current InvokeLogOut Middleware, this method will not be called, because of context.Authentication.SignOut("custom", "Barear"); code.

            string content = $@"<!DOCTYPE html>
                            <html>
                                <body>
                                    <form id='logoutForm' method='post' action='{model.LogoutUrl}'>
                                        <input type='hidden' name='{model.AntiForgery.Name}' value='{model.AntiForgery.Value}'>
                                    </form>
                                    <script>
                                        document.getElementById('logoutForm').submit();
                                    </script>
                                </body>
                            </html>";

            return await ReturnHtmlAsync(model, content, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
