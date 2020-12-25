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

## self

`Executable self`

Current executable.

Only available to programs/services.

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

# Stanadard Members

## StartMission

`MissionModel? StartMission(PersonModel? person, string missionPath)`

Attempts to start the specified mission.

## RemoveMission

`void RemoveMission(MissionModel? mission)`

Removes (ends) a mission.

## PersonT

`PersonModel[]? PersonT(string tag)`

Tries to get persons with the specified tag.

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

`PersonModel SpawnPersonG(string name, string username, string tag, Guid key)`

Spawns a person with the specified proper name, username, tag, and group key.

## RemovePerson

`void RemovePerson(PersonModel? person)`

Removes a person (and all systems).

## SystemT

`SystemModel[]? SystemT(string tag)`

Tries to get system with the specified tag.

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

`SystemModel? SpawnSystemG(PersonModel? owner, string password, string template, string addressOrAddressRange, string tag, Guid key)`

Attempts to spawn a system with the specified tag and group key.

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