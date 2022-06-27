using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using NUnit.Framework;

namespace HacknetSharp.Test;

public class ServerTests
{
    [Test]
    public void SynchronousServer_Empty_Boots()
    {
        using var server = CreateServer();
        server.Start();
    }

    [Test]
    public void SynchronousServer_Empty_SingleUpdate_Works()
    {
        using var server = CreateServer();
        server.Start();
        server.Update(1.0f);
    }

    private SynchronousTestServer CreateServer()
    {
        var worldModel = WorldModel.CreateEmpty("A world", "Wait what", "player_system_template");
        var templates = new TemplateGroup { SystemTemplates = { ["player_system_template"] = new SystemTemplate() } };
        var cfg = new SynchronousTestServerConfig()
            .WithDatabase(new SynchronousTestDatabase())
            .WithMainWorld(worldModel)
            .WithTemplates(templates);
        return new SynchronousTestServer(cfg);
    }
}

internal class SynchronousTestServerConfig : ServerConfigBase
{
    public SynchronousTestDatabase? Database { get; set; }
    public WorldModel? MainWorld { get; set; }

    public SynchronousTestServerConfig WithDatabase(SynchronousTestDatabase database)
    {
        Database = database;
        return this;
    }

    public SynchronousTestServerConfig WithMainWorld(WorldModel defaultWorld)
    {
        MainWorld = defaultWorld;
        return this;
    }

    public new SynchronousTestServerConfig WithTemplates(TemplateGroup templateGroup)
    {
        base.WithTemplates(templateGroup);
        return this;
    }
}

internal class SynchronousTestServer : ServerBaseSynchronous
{
    public SynchronousTestServer(SynchronousTestServerConfig config) : base(config)
    {
        if (config.Database == null) throw new ArgumentException("Null database");
        if (config.MainWorld == null) throw new ArgumentException("Null main world");
        ConfigureDatabase(config.Database);
        ConfigureDefaultWorld(new World(this, config.MainWorld));
    }
}

internal class SynchronousTestDatabase : IServerDatabase
{
    public Task<TResult?> GetAsync<TKey, TResult>(TKey key)
        where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

    public Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
        where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

    public Task<List<TResult>> WhereAsync<TResult>(Expression<Func<TResult, bool>> predicate)
        where TResult : class => throw new NotSupportedException();

    public void Add<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void AddBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Update<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void UpdateBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Delete<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void DeleteBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Sync()
    {
    }

    public Task SyncAsync() => Task.CompletedTask;
}
