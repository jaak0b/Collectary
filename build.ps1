param(
    [Parameter(Position=0)][string] $Target,
    [string] $Configuration = "Debug",
    [switch] $NoLogo,
    [switch] $Help,
    [Parameter(ValueFromRemainingArguments)] $BuildArguments
)

$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
$BuildProjectFile = "$PSScriptRoot/build/_build.csproj"

$env:NUKE_ROOT = $PSScriptRoot

dotnet run --project $BuildProjectFile -- `
    $(if ($Target) { "--target $Target" }) `
    $(if ($Configuration) { "--configuration $Configuration" }) `
    $(if ($NoLogo) { "--no-logo" }) `
    $(if ($Help) { "--help" }) `
    $BuildArguments
