# Fp - File Processing

## Quick Start
1. Get the .NET C# template [here](https://www.nuget.org/packages/Fp.Templates)
2. Create a new project with `dotnet new fpt [-n projectName]`
3. Implement processing logic in the provided subclass of `Fp.Processor`
4. Test with `dotnet run -f net5.0 [-p projectPath] -- [sources...] [-o target] [-- args]`
5. Use a build script to create a native binary under `publish` and execute with `<toolExe> [sources...] [-o target] [-- args]`