using System;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using EPiServer.Logging;
using EPiServer.Security;

namespace Solita.Episerver.WebApi
{
    /// <summary>
    /// Logs all expections using EPiServer.Logging.ILogger.
    /// The handler returns HTTP 500 status with a message "An error has occurred" for end users. 
    /// But for admin/editor/locahost users the full exception is displayed to ease debugging.
    /// </summary>
    public class EpiserverWebApiErrorHandler : ExceptionHandler
    {
        private static readonly ILogger Log = LogManager.GetLogger();

        public override void Handle(ExceptionHandlerContext context)
        {
            var exception = context.Exception;

            // Web API errors are not logged by Episerver by default
            Log.Error($"Exception on url {HttpContext.Current.Request.RawUrl}", exception);

            // Show full exception only for localhost or admins/editors
            var content = ShowDetailedError() ? exception.ToString() : "An error has occurred.";
            context.Result = new InternalServerErrorTextPlainResult(content, Encoding.UTF8, context.Request);
        }

        private static bool ShowDetailedError()
        {
            // Show detailed errors if request if from local machine OR if user has edit/admin rights. 
            return HttpContext.Current.Request.IsLocal || IsEditorOrAdmin(PrincipalInfo.CurrentPrincipal);
        }

        private static bool IsEditorOrAdmin(IPrincipal user)
        {
            return user != null && (user.IsInRole( "WebAdmins") || user.IsInRole("WebEditors") || user.IsInRole("Administrators"));
        }

        private class InternalServerErrorTextPlainResult : IHttpActionResult
        {
            private readonly string _content;
            private readonly Encoding _encoding;
            private readonly HttpRequestMessage _request;

            public InternalServerErrorTextPlainResult(string content, Encoding encoding, HttpRequestMessage request)
            {
                if (content == null || encoding == null || request == null)
                {
                    throw new ArgumentNullException(nameof(content));
                }

                _content = content;
                _encoding = encoding;
                _request = request;
            }
            
            public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(Execute());
            }

            private HttpResponseMessage Execute()
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    RequestMessage = _request,
                    Content = new StringContent(_content, _encoding)
                };
            }
        }
    }
}