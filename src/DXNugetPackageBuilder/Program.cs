using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;

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

            var warnings = new List<Tuple<string, Exception>>();
            var success = new List<string>();

            if (!arguments.NugetPushOnly)
            {
                BuildPackages(arguments, dependency =>
                {
                    if (arguments.Verbose)
                    {
                        Console.WriteLine("\t" + dependency);
                    }
                },
                    ex =>
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(ex);
                        Console.ForegroundColor = oldColor;
                    },
                    warnings.Add,
                    ex => throw ex,
                    success.Add
                    );

                if (warnings.Count > 0)
                {
                    var oldColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine($"{warnings.Count} Warnings occurred");

                    foreach (var warning in warnings)
                    {
                        Console.WriteLine(new string('-', Console.BufferWidth));
                        Console.WriteLine(warning.Item1);
                        Console.WriteLine(new string('-', Console.BufferWidth));

                        Console.WriteLine(warning.Item2);
                    }
                    Console.ForegroundColor = oldColor;
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

        static void BuildPackages(ProgramArguments arguments, Action<string> logAction, Action<Exception> logExceptionAction, Action<Tuple<string, Exception>> logLoadAssemblyAction, Action<Exception> unexpectedExceptionAction, Action<string> successAction)
        {
            if (!Directory.Exists(arguments.SourceDirectory))
            {
                logExceptionAction?.Invoke(new DirectoryNotFoundException($"{arguments.SourceDirectory} does not exists"));
                return;
            }

            // We assume that the Components\Bin has 2 subfolders : Framework for net40, Standard for netstandard20
            var standardDirectory = Path.Combine(Directory.GetParent(arguments.SourceDirectory).FullName, "Standard");

            if (!Directory.Exists(arguments.OutputDirectory))
            {
                Directory.CreateDirectory(arguments.OutputDirectory);
            }

            if (!Directory.Exists(arguments.PdbDirectory))
            {
                Directory.CreateDirectory(arguments.PdbDirectory);
            }

            // When downloading the pdbs from DevExpress, the pdb archive has a subfolder Standard that holds the netstandard20 pdbs
            var standardPdbDirectory = Path.Combine(arguments.PdbDirectory, "Standard");

            if (!Directory.Exists(standardPdbDirectory))
            {
                Directory.CreateDirectory(standardPdbDirectory);
            }

            foreach (var file in Directory.EnumerateFiles(arguments.SourceDirectory, "*.dll").Concat(Directory.EnumerateFiles(arguments.SourceDirectory, "*.exe")).Where(f => Path.GetFileNameWithoutExtension(f).StartsWith("DevExpress", StringComparison.Ordinal)))
            {
                try
                {
                    var packageName = Path.GetFileNameWithoutExtension(file);

                    var package = new PackageBuilder
                    {
                        Description = "DevExpress " + packageName
                    };
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
                        var assembly = Assembly.LoadFile(file); // Will load from GAC if components are installed from DevExpress Installer
                        logAction?.Invoke($"Assembly {assembly.Location} loaded !");

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
                        {
                            packageName = packageName.Replace(dxVersion, string.Empty);
                        }

                        var targetPackagePath = Path.Combine(arguments.OutputDirectory, packageName + "." + assemblyVersion.ToString(4) + ".nupkg");

                        if (File.Exists(targetPackagePath))
                        {
                            File.Delete(targetPackagePath);
                        }

                        package.Id = packageName;
                        package.Version = new NuGetVersion(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);

                        logAction?.Invoke("net40 dependencies:");
                        // We only go for DevExpress dependencies - the rest should be only .NET Framework dependencies
                        var dependencies = PullDependencies(arguments, logAction, package, assembly.GetReferencedAssemblies().Where(r => r.Name.StartsWith("DevExpress", StringComparison.Ordinal)), dxVersion);
                        if (dependencies.Count == 0)
                        {
                            logAction?.Invoke($"No net40 dependencies!");
                        }

                        // We need to provide an explicit framework name, in case we have a netstandard20 version
                        package.DependencyGroups.Add(new PackageDependencyGroup(new NuGetFramework(".NETFramework, Version=4.0", new Version(4,0)), dependencies));

                        // netstandard20 part - skipped if not standard folder is found next to the framework folder.
                        // Only checks if we have a file of the same name in the standard folder, it does not create pure standard20 packages.
                        if (Directory.Exists(standardDirectory))
                        {
                            var standardFile = Path.Combine(standardDirectory, Path.GetFileName(file));
                            if (File.Exists(standardFile))
                            {
                                package.Files.Add(new PhysicalPackageFile
                                {
                                    SourcePath = standardFile,
                                    TargetPath = "lib/netstandard20/" + Path.GetFileName(standardFile),
                                });

                                // PDB Files retains the name, only the folder is different
                                var standardPdbFile = Path.Combine(standardPdbDirectory, pdbFile);

                                if (File.Exists(standardPdbFile))
                                {
                                    package.Files.Add(new PhysicalPackageFile
                                    {
                                        SourcePath = standardPdbFile,
                                        TargetPath = "lib/netstandard20/" + Path.GetFileName(standardPdbFile),
                                    });
                                }

                                // XML Files are identical for both targets
                                if (File.Exists(xmlFile))
                                {
                                    package.Files.Add(new PhysicalPackageFile
                                    {
                                        SourcePath = xmlFile,
                                        TargetPath = "lib/netstandard20/" + Path.GetFileName(xmlFile),
                                    });
                                }

                                var standardAssembly = Assembly.LoadFile(standardFile);
                                logAction?.Invoke($"Assembly {standardAssembly.Location} loaded !");
                                // .Net Standard version of Assembly.LoadFrom would work, by not accessing the GAC
                                if(standardAssembly.Location.Contains("GAC_MSIL"))
                                {
                                    logExceptionAction?.Invoke(new FileLoadException("Trying to load a standard dll from the GAC. It won't work, the script shouldn't be run on computer where DevExpress Installer has been run! Copy the necessary components on a computer without DevExpress installed."));
                                }

                                logAction?.Invoke("netstandard20 dependencies:");
                                // We go for all dependencies, as all of them should be pull from NuGet packages
                                var standardDependencies = PullDependencies(arguments, logAction, package, standardAssembly.GetReferencedAssemblies(), dxVersion);
                                if (dependencies.Count == 0)
                                {
                                    logAction?.Invoke($"No netstandard dependencies!");
                                }
                                package.DependencyGroups.Add(new PackageDependencyGroup(new NuGetFramework(".NETStandard, Version=2.0", new Version(2,0)), standardDependencies));
                            }
                        }

                        CreateLocalization(file, package, arguments);

                        using (var fs = new FileStream(targetPackagePath, FileMode.CreateNew, FileAccess.ReadWrite))
                        {
                            package.Save(fs);

                            successAction?.Invoke(package.Id);
                        }

                        Console.WriteLine(packageName);
                    }
                    catch (Exception ex)
                    {
                        logExceptionAction?.Invoke(ex);
                        logLoadAssemblyAction?.Invoke(Tuple.Create(package.Id, ex));
                    }
                }
                catch (Exception ex)
                {
                    logExceptionAction?.Invoke(ex);
                    unexpectedExceptionAction?.Invoke(ex);
                }
            }
        }

        /// <summary>
        /// Pulls the dependencies from an assembly - works for both net40 and netstandard20
        /// </summary>
        /// <param name="arguments">The arguments provided to the command line.</param>
        /// <param name="logAction">The log action.</param>
        /// <param name="package">The package builder.</param>
        /// <param name="referencedAssemblies">The referenced assemblies from which we'll deduce the dependencies.</param>
        /// <param name="dxVersion">The devExpress version currently built.</param>
        /// <returns></returns>
        static List<PackageDependency> PullDependencies(ProgramArguments arguments, Action<string> logAction, PackageBuilder package, IEnumerable<AssemblyName> referencedAssemblies, string dxVersion)
        {
            var dependencies = new List<PackageDependency>();

            foreach (var refAssembly in referencedAssemblies)
            {
                logAction?.Invoke(refAssembly.Name + ": " + refAssembly.Version);

                var refPackageId = refAssembly.Name;

                if (refPackageId.StartsWith("DevExpress", StringComparison.Ordinal) && refPackageId.Contains(dxVersion))
                {
                    refPackageId = refPackageId.Replace(dxVersion, string.Empty);
                }

                var refAssemblyVersion = refAssembly.Version;

                VersionRange versionRange;

                if (refPackageId == "netstandard")
                {
                    // .NET Standard ugly part - The dll versions are not matching the packages
                    // An alternative, would be to extract the versions from the csproj in the pdbs
                    refPackageId = "NETStandard.Library";
                    if (refAssemblyVersion.Major == 2 && refAssemblyVersion.Minor == 0 && refAssemblyVersion.Build <= 3)
                    {
                        versionRange = new VersionRange(new NuGetVersion(2,0,3));
                        logAction?.Invoke("     Forced Version:" + versionRange);
                    }
                }

                if (refPackageId.StartsWith("DevExpress", StringComparison.Ordinal))
                {
                    var minVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build);
                    var maxVersion = new NuGetVersion(refAssemblyVersion.Major, refAssemblyVersion.Minor, refAssemblyVersion.Build + 1);
                    versionRange = new VersionRange(minVersion, true, maxVersion);
                }
                else
                {
                    // .NET Standard ugly part - The dll versions are not matching the packages
                    // An alternative, would be to extract the versions from the csproj in the pdbs
                    if ((refPackageId == "System.Drawing.Common" || refPackageId == "System.CodeDom" || refPackageId == "System.Data.SqlClient" || refPackageId == "System.Configuration.ConfigurationManager")
                        && refAssemblyVersion.Major == 4 && refAssemblyVersion.Minor <= 5)
                    {
                        versionRange = new VersionRange(new NuGetVersion(4,5,0));
                        logAction?.Invoke("     Forced Version:" + versionRange);
                    }
                    else if ((refPackageId == "System.ComponentModel.Annotations" || refPackageId == "System.ServiceModel.Primitives" || refPackageId == "System.ServiceModel.Http" || refPackageId == "System.Text.Encoding.CodePages")
                        && refAssemblyVersion.Major == 4 && refAssemblyVersion.Minor <= 4)
                    {
                        versionRange = new VersionRange(new NuGetVersion(4,4,0));
                        logAction?.Invoke("     Forced Version:" + versionRange);
                    }
                    else if ((refPackageId == "System.Reflection.Emit" || refPackageId == "System.Reflection.Emit.Lightweight" || refPackageId == "System.Reflection.Emit.ILGeneration")
                        && refAssemblyVersion.Major == 4 && refAssemblyVersion.Minor <= 3)
                    {
                        versionRange = new VersionRange(new NuGetVersion(4,3,0));
                        logAction?.Invoke("     Forced Version:" + versionRange);
                    }
                    else
                    {
                        versionRange = new VersionRange(new NuGetVersion(refAssemblyVersion));
                    }
                }

                var dependency = new PackageDependency(refPackageId, versionRange);

                if (!arguments.Strict)
                {
                    var skippedDependencies = new Dictionary<string, string[]>
                    {
                        ["DevExpress.Persistent.Base"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraReports.Extensions",
                                    "DevExpress.Printing.Core",
                                },

                        ["DevExpress.Persistent.BaseImpl"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Reports",
                                    "DevExpress.XtraReports",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.XtraScheduler.Core",
                                },

                        ["DevExpress.Persistent.BaseImpl.EF"] = new[]
                                {
                                    "DevExpress.Utils",
                                    "DevExpress.ExpressApp.Kpi",
                                    "DevExpress.ExpressApp.ReportsV2",
                                    "DevExpress.ExpressApp.Security",
                                    "DevExpress.ExpressApp.ConditionalAppearance",
                                    "DevExpress.ExpressApp.StateMachine",
                                    "DevExpress.ExpressApp.Chart",
                                    "DevExpress.XtraReports",
                                    "DevExpress.XtraScheduler.Core",
                                    "DevExpress.ExpressApp.Reports"
                                }
                    };

                    if (skippedDependencies.Keys.Any(id => package.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        var skippedDependency = skippedDependencies[package.Id];

                        if (skippedDependency.Any(dependency.Id.Equals))
                        {
                            logAction?.Invoke($"Skipping Dependency: {dependency.Id} for Package {package.Id} to avoid UI in Persistence");
                            continue;
                        }
                    }
                }

                dependencies.Add(dependency);
            }
            return dependencies;
        }

        static void CreateLocalization(string file, PackageBuilder resourcePackage, ProgramArguments arguments)
        {
            var resourceFileName = Path.GetFileName(Path.ChangeExtension(file, "resources.dll"));

            foreach (var lang in arguments.LanguagesEnumerable)
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

        static void PushPackages(ProgramArguments arguments)
        {
            var packages = Directory.GetFiles(arguments.OutputDirectory, "*.nupkg").ToList();

            Console.WriteLine("Pushing {0} packages to " + arguments.NugetSource, packages.Count);

            foreach (var package in packages)
            {
                try
                {
                    var packageName = "\"" + package + "\"";

                    using (var process = new Process())
                    {
                        process.StartInfo = new ProcessStartInfo("nuget.exe", $"push {packageName} -Source {arguments.NugetSource} -ApiKey {arguments.NugetApiKey}")
                        {
                            CreateNoWindow = true,
                            ErrorDialog = false,
                            RedirectStandardError = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false
                        };

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
