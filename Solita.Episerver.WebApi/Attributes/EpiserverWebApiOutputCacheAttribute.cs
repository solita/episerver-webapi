using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using EPiServer.Core;
using EPiServer.Framework.Cache;
using EPiServer.ServiceLocation;

namespace Solita.Episerver.WebApi.Attributes
{
    /// <summary>
    /// OutputCacheAttribute for Web API that invalidates the cache when Episerver content changes.
    /// Caching is disabled in debug-mode (&lt;compilation debug="true"&gt; in web.config) by default.
    /// </summary>
    public class EpiserverWebApiOutputCacheAttribute : ActionFilterAttribute
    {
        private const string DataFactoryCacheKeyPlaceholder = "default";

        private int DurationSeconds { get; }

        /// <summary>
        /// By default, the cache is enabled for all users. Set true to disable the cache for authenticated users.
        /// </summary>
        public bool DisableForAuthenticated { get; set; }

        /// <summary>
        /// By default, the cache is disabled in debug-mode (&lt;compilation debug="true"&gt; in web.config). Set true to enable the cache in debug-mode.
        /// </summary>
        public bool EnableInDebug { get; set; }

        /// <summary>
        /// By default, contains "{ DataFactoryCache.VersionKey }". 
        /// Set to null or empty array to disable the Episerver content cache dependency altogether, or add custom dependencies.
        /// </summary>
        public string[] CacheDependencyKeys { get; set; } = { DataFactoryCacheKeyPlaceholder };


        /// <param name="durationSeconds">Cache duration in seconds</param>
        public EpiserverWebApiOutputCacheAttribute(int durationSeconds)
        {
            DurationSeconds = durationSeconds;
        }

        public override void OnActionExecuting(HttpActionContext ac)
        {
            if (!IsCacheable(ac.Request.Method))
            {
                return;
            }

            var cachekey = CreateCacheKey(ac.Request);
            var cache = GetCache();
            var cachedValue = cache.Get(cachekey) as CacheValue;

            if (cachedValue == null)
            {
                return;
            }

            ac.Response = ac.Request.CreateResponse();
            ac.Response.Content = new StringContent(cachedValue.Result);
            ac.Response.Content.Headers.ContentType = new MediaTypeHeaderValue(cachedValue.ContentType) { CharSet = cachedValue.Charset };
        }

        public override void OnActionExecuted(HttpActionExecutedContext ac)
        {
            if (!IsCacheable(ac.Request.Method) || ac.Exception != null)
            {
                return;
            }

            var cachekey = CreateCacheKey(ac.Request);
            var cache = GetCache();

            if (cache.Get(cachekey) == null)
            {
                var value = new CacheValue
                {
                    ContentType = ac.Response.Content.Headers.ContentType.MediaType,
                    Charset = ac.Response.Content.Headers.ContentType.CharSet,
                    Result = ac.Response.Content.ReadAsStringAsync().Result
                };

                var evictionPolicy = CreateCacheEvictionPolicy();
                cache.Insert(cachekey, value, evictionPolicy);
            }
        }

        private bool IsCacheable(HttpMethod method)
        {
            if (!EnableInDebug && HttpContext.Current.IsDebuggingEnabled)
            {
                return false;
            }

            if (DisableForAuthenticated && HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return false;
            }

            return (DurationSeconds > 0) && (method == HttpMethod.Get);
        }

        private CacheEvictionPolicy CreateCacheEvictionPolicy()
        {
            if (CacheDependencyKeys == null || !CacheDependencyKeys.Any())
            {
                return null;
            }

            var keyCreator = ServiceLocator.Current.GetInstance<IContentCacheKeyCreator>();
            if (CacheDependencyKeys.Contains(DataFactoryCacheKeyPlaceholder))
            {
                //Replace the placeholder variable with the actual datafactory key. This is done only once
                CacheDependencyKeys = CacheDependencyKeys.Select(x => x.Replace(DataFactoryCacheKeyPlaceholder, keyCreator.VersionKey)).ToArray();
            }

            // If "DataFactoryCache.Version" key is used as a dependency the key must exists in the cache. 
            // Otherwise entries are not cached. The key is removed when a remote server content is updated. 
            if (CacheDependencyKeys.Contains(keyCreator.VersionKey))
            {
                // Version call ensures that the key is present
                var version = ServiceLocator.Current.GetInstance<IContentCacheVersion>().Version;
            }

            return new CacheEvictionPolicy(TimeSpan.FromSeconds(DurationSeconds), CacheTimeoutType.Absolute, CacheDependencyKeys);
        }

        private static string CreateCacheKey(HttpRequestMessage request)
        {
            const string cacheKeyBase = "Solita:EpiserverWebApiOutputCacheAttribute#";
            return cacheKeyBase + request.RequestUri.AbsoluteUri;
        }

        private static IObjectInstanceCache GetCache()
        {
            return ServiceLocator.Current.GetInstance<IObjectInstanceCache>();
        }

        private class CacheValue
        {
            public string ContentType { get; set; }
            public string Charset { get; set; }
            public string Result { get; set; }
        }
    }
}
