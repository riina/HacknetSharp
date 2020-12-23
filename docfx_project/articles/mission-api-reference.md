# Mission API reference

The mission API provides some rudimentary logic for missions.

Missions are defined as mission templates in YAML
([Template reference](template-reference.md)) with conditions and behaviour
written inline as lua code.

## me

`PersonModel me`

The person undertaking the active mission.

## person_t

`PersonModel? person_t(string tag)`

Tries to get a person with the specified unique tag.

## system_t

`SystemModel? system_t(string tag)`

Tries to get a system with the specified unique tag.

## system_a

`SystemModel? system_a(string tag)`

Tries to get a system with the specified IP address.

## home

`SystemModel? home(PersonModel? person)`

Attempts to get the home system for a person.

## start_mission

`MissionModel? start_mission(PersonModel? person, string missionPath)`

Attempts to start the specified mission.

## remove_mission

`void remove_mission(MissionModel? mission)`

Removes (ends) a mission.

## system_up

`bool system_up(SystemModel? system)`

Checks if a system is up. Returns false if system is null.

## file_exists

`bool file_exists(SystemModel? system, string path)`

Checks if the filesystem on the specified system contains
a file/folder with the provided path.

## file_contains

`bool file_contains(SystemModel? system, string path, string text, bool ignoreCase)`

Checks if a file exists on the specified system and contains the specified text.

## log

`void log(string message)`

Writes a `LogLevel.Information` message to the world's logger.

## log_ex

`void log_ex(string message, int level)`

Writes a message to the world's logger.

* 0: LogLevel.Information
* 1: LogLevel.Warning
* 2: LogLevel.Error
* default: LogLevel.Critical

## spawn_person

`PersonModel spawn_person(string name, string username)`

Spawns a person with the specified proper name and username.

## spawn_person_tagged

`PersonModel spawn_person_tagged(string name, string username, string tag)`

Spawns a person with the specified proper name, username, and unique tag.

## spawn_system

`SystemModel? spawn_system(PersonModel? owner, string password, string template, string addressOrAddressRange)`

Attempts to spawn a system.

## remove_person

`void remove_person(PersonModel? person)`

Removes a person (and all systems).

## remove_system

`void remove_system(SystemModel? system)`

Removes a system.

## spawn_file

`FileModel? spawn_file(SystemModel? system, string path, string content)`

Spawns a file with the specified content. Returns an existing file if applicable.

## remove_file

`void remove_file(FileModel? file)`

Removes a file.