local x = system_t.__new()
x.Name = "{Owner.UserName}_HOMEBASE ALEXXX"
x.OsName = "EncomOS"
x.ConnectCommandLine = 'echo "I am alex and I am super spy!!!"'
x.RequiredExploits = 2

local v = x.CreateVulnerability()
v.EntryPoint = 21
v.Protocol = "ftp"
v.Exploits = 1
v.Cve = "CVE-2020-5196"

local v = x.CreateVulnerability()
v.EntryPoint = 22
v.Protocol = "ss"
v.Exploits = 1
v.Cve = "CVE-2020-3200"

local c = x.CreateCron()
c.Content = 'Log("Every 10 seconds")'
c.Delay = 10
--x.FirewallIterations = 9
x.ProxyClocks = 24.0
x.FirewallLength = 14
x.FirewallDelay = 0.05
x.FixedFirewall = "pleasure"
x.AddFiles("{Owner.UserName}",
{
  "fold*+*:/bin",
  "fold:/etc",
  "fold:/home",
  "fold*+*:/lib",
  "fold:/mnt",
  "fold+++:/root",
  "fold:/usr",
  "fold:/usr/bin",
  "fold:/usr/lib",
  "fold:/usr/local",
  "fold:/usr/share",
  "fold:/var",
  "fold:/var/spool",
  "prog:/bin/cat core:cat",
  "prog:/bin/cd core:cd",
  "prog:/bin/ls core:ls",
  "prog:/bin/scan core:scan",
  "prog:/bin/map core:map",
  "prog:/bin/cp core:cp",
  "prog:/bin/mv core:mv",
  "prog:/bin/rm core:rm",
  "prog:/bin/mkdir core:mkdir",
  "prog:/bin/scp core:scp",
  "prog:/bin/edit core:edit",
  "prog:/bin/hibiki test/hibiki.program.script.lua",
  "prog:/bin/reset test/reset.program.script.lua",
  "prog:/bin/args test/args.program.script.lua",
  "prog:/bin/eval test/eval.program.script.lua",
  "prog:/bin/exec test/exec.program.script.lua",
  "prog:/bin/lua_ls test/lua_ls.program.script.lua",
  "prog:/bin/debug_show test/debug_show.program.script.lua",
  'text:/home/{UserName}/readme.txt "my name is alex and oh no anyway"'
})
return x