using System;
using Microsoft.Extensions.Caching.Memory;

namespace Repository4GP.Core
{
    public class MemoryPaginationTokenManager : IPaginationTokenManager
    {
        
        /// <summary>
        /// Creates a new instance of pagination token manager that works with memory
        /// </summary>
        /// <param name="cache"></param>
        public MemoryPaginationTokenManager(IMemoryCache cache)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
        }


        /// <summary>
        /// Memory cache
        /// </summary>
        private IMemoryCache Cache { get; }


        /// <summary>
        /// Creates a new token for the given pagination info
        /// </summary>
        /// <param name="paginationInfo">Infos about the pagination</param>
        /// <returns>Requested token</returns>
        public string CreateToken(object paginationInfo)
        {
            string key = Guid.NewGuid().ToString();
            Cache.Set(key, paginationInfo, TimeSpan.FromMinutes(15));
            return key;
        }


        /// <summary>
        /// Decode the value of a token
        /// </summary>
        /// <param name="token">Token to decode</param>
        /// <returns>Info saved with the given token</returns>
        public object DecodeToken(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new ArgumentNullException(nameof(token));

            if (Cache.TryGetValue(token, out object result))
            {
                Cache.Remove(token);
                return result;
            }
            return null;
        }
    }
}
