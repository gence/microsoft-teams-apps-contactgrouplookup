// <copyright file="DistributionList.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Models
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// DistributionList model is for distribution lists data from AAD and table storage.
    /// </summary>
    public class DistributionList
    {
        /// <summary>
        /// Gets or sets the Id from AAD for a particular distribution list.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name from AAD for a particular distribution list.
        /// </summary>
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the mail from AAD for a particular distribution list.
        /// </summary>
        [JsonPropertyName("mail")]
        public string Mail { get; set; }

        /// <summary>
        /// Gets or sets the mail nickname from AAD for a particular distribution list.
        /// </summary>
        [JsonPropertyName("mailNickname")]
        public string MailNickname { get; set; }

        /// <summary>
        /// Gets or sets the mail enabled from AAD for a particular distribution list.
        /// </summary>
        [JsonPropertyName("mailEnabled")]
        public bool? MailEnabled { get; set; }

        /// <summary>
        /// Gets or sets the number of members in a particular distribution list.
        /// </summary>
        [JsonPropertyName("noOfMembers")]
        public int MembersCount { get; set; }
    }
}
