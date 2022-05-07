// <copyright file="GraphUtilityHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Graph;
    using Microsoft.Teams.Apps.DLLookup.Models;

    /// <summary>
    /// This class will contain Graph SDK read and write operations.
    /// </summary>
    public static class GraphUtilityHelper
    {
        /// <summary>
        /// Gets distribution list (Azure AD groups) using MS Graph based on search query.
        /// </summary>
        /// <param name="query">Search query to filter distribution list based on Azure AD group display name.</param>
        /// <param name="graphClient">Instance of Microsoft Graph client.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <returns>A collection of distribution list based on search query.</returns>
        public static async Task<IEnumerable<DistributionList>> GetDistributionListsAsync(string query, GraphServiceClient graphClient, ILogger logger)
        {
            try
            {
                var response = await graphClient
                .Groups
                .Request()
                .Filter($"startswith(displayName, '{Uri.EscapeDataString(query)}')")
                .GetAsync();

                var distributionList = response.
                   Select(e => new DistributionList()
                   {
                       Id = e.Id,
                       DisplayName = e.DisplayName,
                       Mail = e.Mail,
                       MailEnabled = e.MailEnabled,
                       MailNickname = e.MailNickname,
                   });

                return distributionList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred in GetDistributionListsAsync: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Gets distribution list members data from MS Graph.
        /// </summary>
        /// <param name="groupId">Distribution list id of Azure AD group.</param>
        /// <param name="graphClient">Instance of Microsoft Graph client.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <returns>A collection of distribution list member data containing nested groups and group members.</returns>
        public static async Task<List<DistributionListMember>> GetDistributionListMembersAsync(string groupId, GraphServiceClient graphClient, ILogger logger)
        {
            try
            {
                var response = await graphClient.Groups[groupId].Members
                .Request()
                .Top(100)
                .GetAsync();

                var memberList = response
                    .OfType<Microsoft.Graph.User>()
                    .Select(e => new DistributionListMember()
                    {
                        UserObjectId = e.Id,
                        DisplayName = e.DisplayName,
                        ODataType = e.ODataType,
                        UserType = "Member",
                        Mail = e.Mail,
                        UserPrincipalName = e.UserPrincipalName,
                        JobTitle = e.JobTitle,
                    }).ToList();

                var distributionListList = response
                    .OfType<Microsoft.Graph.Group>()
                    .Select(e => new DistributionListMember()
                    {
                        UserObjectId = e.Id,
                        DisplayName = e.DisplayName,
                        ODataType = e.ODataType,
                        Mail = e.Mail,
                    }).ToList();

                memberList.AddRange(distributionListList);
                return memberList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred in GetDistributionListMembersAsync: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Get User presence details from MS Graph.
        /// </summary>
        /// <param name="presenceBatch">List of people presence data in batch.</param>
        /// <param name="graphClient">Instance of Microsoft Graph client.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <returns>A collection of people presence data model with user presence information.</returns>
        public static async Task<List<PeoplePresenceData>> GetUserPresenceAsync(List<PeoplePresenceData> presenceBatch, GraphServiceClient graphClient, ILogger logger)
        {
            try
            {
                List<string> batchIds = new List<string>();
                List<PeoplePresenceData> peoplePresenceResults = new List<PeoplePresenceData>();

                var batchRequestContent = new BatchRequestContent();
                var userIds = presenceBatch.Select(user => user.Id);

                foreach (string userId in userIds)
                {
                    var request = graphClient
                        .Users[userId]
                        .Presence
                        .Request();

                    batchIds.Add(batchRequestContent.AddBatchRequestStep(request));
                }

                var returnedResponse = await graphClient.Batch.Request().PostAsync(batchRequestContent);
                for (int i = 0; i < batchIds.Count; i++)
                {
                    peoplePresenceResults.Add(await returnedResponse.GetResponseByIdAsync<PeoplePresenceData>(batchIds[i]));
                    peoplePresenceResults[i].UserPrincipalName = presenceBatch.FirstOrDefault(user => user.Id == peoplePresenceResults[i].Id).UserPrincipalName;
                    peoplePresenceResults[i].Id = presenceBatch.FirstOrDefault(user => user.Id == peoplePresenceResults[i].Id).Id;
                    peoplePresenceResults[i].Availability = peoplePresenceResults[i].Availability;
                }

                return peoplePresenceResults;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred in GetUserPresenceAsync: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Get distribution list details and members count per distribution list from MS Graph.
        /// </summary>
        /// <param name="groupBatch">List of distribution list id (group id).</param>
        /// <param name="graphClient">Instance of Microsoft Graph client.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <returns>A collection of distribution list with number of members in each list.</returns>
        public static async Task<List<DistributionList>> GetDistributionListDetailsAsync(List<string> groupBatch, GraphServiceClient graphClient, ILogger logger)
        {
            try
            {
                List<DistributionList> distributionBatchList = new List<DistributionList>();
                BatchRequestContent batchRequestContent = new BatchRequestContent();
                List<string> batchIdGroups = new List<string>();

                foreach (string groupId in groupBatch)
                {
                    var request = graphClient
                        .Groups[groupId]
                        .Request();

                    batchIdGroups.Add(batchRequestContent.AddBatchRequestStep(request));
                }

                List<string> batchIdMembers = new List<string>();
                foreach (string groupId in groupBatch)
                {
                    var request = graphClient
                        .Groups[groupId]
                        .Members
                        .Request()
                        .Top(100);

                    batchIdMembers.Add(batchRequestContent.AddBatchRequestStep(request));
                }

                var returnedResponse = await graphClient.Batch.Request().PostAsync(batchRequestContent);

                for (int i = 0; i < batchIdGroups.Count; i++)
                {
                    distributionBatchList.Add(await returnedResponse.GetResponseByIdAsync<DistributionList>(batchIdGroups[i]));
                    var dlMemberData = await returnedResponse.GetResponseByIdAsync<dynamic>(batchIdMembers[i]);
                    JsonObject obj = JsonObject.Create(dlMemberData);
                    List<DistributionListMember> dlMemberList = obj["value"].AsArray().Deserialize<List<DistributionListMember>>();

                    distributionBatchList[i].MembersCount = dlMemberList
                        .Where(member => member.Type == "#microsoft.graph.user")
                        .Count();
                }

                return distributionBatchList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred in GetDistributionListDetailsAsync: {ex.Message}.");
                throw;
            }
        }

        /// <summary>
        /// Gets distribution list members using MS Graph.
        /// </summary>
        /// <param name="groupId">Distribution list id (group id) to get members list.</param>
        /// <param name="graphClient">Instance of Microsoft Graph client.</param>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        /// <returns>A collection of distribution list member data providing all group member details.</returns>
        public static async Task<IEnumerable<DistributionListMember>> GetMembersListAsync(string groupId, GraphServiceClient graphClient, ILogger logger)
        {
            try
            {
                var response = await graphClient.Groups[groupId].Members
                .Request()
                .Top(100)
                .GetAsync();

                var memberList = response
                    .OfType<Microsoft.Graph.User>()
                    .Select(e => new DistributionListMember()
                    {
                        UserObjectId = e.Id,
                        DisplayName = e.DisplayName,
                        ODataType = e.ODataType,
                        UserType = "Member",
                        Mail = e.Mail,
                        UserPrincipalName = e.UserPrincipalName,
                        JobTitle = e.JobTitle,
                    });

                return memberList;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"An error occurred in GetMembersListAsync: {ex.Message}.");
                throw;
            }
        }
    }
}
