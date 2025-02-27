﻿// <copyright file="InMemoryPersistenceContextProvider.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.InMemory;

using System.Threading;

/// <summary>
/// A context provider which uses in-memory repositories to hold its data, e.g. for testing or demo purposes.
/// Changes in one context directly have effect in other contexts! Calling SaveChanges or not doesn't matter.
/// </summary>
public class InMemoryPersistenceContextProvider : IMigratableDatabaseContextProvider
{
    private InMemoryRepositoryManager _repositoryManager = new ();

    /// <inheritdoc/>
    public IContext CreateNewContext()
    {
        return new InMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc/>
    public IContext CreateNewContext(MUnique.OpenMU.DataModel.Configuration.GameConfiguration gameConfiguration)
    {
        return new InMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc/>
    public IContext CreateNewTradeContext()
    {
        return new InMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc/>
    public IPlayerContext CreateNewPlayerContext(MUnique.OpenMU.DataModel.Configuration.GameConfiguration gameConfiguration)
    {
        return new PlayerInMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc/>
    public IConfigurationContext CreateNewConfigurationContext()
    {
        return new ConfigurationInMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc />
    public IFriendServerContext CreateNewFriendServerContext()
    {
        return new FriendServerInMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc/>
    public IGuildServerContext CreateNewGuildContext()
    {
        return new GuildServerInMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc />
    public IContext CreateNewTypedContext<T>()
    {
        return new InMemoryContext(this._repositoryManager);
    }

    /// <inheritdoc />
    public Task<bool> DatabaseExistsAsync()
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<bool> IsDatabaseUpToDateAsync()
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task ApplyAllPendingUpdatesAsync()
    {
        // we don't need to do anything.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task WaitForUpdatedDatabaseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> CanConnectToDatabaseAsync()
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task ReCreateDatabaseAsync()
    {
        this._repositoryManager = new();
        return Task.CompletedTask;
    }
}