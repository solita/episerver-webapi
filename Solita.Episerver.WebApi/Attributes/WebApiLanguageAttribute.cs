using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using EPiServer.Globalization;

namespace Solita.Episerver.WebApi.Attributes
{
    /// <summary>
    /// Sets Episerver default language context according to a method parameter value.
    /// Default language parameter names: "language", "languageId", "epslanguage".
    /// </summary>
    public class WebApiLanguageAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Expected language parameter names, multiple values allowed.
        /// Default values are: { "language", "languageId", "epslanguage" }.
        /// </summary>
        public string[] LanguageParamNames { get; set; } = { "language", "languageId", "epslanguage" };
        
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var language = ResolveLanguage(actionContext.ActionArguments);
            if (language == null)
            {
                return;
            }

            var culture = new CultureInfo(language);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            ContentLanguage.PreferredCulture = culture;
        }

        private string ResolveLanguage(IDictionary<string, object> actionParams)
        {
            var key = LanguageParamNames.FirstOrDefault(k => actionParams.ContainsKey(k));
            return (key != null) ? actionParams[key] as string : null;
        }
    }
}
