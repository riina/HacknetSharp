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

`PersonModel? PersonT(string tag)`

Tries to get a person with the specified unique tag.

## SpawnPerson

`PersonModel SpawnPerson(string name, string username)`

Spawns a person with the specified proper name and username.

## SpawnPersonT

`PersonModel SpawnPersonT(string name, string username, string tag)`

Spawns a person with the specified proper name, username, and unique tag.

## RemovePerson

`void RemovePerson(PersonModel? person)`

Removes a person (and all systems).

## SystemT

`SystemModel? SystemT(string tag)`

Tries to get a system with the specified unique tag.

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

## RemoveSystem

`void RemoveSystem(SystemModel? system)`

Removes a system.

## FileExists

`bool FileExists(SystemModel? system, string path)`

Checks if the filesystem on the specified system contains
a file/folder with the provided path.

## FileContains

`bool FileContains(SystemModel? system, string path, string text, bool ignoreCase)`

Checks if a file exists on the specified system and contains the specified text.

## SpawnFile

`FileModel? SpawnFile(SystemModel? system, string path, string content)`

Spawns a file with the specified content. Returns an existing file if applicable.

## RemoveFile

`void RemoveFile(FileModel? file)`

Removes a file.

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