﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Core.Schemas;
using Squidex.Infrastructure;
using Squidex.Infrastructure.Collections;

namespace Squidex.Domain.Apps.Entities;

public static class AppProviderExtensions
{
    public static async Task<ResolvedComponents> GetComponentsAsync(this IAppProvider appProvider, Schema schema,
        CancellationToken ct = default)
    {
        Dictionary<DomainId, Schema>? result = null;

        var appId = schema.AppId.Id;

        async Task ResolveWithIdsAsync(ReadonlyList<DomainId>? schemaIds)
        {
            if (schemaIds == null)
            {
                return;
            }

            foreach (var schemaId in schemaIds)
            {
                if (schemaId == schema.Id)
                {
                    result ??= [];
                    result[schemaId] = schema;
                }
                else if (result == null || !result.ContainsKey(schemaId))
                {
                    var resolvedEntity = await appProvider.GetSchemaAsync(appId, schemaId, false, ct);

                    if (resolvedEntity != null)
                    {
                        result ??= [];
                        result[schemaId] = resolvedEntity;

                        await ResolveSchemaAsync(resolvedEntity);
                    }
                }
            }
        }

        async Task ResolveArrayAsync(IArrayField arrayField)
        {
            foreach (var nestedField in arrayField.Fields)
            {
                await ResolveFieldAsync(nestedField);
            }
        }

        async Task ResolveFieldAsync(IField field)
        {
            switch (field)
            {
                case IField<ComponentFieldProperties> component:
                    await ResolveWithIdsAsync(component.Properties.SchemaIds);
                    break;

                case IField<ComponentsFieldProperties> components:
                    await ResolveWithIdsAsync(components.Properties.SchemaIds);
                    break;

                case IArrayField arrayField:
                    await ResolveArrayAsync(arrayField);
                    break;
            }
        }

        async Task ResolveSchemaAsync(Schema schema)
        {
            foreach (var field in schema.Fields)
            {
                await ResolveFieldAsync(field);
            }
        }

        await ResolveSchemaAsync(schema);

        if (result == null)
        {
            return ResolvedComponents.Empty;
        }

        return new ResolvedComponents(result);
    }
}
