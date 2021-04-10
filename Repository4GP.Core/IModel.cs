using System;

namespace Repository4GP.Core
{

    /// <summary>
    /// Defines a read only model
    /// </summary>
    public interface IModel<TKey> : IReadOnlyModel<TKey>
    {
        
        /// <summary>
        /// Token used for manage the concurrency
        /// </summary>
        string ConcurrencyToken { get; set; }
    }


    /// <summary>
    /// Defines a read only model
    /// </summary>
    public interface IReadOnlyModel<TKey>
    {
        
        /// <summary>
        /// Primary key
        /// </summary>
        TKey Pk { get; set; }
    }
}
