set DXVersion=15.1
set SymbolsFolder=c:\tmp\symbols
set TargetNugetFolder=C:\tmp\Nuget
set Localization=de;es;ja;ru
set NugetApiKey=
set NugetServer=
set NugetPush=
REM set NugetApiKey=-NugetApiKey 4191b932-d111-4add-a259-69b875d00b6f
REM set NugetServer=-NugetSource http://paratfs:8081/
REM set NugetPush=-NugetPush


set Builder=src\DXNugetPackageBuilder\bin\Debug\DXNugetPackageBuilder.exe

%Builder% "C:\Program Files (x86)\DevExpress %DXVersion%\DevExpressCodedUIExtensions\Tools" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%

%Builder% "C:\Program Files (x86)\DevExpress %DXVersion%\Components\Tools\eXpressAppFramework\Model Editor" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%

%Builder% "C:\Program Files (x86)\DevExpress 15.1\Components\Bin\Framework" %SymbolsFolder% %TargetNugetFolder% %Localization% %NugetServer% %NugetApiKey% %NugetPush%