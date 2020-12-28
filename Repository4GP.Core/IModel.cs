using System;

namespace Repository4GP.Core
{

    /// <summary>
    /// Defines a model
    /// </summary>
    public interface IModel<TKey>
    {
        
        /// <summary>
        /// Primary key
        /// </summary>
        TKey Pk { get; set; }
    }
}
