// <copyright file="UserPageSizeChoiceTableEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using Azure;
using Azure.Data.Tables;

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    /// <summary>
    /// User page size choice table entity class used to represent user's page size choices.
    /// </summary>
    public class UserPageSizeChoiceTableEntity : ITableEntity
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
        /// Gets or sets distribution list page size.
        /// </summary>
        public int DistributionListPageSize { get; set; }

        /// <summary>
        /// Gets or sets distribution list members page size.
        /// </summary>
        public int DistributionListMemberPageSize { get; set; }

        /// <summary>
        /// Gets or sets Partition key with "default" value.
        /// </summary>
        public string DefaultValue
        {
            get { return this.PartitionKey; }
            set { this.PartitionKey = value; }
        }

        /// <summary>
        /// Gets or sets Row key with user's AAD object Id.
        /// </summary>
        public string UserObjectId
        {
            get { return this.RowKey; }
            set { this.RowKey = value; }
        }
    }
}
