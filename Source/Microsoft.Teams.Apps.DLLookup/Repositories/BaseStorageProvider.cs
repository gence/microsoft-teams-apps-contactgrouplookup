// <copyright file="BaseStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    using System;
    using Azure.Data.Tables;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.DLLookup.Models;

    /// <summary>
    /// Implements storage provider which initializes table if not exists and provide table client instance.
    /// </summary>
    public class BaseStorageProvider
    {
        /// <summary>
        /// Microsoft Azure Table storage connection string.
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStorageProvider"/> class.
        /// </summary>
        /// <param name="storageOptions">A set of key/value application storage configuration properties.</param>
        /// <param name="tableName">Table name of azure table storage to initialize.</param>
        public BaseStorageProvider(IOptionsMonitor<StorageOptions> storageOptions, string tableName)
        {
            storageOptions = storageOptions ?? throw new ArgumentNullException(nameof(storageOptions));
            this.connectionString = storageOptions.CurrentValue.ConnectionString ?? throw new ArgumentNullException(nameof(storageOptions));
            this.TableName = tableName;
            this.InitializeTableClient();
        }

        /// <summary>
        /// Gets or sets Microsoft Azure Table storage table name.
        /// </summary>
        protected string TableName { get; set; }

        /// <summary>
        /// Gets or sets Microsoft Azure Table service client.
        /// </summary>
        protected TableClient DLTableClient { get; set; }

        /// <summary>
        /// Create storage table if it does not exist.
        /// </summary>
        private void InitializeTableClient()
        {
            var options = new TableClientOptions();
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.Mode = Azure.Core.RetryMode.Exponential;
            options.Retry.MaxRetries = 3;

            var serviceClient = new TableServiceClient(this.connectionString, options);
            this.DLTableClient = serviceClient.GetTableClient(this.TableName);

            return;
        }
    }
}
