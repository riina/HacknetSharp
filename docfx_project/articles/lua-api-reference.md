# Lua API reference

The lua API provides some rudimentary logic for missions, programs/services, and
hackscripts.

Some examples are available under `sample/env_sample`.

Missions are defined as mission templates in YAML
([Template reference](template-reference.md)) with conditions and behaviour
written inline as lua code.
Missions are required to end in `.mission.yaml`.

Programs and services are lua scripts that are treated like standard managed-code
executables. Programs are required to end in `.program.script.yaml` and services
are required to end in `.service.script.yaml`.

# Fields

## me

`PersonModel me`

The current person in the context.

Only available to missions/programs.

~~Note: this is what should generally be used as the spawn group key source (`me.Key`)
when spawning systems / running hack scripts. This ensures that applicable
entity searches are properly scoped.~~

## key

`Guid key`

The current campaign key.

Only available to missions/hackscripts.

Note: this is what should generally be used as the key source when starting new missions
in a continuing campaign or when running hackscripts / spawning systems as part of a campaign.

## self

`Program|Service|MissionModel|SystemModel self`

Current context.

Programs: Program

Services: Service

Missions: MissionModel

Tasks: SystemModel

## login

`LoginModel login`

Current login.

Only available to programs/services.

## argv

`string[] argv`

Current command arguments.

Only available to programs/services.

## argc

`int argc`

Number of command arguments.

Only available to programs/services.

## args

`string args`

Arguments part of command line as a single string.

Only available to programs/services.

## shell

`ShellProcess shell`

Current shell.

Only available to programs.

## system

`SystemModel system`

Current system.

Only available to programs/services/tasks.

# Specialized Members

## Delay

`void Delay(float delay)`

Pauses coroutine and triggers a delay in seconds.

Only available to programs/services.

## Write

`void Write(string text)`

Queues text to be written to shell output.

Only available to programs.

## Flush

`void Flush()`

Triggers a request for text to be written to shell output.

Only available to programs.

## Unbind

`void Unbind()`

Returns control to the user while still continuing execution.

Only available to programs.

## QueueInput

`void QueueInput(ShellProcess? shell, string input)`

Queues an input with the specified string.

Only available to programs.

## QueueEdit

`void QueueEdit(ShellProcess? shell, Func<string, string> edit)`

Queues an edit based on the specified lua function.

Only available to programs.

## QueueFixedEdit

`void QueueFixedEdit(ShellProcess? shell, string content)`

Queues an edit that will just write the specified content.

Only available to programs.

# Standard Members

## Missions

`MissionModel[]? Missions(PersonModel? person, Guid? campaignKey)`

Gets current missions (filtered by campaign key if specified).

## StartMission

`MissionModel? StartMission(PersonModel? person, string missionPath, Guid? campaignKey)`

Attempts to start the specified mission.

If the campaign key is nil, generates a new campaign key.

## RemoveMission

`void RemoveMission(MissionModel? mission)`

Removes (ends) a mission.

## DropSpawns

`void DropSpawns(Guid? key)`

Drops all entities relevant to the specified campaign / spawn group key.

## DropCampaign

`void DropCampaign(PersonModel? person, string campaignName)`

Drops all active missions relevant to specified campaign. All campaign spawn group keys
are also used to remove related entities.

## DropCampaignK

`void DropCampaignK(PersonModel? person, Guid? key)`

Drops all active missions relevant to specified campaign (spawn group) key.

## PersonT

`PersonModel[] PersonT(string tag)`

Gets persons with the specified tag.

## PersonGT

`PersonModel[] PersonGT(Guid? key, string? tag)`

Gets persons with the specified group and tag.

## PersonGTSingle

`PersonModel? PersonGTSingle(Guid? key, string? tag)`

Gets first existing person with the specified group and tag or nil.

## SpawnPerson

`PersonModel SpawnPerson(string name, string username)`

Spawns a person with the specified proper name and username.

## SpawnPersonG

`PersonModel SpawnPersonG(string name, string username, Guid key)`

Spawns a person with the specified proper name, username, and group key.

## SpawnPersonT

`PersonModel SpawnPersonT(string name, string username, string tag)`

Spawns a person with the specified proper name, username, tag.

## SpawnPersonGT

`PersonModel SpawnPersonGT(string name, string username, Guid key, string tag)`

Spawns a person with the specified proper name, username, tag, and group key.

## EnsurePersonGT

`PersonModel EnsurePersonGT(string name, string username, Guid key, string tag)`

