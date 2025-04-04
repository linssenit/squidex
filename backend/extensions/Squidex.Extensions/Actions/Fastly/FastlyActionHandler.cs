﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Infrastructure;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Extensions.Actions.Fastly;

public sealed class FastlyActionHandler(RuleEventFormatter formatter, IHttpClientFactory httpClientFactory) : RuleActionHandler<FastlyAction, FastlyJob>(formatter)
{
    private const string Description = "Purge key in fastly";

    protected override (string Description, FastlyJob Data) CreateJob(EnrichedEvent @event, FastlyAction action)
    {
        var id = string.Empty;

        if (@event is IEnrichedEntityEvent entityEvent)
        {
            id = DomainId.Combine(@event.AppId.Id, entityEvent.Id).ToString();
        }

        var ruleJob = new FastlyJob
        {
            Key = id,
            FastlyApiKey = action.ApiKey,
            FastlyServiceID = action.ServiceId,
        };

        return (Description, ruleJob);
    }

    protected override async Task<Result> ExecuteJobAsync(FastlyJob job,
        CancellationToken ct = default)
    {
        var httpClient = httpClientFactory.CreateClient("FastlyAction");

        var requestUrl = $"/service/{job.FastlyServiceID}/purge/{job.Key}";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);

        request.Headers.Add("Fastly-Key", job.FastlyApiKey);

        return await httpClient.OneWayRequestAsync(request, ct: ct);
    }
}

public sealed class FastlyJob
{
    public string FastlyApiKey { get; set; }

    public string FastlyServiceID { get; set; }

    public string Key { get; set; }
}
