Write("Are you sure you want to reset this system?\n")
if Confirm()
then
    Write("Resetting\n")
    ResetSystem(system)
end