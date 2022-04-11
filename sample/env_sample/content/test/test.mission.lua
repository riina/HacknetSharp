local x = mission_t.__new()
x.Campaign = "Introduction"
x.Title = "Getting started..."
x.Message =
[[All you have to do is create a "makeme.txt" file
at the root of your system.

Good luck, have fun.]]
x.Start = 'Log("Starting mission.")'
x.AddGoal('FileExists(Home(me), "makeme.txt")')
local o = x.CreateOutcome()
o.AddGoal(0)
o.Next = 'Log("Mission complete.")'
return x