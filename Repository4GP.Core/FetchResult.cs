using System;
using System.Collections.Generic;
using System.Linq;

namespace Repository4GP.Core
{

    /// <summary>
    /// Result of a fetch call
    /// </summary>
    /// <typeparam name="TModel">Type of the model</typeparam>
    public class FetchResult<TModel> 
        where TModel : class
    {

        /// <summary>
        /// Create a new instance of fetch result
        /// </summary>
        /// <param name="items">List of fetched items</param>
        /// <param name="paginationToken">Token for pagination</param>
        public FetchResult(IEnumerable<TModel> items, string paginationToken = null)
        {
            Items = items ?? Enumerable.Empty<TModel>();
            PaginationToken = paginationToken;
        }

        /// <summary>
        /// List of all fetched models
        /// </summary>
        public IEnumerable<TModel> Items { get; }

        /// <summary>
        /// Pagination token (for the FetchNext method)
        /// </summary>
        public string PaginationToken { get; }
        
    }
}
