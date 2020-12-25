local res = world.ScriptManager.EvaluateScript(args, true)
if res then Write(world.ScriptManager.GetString(res) .. "\n") end