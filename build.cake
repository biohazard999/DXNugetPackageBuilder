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

Task("Default")
  .Does(() =>
{
  Information("Hello World!");
});

RunTarget(target);