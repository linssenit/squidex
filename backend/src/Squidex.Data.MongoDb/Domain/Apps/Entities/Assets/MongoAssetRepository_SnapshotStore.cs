﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Domain.Apps.Core.Assets;
using Squidex.Domain.Apps.Entities.Assets;
using Squidex.Infrastructure;
using Squidex.Infrastructure.States;

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Domain.Apps.Entities.MongoDb.Assets;

public sealed partial class MongoAssetRepository : ISnapshotStore<Asset>, IDeleter
{
    Task IDeleter.DeleteAppAsync(App app,
        CancellationToken ct)
    {
        return Collection.DeleteManyAsync(Filter.Eq(x => x.IndexedAppId, app.Id), ct);
    }

    IAsyncEnumerable<SnapshotResult<Asset>> ISnapshotStore<Asset>.ReadAllAsync(
        CancellationToken ct)
    {
        var documents = Collection.Find(FindAll, Batching.Options).ToAsyncEnumerable(ct);

        return documents.Select(x => new SnapshotResult<Asset>(x.DocumentId, x, x.Version));
    }

    async Task<SnapshotResult<Asset>> ISnapshotStore<Asset>.ReadAsync(DomainId key,
        CancellationToken ct)
    {
        using (Telemetry.Activities.StartActivity("MongoAssetRepository/ReadAsync"))
        {
            var existing =
                await Collection.Find(x => x.DocumentId == key)
                    .FirstOrDefaultAsync(ct);

            if (existing != null)
            {
                return new SnapshotResult<Asset>(existing.DocumentId, existing, existing.Version);
            }

            return new SnapshotResult<Asset>(default, null!, EtagVersion.Empty);
        }
    }

    async Task ISnapshotStore<Asset>.WriteAsync(SnapshotWriteJob<Asset> job,
        CancellationToken ct)
    {
        using (Telemetry.Activities.StartActivity("MongoAssetRepository/WriteAsync"))
        {
            var entityJob = job.As(MongoAssetEntity.Create(job));

            await Collection.UpsertVersionedAsync(entityJob, Field.Of<Asset>(x => nameof(x.Version)), ct);
        }
    }

    async Task ISnapshotStore<Asset>.WriteManyAsync(IEnumerable<SnapshotWriteJob<Asset>> jobs,
        CancellationToken ct)
    {
        using (Telemetry.Activities.StartActivity("MongoAssetRepository/WriteManyAsync"))
        {
            var updates = jobs.Select(MongoAssetEntity.Create).Select(x =>
                new ReplaceOneModel<MongoAssetEntity>(
                    Filter.Eq(y => y.DocumentId, x.DocumentId),
                    x)
                {
                    IsUpsert = true,
                }).ToList();

            if (updates.Count == 0)
            {
                return;
            }

            await Collection.BulkWriteAsync(updates, BulkUnordered, ct);
        }
    }

    async Task ISnapshotStore<Asset>.RemoveAsync(DomainId key,
        CancellationToken ct)
    {
        using (Telemetry.Activities.StartActivity("MongoAssetRepository/RemoveAsync"))
        {
            await Collection.DeleteOneAsync(x => x.DocumentId == key, null, ct);
        }
    }
}
