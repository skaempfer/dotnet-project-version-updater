﻿# Project Version Updater

Update a project's version with the option to also increase the version of all projects inside a solution dependening on it.

In a solution of multiple projects connected by project references that are releasable as NuGet packages it is a challenge to pursue a strategy that aims at releasing NuGet packages individually for each package: At NuGet package creation project references are converted to package references. In the process of updating a project it is therefore crucial to also update the version all projects that depend on it. Let's assume we have the following object graph `A <== B <== C` with project B dependening on project A and project C depending on project B. In case you need to update project A you automatically have to update project B and C to make them use the correct NuGet package version of the updated project A. When done by hand this update process is time-consuming and error-prone. This tool helps you achive this task fast and reliably.  

Example usage:

Increase the version of project `ExampleProject` by one minor version and make it a prerelease. The versions of all projects within the solution which depend on this project will be increased by one patch prerelease version. 

```powershell
PS> dotnet update-project --update minor --prerelease --dependants .\ExampleProject.csproj
```

## Installation

### First time installation

If you want to install this tool in your workspace then type the following command:

```powershell
PS> dotnet tool install --local ProjectVersionUpdater
```

### Restore

If this tool is already a known dependency in your workspace then type the following command to restore it:

```powershell
PS> dotnet tool restore
```

## General Usage

```powershell
PS> dotnet update-project [options] <argument>
```

### Arguments

- `ProjectPath` (required): The path to the project file to update

### Options

- `-u` | `--update` (optional): Indicates which version part to increase: major, minor or patch. Defaults to major if omitted.
- `-p` | `--prerelease` (optional):  Indicates if version update should be a prerelease. If no value is provided the naming schema defaults to.
- `-n` | `--name` (optional): Use a custom prerelease label. If omitted the default prelease naming scheme is used: '<version>-[major|minor|patch].[number]'.
- `-d` | `--dependants` (optional): Indicates if all projects which are (transitevely) dependent on the project to update should be updated as well.

## Usage Examples

Update a project to its next major version. Update all its dependent projects to the next patch version.

```powershell
PS> dotnet update-project --update major --dependants .\Project.csproj
```

Update a project to its next minor prerelease version. Update all its dependent projects to the next patch prerelease version.

```powershell
PS> dotnet update-project --update major --prerelease --dependants .\Project.csproj
```

Update a project with a prerelease version to its next release version. Update all its dependent projects to the next patch release. Projects with release versions will be increased by 1, projects with prerelease versions will be increased to the next patch release version

```powershell
PS> dotnet update-project --update minor --dependants .\Project.csproj
```

Update multiple projects to its next release version. Update all their dependant projects to the next patch release.

```powershell
PS> dotnet update-project --update major --dependants .\Project.csproj .\Project.Abstractions.csproj
```

## Local execution from development environment

For local execution of the application you have to three options:

1. Using the command line. Located in the project folder execute the application by running `dotnet run -- <path/to/project/file/to/update>`.
2. [Using Visual Studio for running and debugging.](https://docs.microsoft.com/en-us/dotnet/core/tutorials/debugging-with-visual-studio)
3. [Using Visual Studio Code for running and debugging.](https://docs.microsoft.com/en-us/dotnet/core/tutorials/debugging-with-visual-studio-code)

## Build and Publish

Prerequisites:

For the `dotnet nuget` tool to work correctly with authenticated Azure DevOps Artifacts feeds make sure that you have installed Microsoft's Nuget Credential Provider as described [here](https://github.com/Microsoft/artifacts-credprovider/#setup).  

To build and publish this project as a dotnet cli tool run the following commands:

```powershell
PS> dotnet pack -c Release
PS> dotnet nuget push -s <Feed> -k <Key> .\nupkg\<packagename>.nupkg
```
