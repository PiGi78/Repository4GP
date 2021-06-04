using System;
using System.Runtime.Serialization;

namespace Repository4GP.Core
{


    /// <summary>
    /// Concurrency exception
    /// </summary>
    [Serializable]
    public class ConcurrencyException : Exception
    {

        /// <summary>
        /// Creates a new instance of the exception
        /// </summary>
        public ConcurrencyException() { }


        /// <summary>
        /// Creates a new instance of the exception with the given message
        /// </summary>
        /// <param name="message">Exception message</param>
        public ConcurrencyException(string message) : base(message) { }


        /// <summary>
        /// Creates a new instance of the exception with the given message and inner exception
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="inner">Inner exception</param>
        public ConcurrencyException(string message, Exception inner) : base(message, inner) { }


        /// <summary>
        /// Create a new instance of the exception for serialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Serialization context</param>
        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        
    }

}