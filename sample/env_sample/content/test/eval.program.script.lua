local res = world.ScriptManager.EvaluateScript("return " .. args, true)
if res then Write(world.ScriptManager.GetString(res) .. "\n") end