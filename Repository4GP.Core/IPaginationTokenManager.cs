using System;

namespace Repository4GP.Core
{

    /// <summary>
    /// Manager for pagination token
    /// </summary>
    public interface IPaginationTokenManager
    {

        /// <summary>
        /// Creates a new token for the given pagination info
        /// </summary>
        /// <param name="paginationInfo">Infos about the pagination</param>
        /// <returns>Requested token</returns>
        string CreateToken(object paginationInfo);


        /// <summary>
        /// Decode the value of a token
        /// </summary>
        /// <param name="token">Token to decode</param>
        /// <returns>Info saved with the given token</returns>
        object DecodeToken(string token);
    }
}
