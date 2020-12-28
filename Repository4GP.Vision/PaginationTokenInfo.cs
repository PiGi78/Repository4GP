using System;
using System.Text;
using System.Text.Json;
using Repository4GP.Core;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{

    /// <summary>
    /// Info stored in a pagination token
    /// </summary>
    public class PaginationTokenInfo<TModel>
        where TModel: class
    {

        /// <summary>
        /// Criteria used for fetch data
        /// </summary>
        public FetchCriteria<TModel> Criteria { get; set; }

        /// <summary>
        /// Last record of the page
        /// </summary>
        public IVisionRecord LastRecord { get; set; }


        /// <summary>
        /// Index of the key to use
        /// </summary>
        public int? KeyIndex { get; set; }
        

        /// <summary>
        /// Current page
        /// </summary>
        public int? CurrentPage { get; set; }
    }
}
