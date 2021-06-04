using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Repository4GP.Core;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{


    /// <summary>
    /// Vision repository
    /// </summary>
    /// <typeparam name="TModel">Model type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public abstract class VisionRepository<TModel, TKey> : VisionReadOnlyRepository<TModel, TKey>, IRepository<TModel, TKey>
        where TModel : class, IModel<TKey>, new()
    {


        /// <summary>
        /// Create a new instance of a vision file repository
        /// </summary>
        /// <param name="visionFileSystem">Vision file system</param>
        /// <param name="fileName">Vision file name</param>
        /// <param name="paginationTokenManager">Pagination token manager</param>
        /// <param name="cache">Memory cache</param>
        /// <param name="logger">Logger</param>
        public VisionRepository(string fileName, 
                                IVisionFileSystem visionFileSystem, 
                                IPaginationTokenManager paginationTokenManager,
                                IMemoryCache cache,
                                ILogger logger) 
            : base(fileName, visionFileSystem, paginationTokenManager, cache, logger)
        {
        }


        /// <summary>
        /// Map the model key to the vision primary key
        /// </summary>
        /// <param name="modelKey">Key of the model</param>
        /// <param name="visionRecord">Record where to map the key</param>
        protected abstract void MapPrimaryKeyToVisionRecord(TKey modelKey, IVisionRecord visionRecord);


        /// <summary>
        /// Map the model to the vision record
        /// </summary>
        /// <param name="model">Model to map</param>
        /// <param name="record">Record where to map the model</param>
        protected abstract void MapModelToVisionRecord(TModel model, IVisionRecord record);



        /// <inheritdoc />
        public Task Insert(TModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Logger.LogDebug("Inserting model {Model}", model);
            
            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                file.Open(FileOpenMode.InputOutput);
                var record = file.GetNewRecord();
                MapModelToVisionRecord(model, record);
                file.Write(record);
                file.Close();
                Logger.LogDebug("Inserted record {Record}", record);
            }
            return Task.CompletedTask;
        }


        /// <inheritdoc />
        public Task Update(TModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            Logger.LogDebug("Updating model {Model}", model);
            
            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                // Read the record with lock
                file.Open(FileOpenMode.InputOutput);
                var record = file.GetNewRecord();

                // Check for concurrency
                MapPrimaryKeyToVisionRecord(model.Pk, record);
                var oldRecord = file.ReadLock(record);
                if (oldRecord == null) {
                    throw new ConcurrencyException($"Data for model with primary key '{model.Pk}' are deleted. Cannot update the record");
                }
                var oldModel = GetModel(oldRecord);
                if (model.ConcurrencyToken != oldModel.ConcurrencyToken) {
                    throw new ConcurrencyException($"Data for model with primary key '{model.Pk}' are changed since the last read. Cannot update the record");
                }

                // Update the record
                MapModelToVisionRecord(model, oldRecord);
                file.Rewrite(oldRecord);


                file.Close();
                Logger.LogDebug("Updated record {Record}", oldRecord);
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Delete(TModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            return Delete(model.Pk, model.ConcurrencyToken);
        }


        /// <inheritdoc />
        public Task Delete(TKey key, string concurrencyToken)
        {

            Logger.LogDebug("Deleting model with primary key '{Pk}'", key);
            
            using (var file = VisionFileSystem.GetVisionFile(FileName))
            {
                // Read the record with lock
                file.Open(FileOpenMode.InputOutput);
                var record = file.GetNewRecord();

                // Check for concurrency
                MapPrimaryKeyToVisionRecord(key, record);
                var oldRecord = file.ReadLock(record);
                if (oldRecord == null) {
                    return Task.CompletedTask;
                }
                var oldModel = GetModel(oldRecord);
                if (concurrencyToken != oldModel.ConcurrencyToken) {
                    throw new ConcurrencyException($"Data for model with primary key '{key}' are changed since the last read. Cannot delete the record");
                }

                // Delete the record
                file.Delete(oldRecord);

                file.Close();
                Logger.LogDebug("Deleted record {Record}", oldRecord);
            }
            return Task.CompletedTask;
        }

    }

}