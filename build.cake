var target = string.IsNullOrEmpty(Argument("target", "Default")) ? "Default" : Argument("target", "Default");

var sln = "./DXNugetPackageBuilder.sln";

Task("Clean")
	.Does(() =>
{
	DeleteDirectories(GetDirectories("./src/**/obj"), new DeleteDirectorySettings
	{
		Recursive = true
	});
	DeleteDirectories(GetDirectories("./src/**/bin"), new DeleteDirectorySettings
	{
		Recursive = true
	});
});

Task("Copy-NuGet")
	.Does(() =>
{
	CreateDirectory("./src/DXNugetPackageBuilder/bin/Debug/net45");
	CopyFileToDirectory("./tools/nuget.exe", "./src/DXNugetPackageBuilder/bin/Debug/net45");
});

Task("Restore")
	.Does(() => NuGetRestore(sln));

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Restore")
	.IsDependentOn("Copy-NuGet")
	.Does(() => MSBuild(sln));


Task("Default")
	.IsDependentOn("Build");

RunTarget(target);