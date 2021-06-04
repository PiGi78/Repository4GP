using System;

namespace Repository4GP.Core
{

    /// <summary>
    /// Defines a read only model
    /// </summary>
    public interface IModel<TKey>
    {
        
        /// <summary>
        /// Primary key
        /// </summary>
        TKey Pk { get; set; }

        
        /// <summary>
        /// Token used for manage the concurrency
        /// </summary>
        string ConcurrencyToken { get; set; }
    }

}
