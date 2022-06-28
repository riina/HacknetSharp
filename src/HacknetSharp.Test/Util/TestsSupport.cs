using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using NUnit.Framework;
using static HacknetSharp.Test.Util.SynchronousTestServerTemplateConfiguration;

namespace HacknetSharp.Test.Util;

internal static class TestsSupport
{
    internal static SynchronousTestServer ConfigureSimpleEmptySystem(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateEmptySystemTemplate("jacobian");
        return ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
    }

    internal static SynchronousTestServer ConfigureSimpleNormalSystem(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateNormalSystemTemplate("jacobian");
        return ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
    }

    private static SynchronousTestServer ConfigureSimpleInternal(WorldModel worldModel, TemplateGroup templateGroup,
        SystemTemplate systemTemplate, string systemTemplateName, out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var server = CreateServer(CreateConfig(worldModel, templateGroup));
        server.Start();
        world = server.DefaultWorld;
        var password = ServerUtil.HashPassword("rosebud");
        user = server.Spawn.User("user1", password, false);
        person = world.Spawn.Person("person1", "person1username", user: user);
        Assert.That(person.Systems.Count, Is.EqualTo(0));
        system = world.Spawn.System(systemTemplate, systemTemplateName, person, user.Password, new IPAddressRange("192.168.0.32"));
        Assert.That(person.Systems.Count, Is.EqualTo(1));
        ctx = new SynchronousTestServerPersonContext(person) { Connected = true };
        return server;
    }

    internal static void AssertDisconnect(SynchronousTestServer server, SynchronousTestServerPersonContext ctx)
    {
        ctx.Connected = false;
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo("[Shell terminated]\n"));
    }
}
