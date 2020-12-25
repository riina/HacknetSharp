local res = world.ScriptManager.EvaluateExpression(args, true)
if res then Write(world.ScriptManager.GetString(res) .. "\n") end