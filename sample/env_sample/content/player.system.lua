local x = system_t.__new()
x.Name = "{Owner.UserName}_HOMEBASE"
x.OsName = "EncomOS"
x.ConnectCommandLine = 'echo "Welcome to Encom OS 12."'
x.ProxyClocks = 24.0
x.RequiredExploits = 10
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
  "prog:/bin/probe core:probe",
  "prog:/bin/scp core:scp",
  "prog:/bin/edit core:edit",
  "prog:/bin/porthack core:porthack",
  "prog:/bin/sshdk core:hack ssh 5.0",
  "prog:/bin/ftpftw core:hack ftp 7.0",
  "prog:/bin/solve core:solve",
  "prog:/bin/analyze core:analyze",
  "prog:/bin/login core:login",
  "prog:/bin/overload core:overload",
  "prog:/bin/trap core:trap",
  "prog:/bin/forkbomb core:forkbomb",
  "prog:/bin/chatd core:chatd",
  "prog:/bin/chat core:chat",
  "prog:/bin/c core:c",
  "prog:/bin/missions missions.program.script.lua",
  "text:\"/home/{UserName}/readme.txt\" \"nothing to report, {Name}.\""
})
return x