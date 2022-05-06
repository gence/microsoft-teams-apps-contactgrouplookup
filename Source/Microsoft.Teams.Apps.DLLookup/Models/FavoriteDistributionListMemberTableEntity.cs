// <copyright file="FavoriteDistributionListMemberTableEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    using Azure;
    using Azure.Data.Tables;

    /// <summary>
    /// Favorite Distribution List Member Data table entity class represents pinned member records.
    /// </summary>
    public class FavoriteDistributionListMemberTableEntity : ITableEntity
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
        /// Gets or sets pinned record's distribution list GUID.
        /// </summary>
        public string DistributionListId { get; set; }

        /// <summary>
        /// Gets or sets Partition key with users's object id.
        /// </summary>
        public string UserObjectId
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Gets or sets row key with pinned record id + Distribution list id.
        /// </summary>
        public string DistributionListMemberId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }
    }
}