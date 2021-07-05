using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Repository4GP.Core;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{

    /// <summary>
    /// Readonly repository that works with Vision file
    /// </summary>
    /// <typeparam name="TModel">Model</typeparam>
    /// <typeparam name="TKey">Type of the model key</typeparam>
    public abstract class VisionReadOnlyRepository<TModel, TKey> : IReadOnlyRepository<TModel, TKey>
        where TModel : class, IModel<TKey>, new()
    {

        /// <summary>
        /// Create a new instance of a read only vision file repository
        /// </summary>
        /// <param name="visionFileSystem">Vision file system</param>
        /// <param name="fileName">Vision file name</param>
        /// <param name="paginationTokenManager">Pagination token manager</param>
        /// <param name="cache">Memory cache</param>
        /// <param name="logger">Logger</param>
        public VisionReadOnlyRepository(string fileName, 
                                              IVisionFileSystem visionFileSystem, 
                                              IPaginationTokenManager paginationTokenManager,
                                              IMemoryCache cache,
                                              ILogger logger)
        {
            if (visionFileSystem == null) throw new ArgumentNullException(nameof(visionFileSystem));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (paginationTokenManager == null) throw new ArgumentNullException(nameof(paginationTokenManager));
            if (cache == null) throw new ArgumentNullException(nameof(cache));

            VisionFileSystem = visionFileSystem;
            FileName = fileName;
            PaginationTokenManager = paginationTokenManager;
            Cache = cache;
            Logger = logger ?? NullLogger.Instance;
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
        /// Logger
        /// </summary>
        protected ILogger Logger { get; }


        /// <summary>
        /// Name of the vision file
        /// </summary>
        protected string FileName { get; }


        /// <summary>
        /// Vision file ssytem
        /// </summary>
        protected IVisionFileSystem VisionFileSystem { get; }
        

        /// <inheritdoc />
        public virtual Task<FetchResult<TModel>> Fetch(FetchCriteria<TModel> criteria = null)
        {
            var items = FetchAll();
            var count = items.Count;

            items = ApplyCriteria(items, criteria);
            var criteriaCount = items.Count;

            items = ApplyPagination(items, criteria.PageSize);
            var paginatedCount = items.Count;

            var token = GetPaginationToken(criteria);

            Logger.LogDebug("Read {Count} records, where {CriteriaCount} match criteria, returned {PaginatedCount} " +
                             "after pagination, pagination token {Token}",
                             count, criteriaCount, paginatedCount, token);

            return Task.FromResult(new FetchResult<TModel> (items, token));
        }   


        /// <summary>
        /// Apply the criteria to a list
        /// </summary>
        /// <param name="items">List where apply the criteria</param>
        /// <param name="criteria">Criteria to apply</param>
        /// <returns>List with applied criteria</returns>
        protected virtual List<TModel> ApplyCriteria(List<TModel> items, FetchCriteria<TModel> criteria)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (!items.Any() || criteria == null) return items;

            // Filter
            var filter = criteria?.Filter;
            if (filter != null)
            {
                items = items.Where(x => filter(x)).ToList();
            }
            
            // Order by
            var orderedList = ApplySort(items, criteria.OrderBy);

            return orderedList;
        }


        /// <summary>
        /// Orders the list by the sort clause
        /// </summary>
        /// <param name="source">List to order</param>
        /// <param name="orderByInfo">Sort clause</param>
        /// <returns>Ordered list</returns>
        protected virtual List<TModel> ApplySort(List<TModel> source, OrderByInfo orderByInfo)
        {
            if (source == null) return null;
            if (orderByInfo == null || !orderByInfo.Items.Any()) return source;

            var orderByString = new StringBuilder();
            var separator = string.Empty;
            foreach (var orderInfo in orderByInfo.Items)
            {
                orderByString.Append(separator);
                separator = ", ";
                if (orderInfo.Order == SortOrder.Descending)
                {
                    orderByString.Append($"{orderInfo.PropertyName} DESC");
                }
                else
                {
                    orderByString.Append(orderInfo.PropertyName);
                }
            }

            return source.AsQueryable().OrderBy(orderByString.ToString()).ToList();
        }


        /// <summary>
        /// Apply the pagination to a list
        /// </summary>
        /// <param name="source">List to paginate</param>
        /// <param name="pageSize">Size of a page</param>
        /// <param name="currentPage">Page to extract (0 based)</param>
        /// <returns>Paginated list</returns>
        protected virtual List<TModel> ApplyPagination(List<TModel> source, int pageSize, int currentPage = 0)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (pageSize < 1) return source;

            // Take/Skip
            var take = pageSize; 
            var skip = (currentPage * pageSize);

            // Apply pagination
            var result = skip > 0 ? source.Skip(skip).Take(take) : source.Take(take);

            // Result
            return result.ToList();
        }


        /// <summary>
        /// Gets the pagination token
        /// </summary>
        /// <param name="criteria">Criteria of the pagination token</param>
        /// <param name="currentPage">Current page</param>
        /// <returns>Pagination token or null if there is no pagination</returns>
        protected string GetPaginationToken(FetchCriteria<TModel> criteria, int currentPage = 0)
        {
            var pageSize = criteria?.PageSize;
            if (!pageSize.HasValue || pageSize.Value == 0) return null;

            return PaginationTokenManager.CreateToken(new PaginationTokenInfo<TModel> {
                Criteria = criteria,
                CurrentPage = currentPage
            });
        }


        /// <summary>
        /// Gets all items from cache or from the file if cache is invalid
        /// </summary>
        /// <returns>List of all items</returns>
        protected virtual List<TModel> FetchAll()
        {
            var key = $"{FileName}__{typeof(TModel).FullName}";
            
            // Check for cached data
            var fileDate = File.GetLastWriteTime(FileName);
            if (Cache.TryGetValue<VisionCacheFileEntry<TModel>>(key, out VisionCacheFileEntry<TModel> cacheValue))
            {
                if (fileDate == cacheValue.FileLastWriteTimeUtc)
                {
                    var result = cacheValue.Items;
                    Logger.LogDebug("Fetch {ResultCount} items from cache with key {Key} for file {FileName}",
                                     result.Count, key, FileName);
                    return result;
                }
                Cache.Remove(key);
            }

            // Load data to cache
            lock (cacheSyncObject)
            {
                if (Cache.TryGetValue<VisionCacheFileEntry<TModel>>(key, out VisionCacheFileEntry<TModel> value))
                {
                    return value.Items;
                }

                var items = FetchAllFromFile();
                Logger.LogDebug("Fetch {ItemsCount} items from file {FileName}. Stored them to cache with key {Key}",
                                items.Count, FileName, key);
                var keyEntry = new VisionCacheFileEntry<TModel> 
                {
                    FileLastWriteTimeUtc = fileDate,
                    Items = items                
                };
                Cache.Set(key, items, new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(10) });

                // Returns data
                return items;
            }
        }


        private static object cacheSyncObject = new object();


        /// <inheritdoc />
        public virtual Task<FetchResult<TModel>> FetchNext(string paginationToken)
        {
            if (string.IsNullOrEmpty(paginationToken)) throw new ArgumentNullException(nameof(paginationToken));

            var paginationInfo = (PaginationTokenInfo<TModel>)PaginationTokenManager.DecodeToken(paginationToken);
            if (paginationInfo == null) throw new ApplicationException($"Pagination token is not valid. Current value is '{paginationToken}'");

            Logger.LogDebug("Loading next page for file {FileName}, pagination token {PaginationToken}", FileName, paginationToken);

            var criteria = paginationInfo.Criteria;

            var items = FetchAll();
            var count = items.Count;

            items = ApplyCriteria(items, criteria);
            var criteriaCount = items.Count;

            var currentPage = paginationInfo.CurrentPage.GetValueOrDefault() + 1;

            items = ApplyPagination(items, criteria.PageSize, currentPage);
            var paginatedCount = items.Count;

            var token = GetPaginationToken(criteria, currentPage);

            Logger.LogDebug("Read {RecordCount} records, where {CriteriaCount} match criteria, returned {PaginatedCount} after pagination, " +
                            "new pagination token {Token}",
                            count, criteriaCount, paginatedCount, token);

            return Task.FromResult(new FetchResult<TModel> (items, token));
        }


        /// <inheritdoc />
        public virtual Task<TModel> GetByPk(TKey pk)
        {
            if (pk == null) throw new ArgumentNullException(nameof(pk));

            var items = FetchAll();

            return Task.FromResult(items.Where(x => x.Pk.Equals(pk)).SingleOrDefault());
        }


        /// <inheritdoc />
        public virtual Task<IEnumerable<TModel>> FetchByPks(IEnumerable<TKey> pks)
        {
            if (pks == null || !pks.Any()) throw new ArgumentNullException(nameof(pks));

            var items = FetchAll();

            return Task.FromResult(items.Where(x => pks.Contains(x.Pk)));
        }


        /// <summary>
        /// Gets a model from a record
        /// </summary>
        /// <param name="record">Vision record</param>
        /// <returns>Model for the given record, null if the record is not valid</returns>
        protected abstract TModel GetModel(IVisionRecord record);


        /// <summary>
        /// Fetch models reading all file
        /// </summary>
        /// <returns>Requested models</returns>
        protected virtual List<TModel> FetchAllFromFile()
        {   
            var items = new List<TModel>();
            IVisionRecord record;
            
            var mappedCount = 0;
            var notMappedCount = 0;
            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                Logger.LogDebug("Reading data from file {FilePath}", file.FilePath);
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
                        var model = GetModel(record);
                        if (model == null) 
                        {
                            notMappedCount += 1;
                        }
                        else
                        {
                            mappedCount += 1;
                            items.Add(model);
                        }
                    }

                }
                file.Close();
            }

            Logger.LogDebug("Read {RecordCount}, {MappedCount} mapped to entities, {NotMappedCount} not mapped",
                            mappedCount + notMappedCount, mappedCount, notMappedCount);
            return items;
        }


    
    }
}
