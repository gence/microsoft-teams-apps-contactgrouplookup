// <copyright file="BaseController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Apps.DLLookup.Controllers
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Base controller to handle token generation.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController"/> class.
        /// </summary>
        /// <param name="logger">Instance to send logs to the Application Insights service.</param>
        public BaseController(
            ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Gets user's Azure AD object id.
        /// </summary>
        public string UserObjectId
        {
            get
            {
                var oidClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
                var claim = this.User.Claims.First(p => oidClaimType.Equals(p.Type, StringComparison.Ordinal));
                return claim.Value;
            }
        }
    }
}
