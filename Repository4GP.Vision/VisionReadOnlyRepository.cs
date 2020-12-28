using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Repository4GP.Core;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{

    /// <summary>
    /// Read only repository that works with vision file
    /// </summary>
    /// <typeparam name="TModel">Model type</typeparam>
    /// <typeparam name="TKey">Model primary key type</typeparam>
    public abstract class VisionReadOnlyRepository<TModel, TKey> : IReadOnlyRepository<TModel, TKey>
        where TModel : class, IModel<TKey>, new()
    {

        /// <summary>
        /// Create a new instance of a read only vision file repository
        /// </summary>
        /// <param name="visionFileSystem">Vision file system</param>
        /// <param name="fileName">Vision file name</param>
        /// <param name="paginationTokenManager">Pagination token manager</param>
        public VisionReadOnlyRepository(IVisionFileSystem visionFileSystem, string fileName, IPaginationTokenManager paginationTokenManager)
        {
            if (visionFileSystem == null) throw new ArgumentNullException(nameof(visionFileSystem));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));
            if (paginationTokenManager == null) throw new ArgumentNullException(nameof(paginationTokenManager));

            VisionFileSystem = visionFileSystem;
            FileName = fileName;
            PaginationTokenManager = paginationTokenManager;
        }


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
        public virtual Task<FetchResult<TModel>> Fetch(FetchCriteria<TModel> criteria = null)
        {
            if (criteria?.OrderBy == null)
            {
                return FetchByIndex(criteria, 0);
            }
            return FetchAll(criteria);
        }


        /// <summary>
        /// Fetch next page of models
        /// </summary>
        /// <param name="paginationToken">Token for pagination</param>
        /// <returns>Next page of a pagination</returns>
        public virtual Task<FetchResult<TModel>> FetchNext(string paginationToken)
        {
            if (string.IsNullOrEmpty(paginationToken)) throw new ArgumentNullException(nameof(paginationToken));

            var paginationInfo = (PaginationTokenInfo<TModel>)PaginationTokenManager.DecodeToken(paginationToken);
            if (paginationInfo == null) throw new ApplicationException($"Pagination token is not valid. Current value is '{paginationToken}'");

            if (paginationInfo.KeyIndex.HasValue)
            {
                return FetchByIndex(paginationInfo.Criteria, paginationInfo.KeyIndex.Value, paginationInfo.LastRecord);
            }
            else
            {
                return FetchAll(paginationInfo.Criteria, paginationInfo.CurrentPage.Value);
            }
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
                file.Open(FileOpenMode.Input);

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
        /// Fetch data using a specific zero based index
        /// </summary>
        /// <param name="criteria">Fetch criteria</param>
        /// <param name="keyIndex">Index (key) to use, zero based</param>
        /// <param name="startRecord">Start record</param>
        /// <returns>Result of the fetch</returns>
        protected Task<FetchResult<TModel>> FetchByIndex(FetchCriteria<TModel> criteria, int keyIndex, IVisionRecord startRecord = null)
        {
            var items = new List<TModel>();
            var filter = criteria?.Filter;
            var pageSize = criteria == null ? 0 : criteria.PageSize;
            var count = 0;
            IVisionRecord record = startRecord;

            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                file.Open(FileOpenMode.Input);

                var startMode = startRecord == null ? FileStartMode.GreaterOrEqual : FileStartMode.Greater;
                if (file.Start(keyIndex, startRecord, startMode))
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

                        // Check for filter
                        if (filter == null ||
                            filter(model))
                        {
                            items.Add(model);

                            // End of the page
                            if (pageSize > 0)
                            {
                                count++;
                                if (count == pageSize) break;
                            }
                        }
                    }

                }
                file.Close();
            }

            // Pagination token, if needed
            string paginationToken = null;
            if (pageSize > 0 && record != null)
            {
                var info = new PaginationTokenInfo<TModel>
                {
                    Criteria = criteria,
                    LastRecord = record,
                    KeyIndex = keyIndex
                };
                paginationToken = PaginationTokenManager.CreateToken(info);
            }

            // result
            var result = new FetchResult<TModel>(items, paginationToken);
            return Task.FromResult(result);
        }


        /// <summary>
        /// Fetch models reading all file
        /// </summary>
        /// <param name="criteria">Criteria</param>
        /// <param name="currentPage">Page to load</param>
        /// <returns>Requested models</returns>
        protected Task<FetchResult<TModel>> FetchAll(FetchCriteria<TModel> criteria, int currentPage = 0)
        {
            var items = new List<TModel>();
            var filter = criteria?.Filter;
            int page = currentPage;
            int pageSize = (criteria?.PageSize).HasValue ? criteria.PageSize : 0;
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

                        // Check for filter
                        if (filter == null ||
                            filter(model))
                        {
                            items.Add(model);
                        }
                    }

                }
                file.Close();
            }

            string paginationToken = null;
            if (pageSize > 0 && pageSize < items.Count())
            {
                var info = new PaginationTokenInfo<TModel>
                {
                    Criteria = criteria,
                    CurrentPage = page + 1
                };
                paginationToken = PaginationTokenManager.CreateToken(info);
                var skip = page * pageSize;
                items = items.Skip(skip).Take(pageSize).ToList();
            }

            var result = new FetchResult<TModel>(items, paginationToken);
            return Task.FromResult(result);
        }

    }
}