Ensures a person with the specified proper name, username, tag, and group key exists
or is created.

## RemovePerson

`void RemovePerson(PersonModel? person)`

Removes a person (and all systems).

## SystemT

`SystemModel[] SystemT(string tag)`

Gets systems with the specified tag.


## PersonGT

`SystemModel[] SystemGT(Guid? key, string? tag)`

Gets systems with the specified group and tag.

## PersonGTSingle

`SystemModel? SystemGTSingle(Guid? key, string? tag)`

Gets first existing system with the specified group and tag or nil.

## SystemA

`SystemModel? SystemA(string tag)`

Tries to get a system with the specified IP address.

## Home

`SystemModel? Home(PersonModel? person)`

Attempts to get the home system for a person.

## SystemUp

`bool SystemUp(SystemModel? system)`

Checks if a system is up. Returns false if system is null.

## SpawnSystem

`SystemModel? SpawnSystem(PersonModel? owner, string password, string template, string addressOrAddressRange)`

Attempts to spawn a system.

## SpawnSystemG

`SystemModel? SpawnSystemG(PersonModel? owner, string password, string template, string addressOrAddressRange, Guid key)`

Attempts to spawn a system with the specified group key.

## SpawnSystemT

`SystemModel? SpawnSystemT(PersonModel? owner, string password, string template, string addressOrAddressRange, string tag)`

Attempts to spawn a system with the specified tag.

## SpawnSystemGT

`SystemModel? SpawnSystemGT(PersonModel? owner, string password, string template, string addressOrAddressRange, Guid key, string tag)`

Attempts to spawn a system with the specified tag and group key.

## EnsureSystemGT

`SystemModel? EnsureSystemGT(PersonModel? owner, string password, string template, string addressOrAddressRange, Guid key, string tag)`

Ensures that a system with the specified tag and group key exists
or is created.
Still fails if invalid setup parameters are passed.

## RemoveSystem

`void RemoveSystem(SystemModel? system)`

Removes a system.

## Reset

`void ResetSystem(SystemModel? system)`

Resets a specified system to its template state.

## FileExists

`bool FileExists(SystemModel? system, string path)`

Checks if the filesystem on the specified system contains
a file/folder with the provided path.

## FileContains

`bool FileContains(SystemModel? system, string path, string text, bool ignoreCase)`

Checks if a file exists on the specified system and contains the specified text.

## File

`FileModel? File(SystemModel? system, string path)`

Tries to get a filesystem element at the specified path.

## Folder

`FileModel[]? Folder(SystemModel? system, string path)`

Tries to get folder contents as a table (nil if no folder or path is file).

## SpawnFile

`FileModel? SpawnFile(SystemModel? system, string path, string content)`

Spawns a text file with the specified content. Returns an existing file if applicable.

## RemoveFile

`void RemoveFile(FileModel? file)`

Removes a file (recursive).

## SpawnCron

`CronModel? SpawnCron(SystemModel? system, string content, float start, float delay, float end)`

Spawns a task with the specified content.

## RemoveCron

`void RemoveCron(CronModel? cron)`

Removes a task.

## Log

`void Log(string message)`

Writes a `LogLevel.Information` message to the world's logger.

## LogEx

`void LogEx(string message, int level)`

Writes a message to the world's logger.

* 0: LogLevel.Information
* 1: LogLevel.Warning
* 2: LogLevel.Error
* default: LogLevel.Critical

## StartShell

`(ShellProcess, LoginModel)? StartShell(PersonModel? person, SystemModel? system)`

Starts a shell for the specified person/system.

Fails if either arg is null or if person doesn't have a valid login directly
associated with them on the target system.

## KillProcess

`void KillProcess(Process? process)`

Kills a process.

## Ps

`Process[]? Ps(SystemModel? system)`

Lists all processes on system.

## PsLogin

`Process[]? PsLogin(LoginModel? login)`

Lists all processes for specified login.

## RunHackScript

`void RunHackScript(Guid key, string systemTag, string script)`

Sets up and runs the specified hack script (as a standard Lua program).

Success depends on several factors, including:

* System with tag/host key is found

* Person with tag/host key is found

* Person has a valid directly associated login on the system

* Hackscript exists

Spawn group key is used to locate / generate the system and person used for execution.

Key is also passed through to hackscript as `key` global.


## RunRandoHackScript

`void RunHackScript(Guid key, string systemTag, string personTag, string script)`

Effectively [RunHackScript](#RunHackScript) with specific source person instead of
running as system's owner.