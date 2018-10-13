set DXVersion=18.1
set SymbolsFolder=c:\tmp\symbols
set TargetNugetFolder=C:\tmp\Nuget
set Localization=de;es;ja;ru
set NugetServer=
REM set NugetServer=-NugetSource http://yourNugetServer/
set NugetApiKey=
REM set NugetApiKey=-NugetApiKey Your-Api-Key-Goes-Here
set NugetPush=
REM set NugetPush=-NugetPush

REM Add "-Verbose" for verbosity
Powershell.exe -executionpolicy remotesigned -File build.ps1

set Builder=src\DXNugetPackageBuilder\bin\Debug\netcoreapp2.1\win-x64\DXNugetPackageBuilder.exe

%Builder% --src="C:\Program Files (x86)\DevExpress %DXVersion%\DevExpressCodedUIExtensions\Tools" --pdb="%SymbolsFolder%" -o "%TargetNugetFolder%" --lang=%Localization%
REM --nugetsource=%NugetServer% --nugetapikey=%NugetApiKey% --nugetpush=%NugetPush%

%Builder% --src="C:\Program Files (x86)\DevExpress %DXVersion%\Components\Tools\eXpressAppFramework\Model Editor" --pdb="%SymbolsFolder%" -o "%TargetNugetFolder%" --lang=%Localization%
REM --nugetsource=%NugetServer% --nugetapikey=%NugetApiKey% --nugetpush=%NugetPush%

REM Add "-Verbose" for verbosity
%Builder% --src="C:\Program Files (x86)\DevExpress %DXVersion%\Components\Bin\Framework" --pdb="%SymbolsFolder%" -o "%TargetNugetFolder%" --lang=%Localization%
REM --nugetsource=%NugetServer% --nugetapikey=%NugetApiKey% --nugetpush=%NugetPush%

