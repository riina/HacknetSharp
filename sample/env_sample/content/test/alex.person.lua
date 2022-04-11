local x = person_t.__new()
x.Username = "alex"
x.Password = "sabina"
x.AddressRange = "69.69.69.0/24"
x.EmailProvider = "gmail.com"
x.PrimaryTemplate = "generic.system.lua"
x.PrimaryAddress = "0.0.0.1"

local n = x.CreateNetworkEntry()
n.Template = "test/alex.system.lua"
n.Address = "0.0.0.69"
n.AddLink("0.0.0.68")
n.AddLink("0.0.0.70")

local n = x.CreateNetworkEntry()
n.Template = "generic.system.lua"
n.Address = "0.0.0.68"
n.AddLink("0.0.0.69")
n.AddLink("0.0.0.70")

local n = x.CreateNetworkEntry()
n.Template = "generic.system.lua"
n.Address = "0.0.0.70"
n.AddLink("0.0.0.68")
n.AddLink("0.0.0.69")

return x