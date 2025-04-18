﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Domain.Apps.Entities.TestHelpers;
using Squidex.Events;

namespace Squidex.Domain.Apps.Entities.Apps;

public class AppEventDeleterTests : GivenContext
{
    private readonly IEventStore eventStore = A.Fake<IEventStore>();
    private readonly AppEventDeleter sut;

    public AppEventDeleterTests()
    {
        sut = new AppEventDeleter(eventStore);
    }

    [Fact]
    public void Should_run_last()
    {
        var order = sut.Order;

        Assert.Equal(int.MaxValue, order);
    }

    [Fact]
    public async Task Should_remove_events_from_streams()
    {
        await sut.DeleteAppAsync(App, CancellationToken);

        A.CallTo(() => eventStore.DeleteAsync(
                A<StreamFilter>.That.Matches(x => x.Prefixes!.Contains($"%-{AppId.Id}")),
                CancellationToken))
            .MustHaveHappened();
    }
}
