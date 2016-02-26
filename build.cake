var target = Argument("target", "Default");

Task("Paket-Bootstrapper")
	.Does(() =>
{
	StartProcess(".paket/paket.bootstrapper.exe", new ProcessSettings{
      WorkingDirectory = ".",
      Arguments = ""
   });   	
});

Task("Paket-Restore")
	.IsDependentOn("Paket-Bootstrapper")
	.Does(() =>
{
	StartProcess(".paket/paket.exe", new ProcessSettings{
      WorkingDirectory = ".",
      Arguments = "restore"
   });   	
});

Task("Paket-Install")
	.IsDependentOn("Paket-Bootstrapper")
	.Does(() =>
{
	StartProcess(".paket/paket.exe", new ProcessSettings{
      WorkingDirectory = ".",
      Arguments = "install"
   });   	
});


Task("Clean")
	.Does(() =>
{
	CleanDirectories("./src/**/bin/debug");
});


Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Paket-Install")	
	.Does(() =>
{
	DotNetBuild("./DXNugetPackageBuilder.sln");
});


Task("Default").IsDependentOn("Build");

RunTarget(target);