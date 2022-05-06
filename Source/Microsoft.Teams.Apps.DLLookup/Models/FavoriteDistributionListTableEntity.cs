// <copyright file="FavoriteDistributionListTableEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    /// <summary>
    /// Favorite Distribution List data table entity class used to represent pinned distribution list records.
    /// </summary>
    public class FavoriteDistributionListTableEntity : ITableEntity
    {
        /// <inheritdoc/>
        public string PartitionKey { get; set; }

        /// <inheritdoc/>
        public string RowKey { get; set; }

        /// <inheritdoc/>
        public DateTimeOffset? Timestamp { get; set; }

        /// <inheritdoc/>
        public ETag ETag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether record is pinned or not.
        /// </summary>
        public bool PinStatus { get; set; }

        /// <summary>
        /// Gets or sets Row key with distribution list id.
        /// </summary>
        public string GroupId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }

        /// <summary>
        /// Gets or sets partition key with user's object Id.
        /// </summary>
        public string UserObjectId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }
    }
}