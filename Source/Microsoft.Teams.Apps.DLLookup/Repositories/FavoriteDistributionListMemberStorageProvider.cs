// <copyright file="FavoriteDistributionListMemberStorageProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    using System;

    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Teams.Apps.DLLookup.Models;

    /// <summary>
    /// The class contains read, write and delete operations for distribution list member on table storage.
    /// </summary>
    public class FavoriteDistributionListMemberStorageProvider : BaseStorageProvider
    {
        private const string FavoriteMembersTableName = "FavoriteDistributionListMembers";

        /// <summary>
        /// Instance to send logs to the Application Insights service.
        /// </summary>
        private readonly ILogger<FavoriteDistributionListStorageProvider> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="FavoriteDistributionListMemberStorageProvider"/> class.
        /// </summary>
        /// <param name="storageOptions">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Sends logs to the Application Insights service.</param>
        public FavoriteDistributionListMemberStorageProvider(
            IOptionsMonitor<StorageOptions> storageOptions,
            ILogger<FavoriteDistributionListStorageProvider> logger)
            : base(storageOptions, FavoriteMembersTableName)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Adds favorite distribution list member data to table storage.
        /// </summary>
        /// <param name="favoriteDistributionListMemberDataEntity">Favorite distribution list member data to be added to storage.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task AddFavoriteMemberToStorageAsync(FavoriteDistributionListMemberTableEntity favoriteDistributionListMemberDataEntity)
        {
            try
            {
                await this.EnsureInitializedAsync();
                await this.DLTableClient.UpsertEntityAsync<FavoriteDistributionListMemberTableEntity>(favoriteDistributionListMemberDataEntity);
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in AddFavoriteMemberToStorageAsync: DistributionListMemberId: {favoriteDistributionListMemberDataEntity.UserObjectId}.");
                throw;
            }
        }

        /// <summary>
        /// Gets favorite distribution list members from table storage.
        /// </summary>
        /// <param name="userObjectId">User's Azure Active Directory Id.</param>
        /// <returns>List of pinned members.</returns>
        public async Task<IEnumerable<FavoriteDistributionListMemberTableEntity>> GetFavoriteMembersFromStorageAsync(string userObjectId)
        {
            try
            {
                await this.EnsureInitializedAsync();
                var queryResults = this.DLTableClient.QueryAsync<FavoriteDistributionListMemberTableEntity>(filter: TableClient.CreateQueryFilter($"PartitionKey eq {userObjectId}"));
                List<FavoriteDistributionListMemberTableEntity> result = new List<FavoriteDistributionListMemberTableEntity>();

                await foreach (var p in queryResults.AsPages())
                {
                    result.AddRange(p.Values);
                }

                return result;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in GetFavoriteMembersFromStorageAsync.");
                throw;
            }
        }

        /// <summary>
        /// Removes Distribution List member from table storage.
        /// </summary>
        /// <param name="favoriteDistributionListMemberTableEntity">Favorite distribution list member data to be deleted from storage.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task DeleteFavoriteMemberFromStorageAsync(FavoriteDistributionListMemberTableEntity favoriteDistributionListMemberTableEntity)
        {
            try
            {
                await this.EnsureInitializedAsync();
                await this.DLTableClient.DeleteEntityAsync(favoriteDistributionListMemberTableEntity.PartitionKey, favoriteDistributionListMemberTableEntity.RowKey);
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in DeleteFavoriteMemberFromStorageAsync: UserObjectId: {favoriteDistributionListMemberTableEntity.UserObjectId}.");
                throw;
            }
        }

        /// <summary>
        /// Gets a favorite distribution list member from table storage.
        /// </summary>
        /// <param name="pinnedDistributionListId">Pinned member id and distribution id as row key.</param>
        /// <param name="userObjectId">User's Azure Active Directory Id.</param>
        /// <returns>Favorite distribution list member record.</returns>
        public async Task<FavoriteDistributionListMemberTableEntity> GetFavoriteMemberFromStorageAsync(string pinnedDistributionListId, string userObjectId)
        {
            try
            {
                await this.EnsureInitializedAsync();
                FavoriteDistributionListMemberTableEntity queryResult = await this.DLTableClient.GetEntityAsync<FavoriteDistributionListMemberTableEntity>(userObjectId, pinnedDistributionListId);
                return queryResult;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"An error occurred in GetFavoriteDistributionListFromStorageAsync: userObjectId: {userObjectId}.");
                throw;
            }
        }
    }
}
