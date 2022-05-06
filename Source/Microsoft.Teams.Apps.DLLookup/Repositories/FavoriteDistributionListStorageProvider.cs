﻿// <copyright file="FavoriteDistributionListStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Data.Tables;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.DLLookup.Models;

    /// <summary>
    /// The class contains read, create and delete operations for distribution list on table storage.
    /// </summary>
    public class FavoriteDistributionListStorageProvider : BaseStorageProvider
    {
        private const string FavoriteDistributionListsTableName = "FavoriteDistributionLists";

        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<FavoriteDistributionListStorageProvider> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FavoriteDistributionListStorageProvider"/> class.
        /// </summary>
        /// <param name="storageOptions">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public FavoriteDistributionListStorageProvider(
            IOptionsMonitor<StorageOptions> storageOptions,
            ILogger<FavoriteDistributionListStorageProvider> logger)
            : base(storageOptions, FavoriteDistributionListsTableName)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets all favorite Distribution Lists from table storage.
        /// </summary>
        /// <param name="userObjectId">User's Azure Active Directory Id.</param>
        /// <returns>List of favorite Distribution List entities.</returns>
        public async Task<IEnumerable<FavoriteDistributionListTableEntity>> GetFavoriteDistributionListsFromStorageAsync(string userObjectId)
        {
            try
            {
                await this.EnsureInitializedAsync();
                AsyncPageable<FavoriteDistributionListTableEntity> queryResults = this.DLTableClient.QueryAsync<FavoriteDistributionListTableEntity>(filter: TableClient.CreateQueryFilter($"PartitionKey eq {userObjectId}"));
                List<FavoriteDistributionListTableEntity> result = new List<FavoriteDistributionListTableEntity>();

                await foreach (var p in queryResults.AsPages())
                {
                    result.AddRange(p.Values);
                }

                return result;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in GetFavoriteDistributionListsFromStorageAsync.");
                throw;
            }
        }

        /// <summary>
        /// Adds favorite distribution list to storage.
        /// </summary>
        /// <param name="favoriteDistributionListDataEntity">Distribution list entity to be added as favorite.</param>
        /// <returns>Add operation response.</returns>
        public async Task AddFavoriteDistributionListToStorageAsync(FavoriteDistributionListTableEntity favoriteDistributionListDataEntity)
        {
            try
            {
                await this.EnsureInitializedAsync();
                await this.DLTableClient.UpsertEntityAsync<FavoriteDistributionListTableEntity>(favoriteDistributionListDataEntity);
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in AddFavoriteDistributionListToStorageAsync: UserObjectId: {favoriteDistributionListDataEntity.UserObjectId}.");
                throw;
            }
        }

        /// <summary>
        /// Delete an entity in the table storage.
        /// </summary>
        /// <param name="favoriteDistributionListEntity">Distribution list entity to be removed as favorite.</param>
        /// <returns>A delete task that represents the work queued to execute.</returns>
        public async Task RemoveFavoriteDistributionListFromStorageAsync(FavoriteDistributionListTableEntity favoriteDistributionListEntity)
        {
            try
            {
                await this.EnsureInitializedAsync();
                await this.DLTableClient.DeleteEntityAsync(favoriteDistributionListEntity.PartitionKey, favoriteDistributionListEntity.RowKey);
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in RemoveFavoriteDistributionListFromStorageAsync: UserObjectId: {favoriteDistributionListEntity.UserObjectId}.");
                throw;
            }
        }

        /// <summary>
        /// Get an entity by the keys in the table storage.
        /// </summary>
        /// <param name="favoriteDistributionListDataId">Distribution list Id to be deleted.</param>
        /// <param name="userObjectId">User's Azure Active Directory Id.</param>
        /// <returns>The entity matching the keys.</returns>
        public async Task<FavoriteDistributionListTableEntity> GetFavoriteDistributionListFromStorageAsync(string favoriteDistributionListDataId, string userObjectId)
        {
            try
            {
                await this.EnsureInitializedAsync();
                FavoriteDistributionListTableEntity queryResult = await this.DLTableClient.GetEntityAsync<FavoriteDistributionListTableEntity>(userObjectId, favoriteDistributionListDataId);
                return queryResult;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in GetFavoriteDistributionListFromStorageAsync favoriteDistributionListDataId: {favoriteDistributionListDataId}.");
                throw;
            }
        }
    }
}
