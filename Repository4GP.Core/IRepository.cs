using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Repository4GP.Core
{
    /// <summary>
    /// Repository
    /// </summary>
    public interface IRepository<TModel, in TKey> : IReadOnlyRepository<TModel, TKey>
        where TModel : class, IModel<TKey>
    {
        

        /// <summary>
        /// Insert a new item
        /// </summary>
        /// <param name="model">Item to add</param>
        Task Insert(TModel model);
        
        
        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="model">Item to update</param>
        /// <exception cref="ConcurrencyException">Thrown when the data are changed in the meanwhile</exception>
        Task Update(TModel model);


        /// <summary>
        /// Delete an item
        /// </summary>
        /// <param name="model">Item to delete</param>
        /// <exception cref="ConcurrencyException">Thrown when the data are changed in the meanwhile</exception>
        Task Delete(TModel model);


        /// <summary>
        /// Delete an item by the given keu
        /// </summary>
        /// <param name="key">Key of the item to delete</param>
        /// <param name="concurrencyToken">Concurrency token of the model</param>
        /// <exception cref="ConcurrencyException">Thrown when the data are changed in the meanwhile</exception>
        Task Delete(TKey key, string concurrencyToken);


    }
}
