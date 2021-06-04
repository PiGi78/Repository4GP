using System.Security.Cryptography;
using System.Text;
using Vision4GP.Core.FileSystem;

namespace Repository4GP.Vision
{


    /// <summary>
    /// Extensions for vision
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Get the concurrency token for a given record
        /// </summary>
        /// <param name="record">Record for which compute the token</param>
        /// <returns>Concurrency token</returns>
        public static string GetConcurrencyToken(this IVisionRecord record)
        {
            if (record == null) return string.Empty;

            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(record.GetRawContent());
                return Encoding.ASCII.GetString(result);
            }
        }

    }
}