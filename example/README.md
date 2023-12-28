# Example for Project Version Updater

This example is comprised of three projects that have the following dependency hierarchy:

`Project A <-- Project B <-- Project C`

The example shows the functionality of the Project Version Updater dotnet tool to update Project A as well as resolving the projects that are directly or transitevly depending on Project A and updating them as well.

## Running the example

1. Run `init-example.ps1` to make a version of the build tool package available in the example workspace
3. Run `dotnet update-project --update major --dependants .\ProjectA\ProjectA.csproj`. This will
   1. Update the version of `Project A` to its next major version
   2. Update the version of `Project B` and `Project C` to their next bugfix version.
