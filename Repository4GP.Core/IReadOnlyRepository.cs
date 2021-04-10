using System.Threading.Tasks;
using System.Collections.Generic;

namespace Repository4GP.Core
{
    /// <summary>
    /// Read only repository
    /// </summary>
    public interface IReadOnlyRepository<TModel, in TKey>
        where TModel : class, IReadOnlyModel<TKey>
    {
        
        /// <summary>
        /// Fetch all models
        /// </summary>
        /// <param name="criteria">Criteria to apply</param>
        /// <returns>Result of the fetch</returns>
        Task<FetchResult<TModel>> Fetch(FetchCriteria<TModel> criteria = null);


        /// <summary>
        /// Fetch next page of models
        /// </summary>
        /// <param name="paginationToken">Token for pagination</param>
        /// <returns>Next page of a pagination</returns>
        Task<FetchResult<TModel>> FetchNext(string paginationToken);


        /// <summary>
        /// Get a model by the given primary key
        /// </summary>
        /// <param name="pk">Primary key to look for</param>
        /// <returns>Requested model or null if not found</returns>
        Task<TModel> GetByPk(TKey pk);


        /// <summary>
        /// Fetch models with given primary keys
        /// </summary>
        /// <param name="pks">Primary keys to look for</param>
        /// <returns>All models that match the given list of primary keys</returns>
        Task<IEnumerable<TModel>> FetchByPks(IEnumerable<TKey> pks);


    }
}
