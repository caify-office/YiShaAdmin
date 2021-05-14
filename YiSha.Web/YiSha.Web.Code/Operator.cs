using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using YiSha.Cache;
using YiSha.Util.Model;
using YiSha.Web.Code.State;

namespace YiSha.Web.Code
{
    public class Operator
    {
        public static Operator Instance => new();

        private readonly string _loginProvider = GlobalContext.Configuration.GetSection("SystemConfig:LoginProvider").Value;
        private const string _TokenName = "UserToken"; //cookie name or session name

        public async Task AddCurrent(string token)
        {
            switch (_loginProvider)
            {
                case "Cookie":
                    CookieHelper.WriteCookie(_TokenName, token);
                    break;

                case "Session":
                    SessionHelper.WriteSession(_TokenName, token);
                    break;

                case "WebApi":
                    var user = await new DataRepository().GetUserByToken(token);
                    if (user != null)
                    {
                        CacheFactory.Cache.SetCache(token, user);
                    }
                    break;

                default: throw new Exception("未找到LoginProvider配置");
            }
        }

        /// <summary>
        /// Api接口需要传入apiToken
        /// </summary>
        public void RemoveCurrent(string apiToken = "")
        {
            switch (_loginProvider)
            {
                case "Cookie":
                    CookieHelper.RemoveCookie(_TokenName);
                    break;

                case "Session":
                    SessionHelper.RemoveSession(_TokenName);
                    break;

                case "WebApi":
                    CacheFactory.Cache.RemoveCache(apiToken);
                    break;

                default: throw new Exception("未找到LoginProvider配置");
            }
        }

        private string GetToken(string apiToken = "")
        {
            var hca = GlobalContext.ServiceProvider?.GetService<IHttpContextAccessor>();
            return _loginProvider switch
            {
                "Cookie" => hca?.HttpContext != null ? CookieHelper.GetCookie(_TokenName) : "",
                "Session" => hca?.HttpContext != null ? SessionHelper.GetSession(_TokenName) : "",
                "WebApi" => apiToken,
                _ => ""
            };
        }

        /// <summary>
        /// Api接口需要传入apiToken
        /// </summary>
        public async Task<OperatorInfo> Current(string apiToken = "")
        {
            string token = GetToken(apiToken)?.Trim('"');
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var user = CacheFactory.Cache.GetCache<OperatorInfo>(token);
            if (user != null)
            {
                return user;
            }

            user = await new DataRepository().GetUserByToken(token);
            if (user != null)
            {
                CacheFactory.Cache.SetCache(token, user);
            }
            return user;
        }
    }
}