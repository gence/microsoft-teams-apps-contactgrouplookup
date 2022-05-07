// <copyright file="FavoriteDistributionListDataRepository.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.DLLookup.Helpers;
    using Microsoft.Teams.Apps.DLLookup.Helpers.Extentions;
    using Microsoft.Teams.Apps.DLLookup.Models;
    using Microsoft.Teams.Apps.DLLookup.Repositories.Interfaces;

    /// <summary>
    /// This class contains read, write and update operations for distribution list member data on AAD and table storage.
    /// </summary>
    public class FavoriteDistributionListDataRepository : FavoriteDistributionListStorageProvider, IFavoriteDistributionListDataRepository
    {
        /// <summary>
        /// MS Graph batch limit is 20. Setting it 10 here as 2 APIs are added in batch.
        /// https://docs.microsoft.com/en-us/graph/known-issues#json-batching.
        /// </summary>
        private const int BatchSplitCount = 10;
        private readonly ILogger<FavoriteDistributionListDataRepository> logger;
        private GraphServiceClient graphClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FavoriteDistributionListDataRepository"/> class.
        /// </summary>
        /// <param name="storageOptions">A set of key/value application configuration properties for Microsoft Azure Table storage.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <param name="graphServiceClient">Instance of client to access Microsoft Graph.</param>
        public FavoriteDistributionListDataRepository(
            IOptionsMonitor<StorageOptions> storageOptions,
            ILogger<FavoriteDistributionListDataRepository> logger,
            GraphServiceClient graphServiceClient)
            : base(storageOptions, logger)
        {
            this.logger = logger;
            this.graphClient = graphServiceClient;
        }

        /// <summary>
        /// Creates/Updates favorite distribution list data in table storage.
        /// </summary>
        /// <param name="favoriteDistributionList">Instance of favoriteDistributionListData.</param>
        /// <returns>Returns data model.</returns>
        public async Task CreateOrUpdateFavoriteDistributionListAsync(
           FavoriteDistributionListData favoriteDistributionList)
        {
            FavoriteDistributionListTableEntity favoriteDistributionListDataEntity = new FavoriteDistributionListTableEntity()
            {
                GroupId = favoriteDistributionList.Id,
                PinStatus = favoriteDistributionList.IsPinned,
                UserObjectId = favoriteDistributionList.UserObjectId,
            };

            await this.AddFavoriteDistributionListToStorageAsync(favoriteDistributionListDataEntity);
        }

        /// <summary>
        /// Gets distribution list data from MS Graph based on search query.
        /// </summary>
        /// <param name="query">Search query used to filter distribution list.</param>
        /// <returns>Distribution lists filtered with search query.</returns>
        public async Task<List<DistributionList>> GetDistributionListsAsync(
            string query)
        {
            var distributionLists = await GraphUtilityHelper.GetDistributionListsAsync(query, this.graphClient, this.logger);
            return distributionLists.ToList();
        }

        /// <summary>
        /// Get favorite distribution list details and members count from Graph.
        /// </summary>
        /// <param name="groupIds">List of Distribution List Ids.</param>
        /// <returns>Count of members in distribution list.</returns>
        public async Task<List<DistributionList>> GetDistributionListDetailsFromGraphAsync(List<string> groupIds)
        {
            // MS Graph batch limit is 20
            // refer https://docs.microsoft.com/en-us/graph/known-issues#json-batching to known issues with Microsoft Graph batch APIs
            IEnumerable<List<string>> groupBatches = groupIds.SplitList(BatchSplitCount);
            List<DistributionList> distributionListList = new List<DistributionList>();

            foreach (List<string> groupBatch in groupBatches)
            {
                try
                {
                    distributionListList.AddRange(await GraphUtilityHelper.GetDistributionListDetailsAsync(groupBatch, this.graphClient, this.logger));
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, $"An error occurred in GetDistributionListDetailsFromGraphAsync.");
                }
            }

            return distributionListList;
        }

        /// <summary>
        /// Gets favorite Distribution List details from Graph.
        /// </summary>
        /// <param name="favoriteDistributionListEntities">Favorite Distribution List data from storage.</param>
        /// <returns>Favorite distribution list data from graph.</returns>
        public async Task<List<FavoriteDistributionListData>> GetFavoriteDistributionListsFromGraphAsync(
            IEnumerable<FavoriteDistributionListTableEntity> favoriteDistributionListEntities)
        {
            List<FavoriteDistributionListData> favoriteDistributionList = new List<FavoriteDistributionListData>();

            List<string> groupIds = favoriteDistributionListEntities.Select(dl => dl.GroupId).ToList();
            List<DistributionList> distributionList = await this.GetDistributionListDetailsFromGraphAsync(groupIds);

            foreach (FavoriteDistributionListTableEntity currentItem in favoriteDistributionListEntities)
            {
                DistributionList currentDistributionList = distributionList.Find(dl => dl.Id == currentItem.GroupId);
                if (currentDistributionList == null)
                {
                    continue;
                }

                favoriteDistributionList.Add(
                    new FavoriteDistributionListData
                    {
                        IsPinned = currentItem.PinStatus,
                        DisplayName = currentDistributionList.DisplayName,
                        Mail = currentDistributionList.Mail,
                        ContactsCount = currentDistributionList.MembersCount,
                        Id = currentItem.GroupId,
                        UserObjectId = currentItem.UserObjectId,
                    });
            }

            return favoriteDistributionList;
        }
    }
}
