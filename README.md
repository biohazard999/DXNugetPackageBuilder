# DXNugetPackageBuilder
A nuget package builder for the [DevExpress](//www.devexpress.com) Universal Suite

![Build](https://mgrundner.visualstudio.com/DefaultCollection/_apis/public/build/definitions/289e8c64-e092-4fea-b963-56339082e2f2/38/badge)

## Preparation

From your [Download-Manager](https://www.devexpress.com/ClientCenter/DownloadManager/)
 
- Install the .NET Controls & Libraries Installer
- Install the Coded UI Test Extensions for WinForms
- Install CodeRush
- Download the .NET Controls and Libraries PDB Files
	- Extract them to c:\tmp\symbols

To build and run your will need .NET 4.6 & Visual Studio 2015 (any kind)

## Usage

Adjust the parameters of the `buildPackages.bat`

Example:

```cmd
set DXVersion=15.2
set SymbolsFolder=c:\tmp\symbols
set TargetNugetFolder=C:\tmp\Nuget
set Localization=de;es;ja;ru
set NugetServer=
REM set NugetServer=-NugetSource http://yourNugetServer/
set NugetApiKey=
REM set NugetApiKey=-NugetApiKey Your-Api-Key-Goes-Here
set NugetPush=
REM set NugetPush=-NugetPush


Powershell.exe -executionpolicy remotesigned -File  build.ps1

set Builder=src\DXNugetPackageBuilder\bin\Debug\DXNugetPackageBuilder.exe

%Builder% "C:\Program Files (x86)\DevExpress %DXVersion%\DevExpressCodedUIExtensions\Tools" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%

%Builder% "C:\Program Files (x86)\DevExpress %DXVersion%\Components\Tools\eXpressAppFramework\Model Editor" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%

%Builder% "C:\Program Files (x86)\DevExpress %DXVersion%\Components\Bin\Framework" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%
```

Run it!

The default settings will output your fresh nuget packages to `C:\tmp\Nuget`

Enjoy!

### To publish automatically

Adjust:
 
```cmd
REM set NugetServer=
set NugetServer=-NugetSource http://yourNugetServer/
REM set NugetApiKey=
set NugetApiKey=-NugetApiKey Your-Api-Key-Goes-Here
REM set NugetPush=
set NugetPush=-NugetPush
```

> To publish your packages to a sepearte nuget server make sure you have nuget.exe (2.8) on your path enviroment variable.


## Contribution
Pull Request and other contributions are welcome!

### Issues
Feel free to file an [issue](//github.com/biohazard999/DXNugetPackageBuilder/issues)!

## More Info

[Blog](http://blog.delegate.at)