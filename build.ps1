$BuildProjectFile = "$PSScriptRoot/build/_build.csproj"
$env:NUKE_ROOT = $PSScriptRoot
dotnet run --project $BuildProjectFile -- @args
