using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using NUnit.Framework;

namespace HacknetSharp.Test.Util;

public class ProgramTestBase
{
#nullable disable
    internal Setup _setup;
    internal SynchronousTestServer _server;
    internal World _world;
    internal UserModel _user;
    internal PersonModel _person;
    internal SystemModel _system;
    internal SynchronousTestServerPersonContext _ctx;
    internal ShellProcess _shell;
#nullable restore

    internal ProgramTestBase UnixFs()
    {
        _setup = _setup with { Populated = true };
        return this;
    }

    internal ProgramTestBase EmptyFs()
    {
        _setup = _setup with { Populated = false };
        return this;
    }

    internal ProgramTestBase Admin()
    {
        _setup = _setup with { Admin = true };
        return this;
    }

    internal ProgramTestBase Unelevated()
    {
        _setup = _setup with { Admin = false };
        return this;
    }

    internal ProgramTestBase Identity(string user)
    {
        _setup = _setup with { Identity = user };
        return this;
    }

    internal ProgramTestBase Name(string personName)
    {
        _setup = _setup with { Name = personName };
        return this;
    }

    internal ProgramTestBase UserName(string personUserName)
    {
        _setup = _setup with { UserName = personUserName };
        return this;
    }

    internal ProgramTestBase Password(string password)
    {
        _setup = _setup with { Password = password };
        return this;
    }

    internal ProgramTestBase Files(params string[] files)
    {
        _setup = _setup with { AdditionalFiles = files };
        return this;
    }

    internal ProgramTestBase Configure(Setup options)
    {
        _server = TestsSupport.Configure(options, out _world, out _user, out _person, out _system, out _ctx);
        return this;
    }

    internal ProgramTestBase Build()
    {
        _server = TestsSupport.Configure(_setup, out _world, out _user, out _person, out _system, out _ctx);
        return this;
    }

    internal ProgramTestBase Shell()
    {
        _shell = TestsSupport.StartShell(_world, _ctx, _person, _system);
        return this;
    }

    internal ProgramTestBase Run(string command, int consoleWidth = 32)
    {
        TestsSupport.QueueAndUpdate(_server, _ctx, _user, command, consoleWidth);
        return this;
    }

    internal ProgramTestBase Queue(string command, int consoleWidth = 32)
    {
        TestsSupport.Queue(_server, _ctx, _user, command, consoleWidth);
        return this;
    }

    internal ProgramTestBase Update(float deltaTime)
    {
        TestsSupport.Update(_server, deltaTime);
        return this;
    }

    internal ProgramTestBase AssertDisconnect()
    {
        TestsSupport.AssertDisconnect(_server, _ctx);
        return this;
    }

    internal string NextText()
    {
        return _ctx.NextText();
    }

    internal int ProcessCount()
    {
        return _system.Processes.Count(v => v.Value != _shell);
    }

    [SetUp]
    public void SetUp()
    {
        _setup = new Setup();
    }

    [TearDown]
    public void TearDown()
    {
#nullable disable
        _server?.Dispose();
        _server = null;
        _world = null;
        _user = null;
        _person = null;
        _system = null;
        _ctx = null;
        _shell = null;
#nullable restore
    }
}
