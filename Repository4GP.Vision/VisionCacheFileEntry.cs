using System;
using System.Collections.Generic;

namespace Repository4GP.Vision
{

    /// <summary>
    /// Entry for cached file
    /// </summary>
    public class VisionCacheFileEntry<TModel>
    {

        /// <summary>
        /// Date/Time of the last operation on file
        /// </summary>
        public DateTime FileLastWriteTimeUtc { get;set; }
        

        /// <summary>
        /// List of all items
        /// </summary>
        public List<TModel> Items { get; set; }
    }
}
