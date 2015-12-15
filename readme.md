# Solita EPiServer WebApi 
Web API toolkit for Episerver 9 to boost productivity and performance.

Contains following ActionFilterAttributes:
* **[EpiserverWebApiOutputCacheAttribute](https://github.com/solita/episerver-webapi/blob/master/Solita.Episerver.WebApi/Attributes/EpiserverWebApiOutputCacheAttribute.cs)**: OutputCacheAttribute for Web API that invalidates the cache when Episerver content changes.
* **[WebApiLanguageAttribute](https://github.com/solita/episerver-webapi/blob/master/Solita.Episerver.WebApi/Attributes/WebApiLanguageAttribute.cs)**: Sets Episerver default language context according to a method parameter value.
* **[ValidateWebApiModelAttribute](https://github.com/solita/episerver-webapi/blob/master/Solita.Episerver.WebApi/Attributes/ValidateWebApiModelAttribute.cs)**: Automatically rejects requests that have invalid model state.

Contains following components:
* **[EpiserverWebApiErrorHandler](https://github.com/solita/episerver-webapi/blob/master/Solita.Episerver.WebApi/EpiserverWebApiErrorHandler.cs)**: Logs errors and displays full exception only for editors/admins/localhost.
* **[StructureMapWebApiDependencyResolver](https://github.com/solita/episerver-webapi/blob/master/Solita.Episerver.WebApi/StructureMapWebApiDependencyResolver.cs)**: Web API DependencyResolver for StructureMap.

## Configuration example
A sample configuration on how to use the components. Also contains some best-practice tips.

```csharp
[ModuleDependency(typeof(ServiceContainerInitialization))]
[InitializableModule]
public class WebApiInitializer : IConfigurableModule
{
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
        // Resolver for Web API
        GlobalConfiguration.Configuration.DependencyResolver = new StructureMapWebApiDependencyResolver(context.Container);
    }

    public void Initialize(InitializationEngine context)
    {
        // Configure Web API as a whole in one configurator class
        GlobalConfiguration.Configure(WebApiConfig.ConfigureWebApi);
    }
    
    public void Uninitialize(InitializationEngine context) { }
}
```

```csharp
public static class WebApiConfig
{
    public static void ConfigureWebApi(HttpConfiguration config)
    {
        // "Best-practice" setup for a typical project 
        AllowAttributeRouting(config);
        AddRoutes(config);
        ReturnJsonOnly(config);
        IndentJson(config);
        
        // Set EpiserverWebApiErrorHandler as default error handler
        SetCustomErrorHandler(config);
    }
    
    private static void AllowAttributeRouting(HttpConfiguration config)
    {
        config.MapHttpAttributeRoutes();
    }

    private static void AddRoutes(HttpConfiguration config)
    {
        /* Episerver Find defines this route already. Uncomment if Find is not used.
        config.Routes.MapHttpRoute(
            name: "DefaultApi",
            routeTemplate: "api/{language}/{controller}/{id}",
            defaults: new { id = RouteParameter.Optional });
         * */
    }

    private static void ReturnJsonOnly(HttpConfiguration config)
    {
        // Web API returns xml or json depending on the request http headers (accept).
        // When you debug the method with a browser it defaults to xml.
        // In a typical use case we need json only, thus this feature is counterproductive. 
        // By removing the xml supported media types the method will always default to json.
        config.Formatters.XmlFormatter.SupportedMediaTypes.Clear();
    }

    private static void IndentJson(HttpConfiguration config)
    {
        config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
    }

    private static void SetCustomErrorHandler(HttpConfiguration config)
    {
        // Log errors and display full exception for admins/editors/localhost
        config.Services.Replace(typeof(IExceptionHandler), new EpiserverWebApiErrorHandler());
    }
}  
```

## EpiserverWebApiOutputCacheAttribute
OutputCacheAttribute for Web API that invalidates the cache when Episerver content changes.
This is accomplished with a cache depency for the Episerver content cache (_"DataFactoryCache.Version"_), which will clear all the cached results when Episerver content is modified. By default, the cache is disabled in debug-mode.

The attribute has one required constructor parameter and three optional properties.
* **DurationSeconds**: Cache duration in seconds. Required constructor parameter.
* **EnableInDebug**: By default, the cache is disabled in debug-mode (`<compilation debug="true">` in web.config). Set true to enable the cache in debug-mode.
* **DisableForAuthenticated**: By default, the cache is enabled for all users. Set true to disable the cache for authenticated users.
* **CacheDependencyKeys**: By default, contains "_{ DataFactoryCache.VersionKey }_". Set to null to disable the Episerver content cache dependency altogether, or add custom dependencies.

```csharp
[EpiserverWebApiOutputCache(60 * 60)]
public class StoreApiController : ApiController
{
    [HttpGet]
    [Route("api/stores")]
    public IList<Store> GetStores(int count)
    {
        // Result is cached for each unique url for 60 minutes or until Episerver content is modified.
    }        
}
```

## WebApiLanguageAttribute
Sets Episerver default language context according to a method parameter value.
Default language parameter names: "language", "languageId", "epslanguage".
Just add a parameter with any of the names, and pass the language value by it to make it default.

The attribute has one optional property.
* **LanguageParamNames**: Expected language parameter names. Default values are: { "language", "languageId", "epslanguage" }.

```csharp
[WebApiLanguage]
public class SearchApiController : ApiController
{
    [HttpGet]
    [Route("api/search")]
    public SearchResult Search(string query, string language)
    {
        // PreferredCulture, CurrentCulture and CurrentUICulture are set to language's culture.
        // All Episerver interfaces now use "language" as the default language.
        // In essence, this makes language issues with Episerver API as easy as with PageControllers.
    }        
}
```

## ValidateWebApiModelAttribute
Checks if any of the parameters is null, or has invalid model state. Returns HttpStatusCode.BadRequest in both cases.

```csharp
public class CommentApiController : ApiController
{
    [HttpPost]
    [Route("api/comment")]
    [ValidateWebApiModel]
    public CommentModel PostComment(CommentModel model)
    {
        // Method is only executed when model is not null and its state is valid.
        // Otherwise HttpStatusCode.BadRequest is returned with a exception about the errors.
    }
    
    public class CommentModel
    {
        [Required]
        public string Username { get; set; }
        [Required]
        [MaxLength(4000)]
        public string Message { get; set; }
    }
}
```

## EpiserverWebApiErrorHandler
Custom error handler that handles the error logging and error message visibility with responses. 
All errors are logged using EPiServer.Logging.ILogger. The handler returns HTTP 500 status with a message "An error has occurred" for end users. But for admin/editor/locahost users the full exception is displayed to ease debugging.

## StructureMapWebApiDependencyResolver
This a simple DependencyResolver for Web API. It is included to avoid unnecessary boilerplate code in the project.