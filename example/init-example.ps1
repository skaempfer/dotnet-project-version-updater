Remove-Item $PSScriptRoot\.config\ -Recurse -ErrorAction Ignore

Remove-Item $PSScriptRoot\nuget\ -Recurse -ErrorAction Ignore

& dotnet pack $PSScriptRoot\..\src\ProjectVersionUpdater\ProjectVersionUpdater.csproj -c Continuous -o $PSScriptRoot\nuget

& dotnet new tool-manifest

& dotnet tool install --no-cache --configfile $PSScriptRoot\nuget.config --tool-manifest $PSScriptRoot\.config\dotnet-tools.json --prerelease ProjectVersionUpdater

