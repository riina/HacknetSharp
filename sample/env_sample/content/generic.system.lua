local x = system_t.__new()
x.Name = "{Owner.UserName}_HOMEBASE KEKW"
x.OsName = "EncomOS"
x.AddUser("daphne+", "andthenonedayIgotin")
x.AddUser("seiteki", "grovel")
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
  "prog:/bin/scp core:scp",
  "prog:/bin/edit core:edit",
  'text:"/home/{UserName}/readme.txt" "Are you ready to comply?"',
  "file:/home/{UserName}/image.png misc/image.png"
})
return x