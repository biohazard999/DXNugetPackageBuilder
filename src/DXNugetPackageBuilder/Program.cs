using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using NuGet;

namespace DXNugetPackageBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            var arguments = ProgramArguments.Create(args);

            if (arguments == null)
            {
                Console.ReadLine();
                return;
            }

            var waringns = new List<Tuple<string, Exception>>();
            var success = new List<string>();

            if (!arguments.NugetPushOnly)
            {

                BuildPackages(arguments, dependency =>
                {
                    if (arguments.Verbose)
                        Console.WriteLine("\t" + dependency);
                },
                    ex =>
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex.ToString());
                        Console.ForegroundColor = oldColor;
                    },
                    waringns.Add,
                    ex =>
                    {
                        throw ex;
                    },
                    success.Add
                    );


                if (waringns.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("{0} Warnigns occured", waringns.Count);

                    foreach (var warning in waringns)
                    {
                        Console.WriteLine(new string('-', Console.BufferWidth));
                        Console.WriteLine(warning.Item1);
                        Console.WriteLine(new string('-', Console.BufferWidth));

                        Console.WriteLine(warning.Item2);
                    }
                }
            }

            if (arguments.NugetPush)
            {
                Console.WriteLine("Created all packages.");

                if (string.IsNullOrEmpty(arguments.NugetSource))
                {
                    Console.WriteLine("NugetSource is empty, cannot push packages");
                    Console.WriteLine("Please press enter to exit");
                    Console.ReadLine();
                }
                else
                {
                    PushPackages(arguments);
                }
            }
            else
            {
                Console.WriteLine("Created all packages, please press enter to exit");
                Console.ReadLine();
            }
        }

        private static void BuildPackages(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            if (!Directory.Exists(arguments.SourceDirectory))
            {
                logExceptionAction(new DirectoryNotFoundException($"{arguments.SourceDirectory} does not exists"));
                return;
            }

            if (!Directory.Exists(arguments.OutputDirectory))
            {
                Directory.CreateDirectory(arguments.OutputDirectory);
            }

            if (!Directory.Exists(arguments.PdbDirectory))
            {
                Directory.CreateDirectory(arguments.PdbDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, "*.dll").Concat(Directory.EnumerateFiles(arguments.SourceDirectory, "*.exe")).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("DevExpress")))
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(file);

                    var package = new PackageBuilder();

                    package.Description = "DevExpress " + packageName;
                    package.Authors.Add("Developer Express Inc.");
                    package.IconUrl = new Uri("https://www.devexpress.com/favicon.ico?v=2");
                    package.Copyright = "2008-" + DateTime.Today.Year;
                    package.ProjectUrl = new Uri("https://www.devexpress.com/");

                    package.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = file,
                        TargetPath = "lib/net40/" + Path.GetFileName(file),
                    });

                    try
                    {

                        var assembly = Assembly.LoadFile(file);
                     
                        var pdbFile = Path.ChangeExtension(Path.GetFileName(file), "pdb");

                        pdbFile = Path.Combine(arguments.PdbDirectory, pdbFile);

                        if (File.Exists(pdbFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = pdbFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(pdbFile),
                            });
                        }

                        var xmlFile = Path.ChangeExtension(file, "xml");

                        if (File.Exists(xmlFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = xmlFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(xmlFile),
                            });
                        }

                        var configFile = file + ".config";

                        if (File.Exists(configFile))
                        {
                            package.Files.Add(new PhysicalPackageFile
                            {
                                SourcePath = configFile,
                                TargetPath = "lib/net40/" + Path.GetFileName(configFile),
                            });
                        }


                        var assemblyVersion = assembly.GetName().Version;

                        var dxVersion = ".v" + assemblyVersion.Major + "." + assemblyVersion.Minor;

                        if (arguments.UseAssemblyFileVersion)
                        {
                            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                            var version = fvi.FileVersion;
                            assemblyVersion = new Version(version);
                        }

                        if (packageName.Contains(dxVersion))
                            packageName = packageName.Replace(dxVersion, string.Empty);

                        var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName + "." + assemblyVersion.ToString(4) + ".nupkg");

                        if (File.Exists(targetPackagePath))
                            File.Delete(targetPackagePath);

                        package.Id = packageName;
                        package.Version = new SemanticVersion(assemblyVersion);

                        var dependencies = new List<PackageDependency>();

                        foreach (var refAssembly in assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("DevExpress")))
                        {
                            logAction(refAssembly.Name);

                            var refPackageId = refAssembly.Name;

                            if (refPackageId.Contains(dxVersion))
                                refPackageId = refPackageId.Replace(dxVersion, string.Empty);


                            var refAssemblyVersion = refAssembly.Version;
                            
                            var minVersion = new SemanticVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build));
                            var maxVersion = new SemanticVersion(new Version(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build + 1));

                            var versionSpec = new VersionSpec { MinVersion = minVersion, MaxVersion = maxVersion, IsMinInclusive = true };

                            var dependency = new PackageDependency(refPackageId, versionSpec);
                            dependencies.Add(dependency);
                        }

                        package.DependencySets.Add(new PackageDependencySet(null, dependencies));


                        CreateLocalization(file, package, arguments);


                        using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                        {
                            package.Save(fs);

                            successAction(package.Id);
                        }

                        Console.WriteLine(packageName);
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction(ex);
                        logLoadAssemblyAction(Tuple.Create(package.Id, ex));
                    }
                }
                catch (Exception ex)
                {
                    logExceptionAction(ex);
                    unexpectedExceptionAction(ex);
                }
            }
        }


        private static void CreateLocalization(string file, PackageBuilder resourcePackage, ProgramArguments arguments)
        {
            var assemblyFileName = Path.GetFileName(file);
            var resourceFileName = Path.GetFileName(Path.ChangeExtension(file, "resources.dll"));

            foreach (var lang in arguments.LanguagesAsArray)
            {
                var localizedAssemblyPath = Path.Combine(arguments.SourceDirectory, lang, resourceFileName);
                if (File.Exists(localizedAssemblyPath))
                {
                    resourcePackage.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = localizedAssemblyPath,
                        TargetPath = "lib/net40/" + lang + "/" + Path.GetFileName(localizedAssemblyPath),
                    });
                }

                var xmlFile = Path.ChangeExtension(localizedAssemblyPath, "xml");

                if (File.Exists(xmlFile))
                {
                    resourcePackage.Files.Add(new PhysicalPackageFile
                    {
                        SourcePath = xmlFile,
                        TargetPath = "lib/net40/" + lang + "/" + Path.GetFileName(xmlFile),
                    });
                }
            }
        }

        private static void PushPackages(ProgramArguments arguments)
        {
            var packages = Directory.GetFiles(arguments.OutputDirectory, "*.nupkg").ToList();

            Console.WriteLine("Pushing {0} packages to " + arguments.NugetSource, packages.Count());

            foreach (var package in packages)
            {
                try
                {
                    var packageName = "\"" + package + "\"";

                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo("nuget.exe", string.Format("push {0} -Source {1} -ApiKey {2}", packageName, arguments.NugetSource, arguments.NugetApiKey));
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.ErrorDialog = false;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;

                        process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
                        process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

                        process.EnableRaisingEvents = true;

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Console.ReadKey();
                }

            }
        }
    }
}
