﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Migrations;

namespace Squidex.Config.Startup;

public sealed class MigrationRebuilderHost(RebuildRunner rebuildRunner) : IHostedService
{
    public Task StartAsync(
        CancellationToken cancellationToken)
    {
        return rebuildRunner.RunAsync(cancellationToken);
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
