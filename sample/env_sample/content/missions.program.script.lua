local missions = Missions(me)
for i = 1,#missions do
    local data = missions[i].Data
    Write('«« ' .. data.Campaign .. ' | ' .. data.Title .. ' »»\n\n' .. data.Message .. '\n\n')
end