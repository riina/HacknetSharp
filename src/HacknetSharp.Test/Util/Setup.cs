using System;

namespace HacknetSharp.Test.Util;

public record Setup(
    bool Populated = true,
    bool Admin = true,
    string[]? AdditionalFiles = null,
    string SystemName = "{Owner} HomeBase",
    string SystemTemplateName = "System_Template_1",
    string Identity = "User",
    string Name = "Person",
    string UserName = "PersonUsername",
    string Password = "Password",
    ReadOnlyMemory<string> AddressPool = default);
