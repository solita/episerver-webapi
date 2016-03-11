﻿using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using EPiServer;
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
        public string[] CacheDependencyKeys { get; set; } = { DataFactoryCache.VersionKey };


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

                var evictionPolicy = (CacheDependencyKeys != null && CacheDependencyKeys.Any())
                                     ? new CacheEvictionPolicy(CacheDependencyKeys, TimeSpan.FromSeconds(DurationSeconds), CacheTimeoutType.Absolute)
                                     : null;
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
