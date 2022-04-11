local x = world_t.__new()
x.Label = "Liyue kinda sux"
x.PlayerSystemTemplate = "player.system.lua"
x.StartingMission = "test/test.mission.lua"
x.StartupCommandLine = "echo \"Registering with system...\""
x.PlayerAddressRange = "66.34.0.0/16"
x.RebootDuration = 30.0
local p = x.CreatePersonGroup()
p.Template = "generic.person.lua"
p.Count = 3
local p = x.CreatePersonGroup()
p.Template = "test/alex.person.lua"
return x