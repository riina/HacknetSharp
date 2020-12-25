local dir
if argc > 1 then dir = argv[2] else dir = pwd end
local res = Folder(system, dir)
if not res
then
    local f = File(system, dir)
    if f
    then
        -- Checks if there's a file. If we got nil from Folder then it's a file here.
        Write(f.FullPath)
    else
        Write('No such file or directory\n')
    end
else
    for i = 1,#res do
        Write(res[i].FullPath .. "\n")
    end
end