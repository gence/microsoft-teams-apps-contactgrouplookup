// <copyright file="GlobalSuppressions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1200:Using directives should be placed correctly", Justification = "Not applicable in Top-Level Statements")]
[assembly: SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Warning encountered in Program.cs")]
[assembly: SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "No security effect.", Scope = "module")]
[assembly: SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Not applicable in Top-Level Statements", Scope = "module")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:ElementsMustBeSeparatedByBlankLine", Justification = "Reviewed.")]
