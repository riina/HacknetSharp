local res = EvaluateScript("return " .. args, true)
if res then Write(ToString(res) .. "\n") end