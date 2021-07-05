using System;
using System.Collections.Generic;
using System.Linq;

namespace Repository4GP.Core
{


    /// <summary>
    /// Pagination info for a fetch
    /// </summary>
    public class FetchCriteria<TModel>
        where TModel : class
    {
        
        /// <summary>
        /// Number of items for each page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Filter to apply
        /// </summary>
        public Func<TModel, bool> Filter { get; set; }

        /// <summary>
        /// Order of the fetch result
        /// </summary>
        public OrderByInfo OrderBy { get; } = new OrderByInfo();
        
    }


    /// <summary>
    /// Info about order by clause
    /// </summary>
    public class OrderByInfo 
    {

        /// <summary>
        /// Items of the order by clause
        /// </summary>
        public List<OrderByItem> Items { get; }  = new List<OrderByItem>();


        /// <summary>
        /// True if the there is any item to sort for
        /// </summary>
        public bool HasOrder()
        {
            return Items.Any();
        }

        
        /// <summary>
        /// Add an order by clause, sorted ascending
        /// </summary>
        /// <param name="propertyName">Name of the property to order by</param>
        public void AddAscending(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            Items.Add(new OrderByItem(propertyName, SortOrder.Ascending));
        }
        
        /// <summary>
        /// Add an order by clause, sorted descending
        /// </summary>
        /// <param name="propertyName">Name of the property to order by</param>
        public void AddDescending(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            Items.Add(new OrderByItem(propertyName, SortOrder.Descending));
        }
    }

    /// <summary>
    /// Info about sorting for a property
    /// </summary>
    public class OrderByItem
    {

        /// <summary>
        /// Create a new instance of OrderByItem
        /// </summary>
        /// <param name="propertyName">Name of the property to order by</param>
        /// <param name="order">Sort order</param>
        public OrderByItem(string propertyName, SortOrder order)
        {
            if (string.IsNullOrEmpty(propertyName)) throw new ArgumentNullException(nameof(propertyName));

            PropertyName = propertyName;
            Order = order;
        }

        /// <summary>
        /// Name of the property to order by
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Sort order
        /// </summary>
        public SortOrder Order { get; } = SortOrder.Ascending;

    }


    /// <summary>
    /// Sort of the item pagination
    /// </summary>
    public enum SortOrder
    {
        ///<summary>
        /// Sort ascending
        ///</summary>
        Ascending = 0,
        ///<summary>
        /// Sort descending
        ///</summary>
        Descending = 10
    }



}
