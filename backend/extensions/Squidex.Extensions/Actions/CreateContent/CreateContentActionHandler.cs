﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Contents;
using Squidex.Domain.Apps.Core.HandleRules;
using Squidex.Domain.Apps.Core.Rules.EnrichedEvents;
using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Domain.Apps.Entities;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Commands;
using Squidex.Infrastructure.Json;
using Command = Squidex.Domain.Apps.Entities.Contents.Commands.CreateContent;

namespace Squidex.Extensions.Actions.CreateContent;

public sealed class CreateContentActionHandler(RuleEventFormatter formatter, IAppProvider appProvider, ICommandBus commandBus, IJsonSerializer jsonSerializer) : RuleActionHandler<CreateContentAction, Command>(formatter)
{
    private const string Description = "Create a content";

    protected override async Task<(string Description, Command Data)> CreateJobAsync(EnrichedEvent @event, CreateContentAction action)
    {
        var ruleJob = new Command
        {
            AppId = @event.AppId,
        };

        var schema = await appProvider.GetSchemaAsync(@event.AppId.Id, action.Schema, true)
            ?? throw new InvalidOperationException($"Cannot find schema '{action.Schema}'");

        ruleJob.SchemaId = schema.NamedId();
        ruleJob.FromRule = true;

        var json = await FormatAsync(action.Data, @event);

        ruleJob.Data = jsonSerializer.Deserialize<ContentData>(json!);

        if (!string.IsNullOrEmpty(action.Client))
        {
            ruleJob.Actor = RefToken.Client(action.Client);
        }
        else if (@event is EnrichedUserEventBase userEvent)
        {
            ruleJob.Actor = userEvent.Actor;
        }

        if (action.Publish)
        {
            ruleJob.Status = Status.Published;
        }

        return (Description, ruleJob);
    }

    protected override async Task<Result> ExecuteJobAsync(Command job,
        CancellationToken ct = default)
    {
        var command = job;

        await commandBus.PublishAsync(command, ct);

        return Result.Success($"Created to: {command.SchemaId.Name}");
    }
}
