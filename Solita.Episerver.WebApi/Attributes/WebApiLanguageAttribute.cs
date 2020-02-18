using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

        private bool UseDtoLanguageParameter { get; }

        /// <summary>
        /// Sets Episerver default language context according to a method parameter value.
        /// Default language parameter names: "language", "languageId", "epslanguage". 
        /// </summary>
        /// <param name="useDtoLanguageParameter">Search language parameter from method parameter objects properties</param>
        public WebApiLanguageAttribute(bool useDtoLanguageParameter = false)
        {
            UseDtoLanguageParameter = useDtoLanguageParameter;
        }

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
            string language = ResolveLanguageByParamName(actionParams);
            return !string.IsNullOrWhiteSpace(language)
                ? language
                : (UseDtoLanguageParameter ? ResolveLanguageFromDto(actionParams) : null);
        }

        private string ResolveLanguageByParamName(IDictionary<string, object> actionParams)
        {
            var key = LanguageParamNames.FirstOrDefault(k => actionParams.ContainsKey(k));
            return (key != null) ? actionParams[key] as string : null;
        }

        /// <summary>
        /// Search language value from request dto parameters
        /// </summary>
        /// <param name="actionParams"></param>
        /// <returns></returns>
        private string ResolveLanguageFromDto(IDictionary<string, object> actionParams)
        {
            return
            (
                from param in actionParams.Values
                let language = ResolveLanguageFromDto(param)
                where !string.IsNullOrWhiteSpace(language)
                select language
            ).FirstOrDefault();
        }

        /// <summary>
        /// Searches language from dtos top level properties
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        private string ResolveLanguageFromDto(object dto)
        {
            if (dto == null)
            {
                return null;
            }

            var languageProperty = FindLanguageProperty(dto);
            return languageProperty != null ? PropertyValueAsString(languageProperty, dto) : null;
        }

        private PropertyInfo FindLanguageProperty(object dto)
        {
            var properties = FindPublicStringProperties(dto);
            return properties?.Where(x => LanguageParamNames.Contains(x.Name.ToLower())).FirstOrDefault();
        }

        private static IEnumerable<PropertyInfo> FindPublicStringProperties(object dto)
        {
            return dto?.GetType()
                ?.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                ?.Where(x => x.PropertyType == typeof(string));
        }

        private static string PropertyValueAsString(PropertyInfo property, object dto)
        {
            return property?.GetValue(dto, null)?.ToString();
        }
    }
}
