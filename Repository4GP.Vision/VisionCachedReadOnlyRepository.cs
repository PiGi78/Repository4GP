using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Repository4GP.Core;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{
    public abstract class VisionCachedReadOnlyRepository<TModel, TKey> : IReadOnlyRepository<TModel, TKey>
        where TModel : class, IModel<TKey>, new()
    {

        /// <summary>
        /// Create a new instance of a read only vision file repository that uses cache
        /// </summary>
        /// <param name="visionFileSystem">Vision file system</param>
        /// <param name="fileName">Vision file name</param>
        /// <param name="paginationTokenManager">Pagination token manager</param>
        /// <param name="cache">Memory cache</param>
        public VisionCachedReadOnlyRepository(IVisionFileSystem visionFileSystem, 
                                              string fileName, 
                                              IPaginationTokenManager paginationTokenManager,
                                              IMemoryCache cache)
        {
            if (visionFileSystem == null) throw new ArgumentNullException(nameof(visionFileSystem));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (paginationTokenManager == null) throw new ArgumentNullException(nameof(paginationTokenManager));

            VisionFileSystem = visionFileSystem;
            FileName = fileName;
            PaginationTokenManager = paginationTokenManager;
        }

        /// <summary>
        /// Cache
        /// </summary>
        protected IMemoryCache Cache { get; }


        /// <summary>
        /// Pagination token manager
        /// </summary>
        protected IPaginationTokenManager PaginationTokenManager { get; }


        /// <summary>
        /// Name of the vision file
        /// </summary>
        protected string FileName { get; }


        /// <summary>
        /// Vision file ssytem
        /// </summary>
        protected IVisionFileSystem VisionFileSystem { get; }
        

        /// <summary>
        /// Fetch all models
        /// </summary>
        /// <param name="criteria">Criteria to apply</param>
        /// <returns>Result of the fetch</returns>
        public virtual async Task<FetchResult<TModel>> Fetch(FetchCriteria<TModel> criteria = null)
        {
            var items = await FetchAll();
            
            // Filter
            var filter = criteria?.Filter;
            if (filter != null)
            {
                items = items.Where(x => filter(x)).ToList();
            }
            

            // Pagination
            int pageSize = (criteria?.PageSize).HasValue ? criteria.PageSize : 0;
            string paginationToken = null;
            if (pageSize < items.Count())
            {
                var info = new PaginationTokenInfo<TModel>
                {
                    Criteria = criteria,
                    CurrentPage = 1
                };
                paginationToken = PaginationTokenManager.CreateToken(info);
                items = items.Take(criteria.PageSize).ToList();
            }

            var result = new FetchResult<TModel>(items, paginationToken);
            return result;

        }

        /// <summary>
        /// Cache key to store file data
        /// </summary>
        protected virtual string GetCacheKey()
        {
            return $"{FileName}_{typeof(TModel).FullName}";
        }


        /// <summary>
        /// Gets all items from cache or from the file if cache is invalid
        /// </summary>
        /// <returns>List of all items</returns>
        protected virtual async Task<List<TModel>> FetchAll()
        {
            var key = GetCacheKey();
            
            var fileDate = File.GetLastWriteTime(FileName);
            if (Cache.TryGetValue<VisionCacheFileEntry<TModel>>(key, out VisionCacheFileEntry<TModel> cacheValue))
            {
                if (fileDate == cacheValue.FileLastWriteTimeUtc)
                {
                    return cacheValue.Items;
                }
            }

            var items = await FetchAllFromFile();
            var keyEntry = new VisionCacheFileEntry<TModel> 
            {
                FileLastWriteTimeUtc = fileDate,
                Items = items                
            };
            Cache.Set(key, items, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });

            return items;
        }


        /// <summary>
        /// Fetch next page of models
        /// </summary>
        /// <param name="paginationToken">Token for pagination</param>
        /// <returns>Next page of a pagination</returns>
        public virtual async Task<FetchResult<TModel>> FetchNext(string paginationToken)
        {
            if (string.IsNullOrEmpty(paginationToken)) throw new ArgumentNullException(nameof(paginationToken));

            var paginationInfo = (PaginationTokenInfo<TModel>)PaginationTokenManager.DecodeToken(paginationToken);
            if (paginationInfo == null) throw new ApplicationException($"Pagination token is not valid. Current value is '{paginationToken}'");

            var criteria = paginationInfo.Criteria;
            var items = await FetchAll();

            // Filter
            var filter = criteria.Filter;
            if (filter != null)
            {
                items = items.Where(x => filter(x)).ToList();
            }
            

            // Pagination
            int pageSize = criteria.PageSize;
            var page = paginationInfo.CurrentPage.Value;
            string newToken = null;
            if (pageSize < items.Count())
            {
                var info = new PaginationTokenInfo<TModel>
                {
                    Criteria = criteria,
                    CurrentPage = page + 1
                };
                newToken = PaginationTokenManager.CreateToken(info);
                var skip = pageSize * page;
                items = items.Skip(skip).Take(pageSize).ToList();
            }

            var result = new FetchResult<TModel>(items, newToken);
            return result;
        }


        /// <summary>
        /// Get a model by the given primary key, null if not found
        /// </summary>
        /// <param name="pk">Primary key to look for</param>
        /// <returns>Requested model or null if not found</returns>
        public virtual async Task<TModel> GetByPk(TKey pk)
        {
            if (pk == null) throw new ArgumentNullException(nameof(pk));

            var items = await FetchByPks(new List<TKey> { pk });
            if (items.Any())
            {
                return items.Single();
            }
            return null;
        }


        /// <summary>
        /// Fetch models with given primary keys
        /// </summary>
        /// <param name="pks">Primary keys to look for</param>
        /// <returns>All models that match the given list of primary keys</returns>
        public virtual Task<IEnumerable<TModel>> FetchByPks(IEnumerable<TKey> pks)
        {
            if (pks == null || !pks.Any()) throw new ArgumentNullException(nameof(pks));
            
            var result = new List<TModel>();

            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                file.Open(FileOpenMode.InputOutput);

                foreach (var pk in pks)
                {
                    var record = file.GetNewRecord();
                    MapPrimaryKeyToRecord(pk, record);
                    var newRecord = file.Read(record);
                    if (newRecord != null)
                    {
                        var model = new TModel();
                        MapRecordToModel(record, model);
                        result.Add(model);
                    }
                }

                file.Close();
            }

            return Task.FromResult(result.AsEnumerable());
        }


        /// <summary>
        /// Map the primary key to a vision record
        /// </summary>
        /// <param name="pk">Primary key to map</param>
        /// <param name="record">Record where map the primary key</param>
        protected abstract void MapPrimaryKeyToRecord(TKey pk, IVisionRecord record);


        /// <summary>
        /// Map data from vision record to model
        /// </summary>
        /// <param name="record">Record where take the properties</param>
        /// <param name="model">Model where put the values</param>
        protected abstract void MapRecordToModel(IVisionRecord record, TModel model);


        /// <summary>
        /// Fetch models reading all file
        /// </summary>
        /// <returns>Requested models</returns>
        protected Task<List<TModel>> FetchAllFromFile()
        {
            var items = new List<TModel>();
            IVisionRecord record;
            
            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                file.Open(FileOpenMode.Input);
                if (file.Start())
                {
                    // Read next loop
                    while (true)
                    {
                        // Next record
                        record = file.ReadNext();
                        if (record == null) break;

                        // Convert to model
                        var model = new TModel();
                        MapRecordToModel(record, model);
                        
                        items.Add(model);
                    }

                }
                file.Close();
            }

            return Task.FromResult(items);
        }

    
    }
}
