systems = SystemGT(nil,nil)
persons = PersonGT(nil,nil)
Write('Systems:\n')
for i=1,#systems do
    Write(systems[i].ToString() .. '\n')
end
Write('Persons:\n')
for i=1,#persons do
    Write(persons[i].ToString() .. '\n')
end