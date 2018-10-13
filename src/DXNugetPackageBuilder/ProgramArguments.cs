using CommandLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DXNugetPackageBuilder
{
    [Description("Builds Nuget Packages from DevExpress-Assemblies")]
    public class ProgramArguments
    {
        [Option("src", Required = true, HelpText = "The directory where the DevExpress Assemblies live")]
        public string SourceDirectory { get; set; }

        [Option("pdb", Required = true, HelpText = "The directory where the DevExpress PDB's live")]
        public string PdbDirectory { get; set; }

        [Option('o', "output", Required = true, HelpText = "The directory where the Nuget-Packages should be written")]
        public string OutputDirectory { get; set; }

        [Option("lang", Required = false, HelpText = "The supported languages, separated by ;")]
        public string Languages { get; set; }

        [Option(Required = false)]
        public bool NugetPushOnly { get; set; }

        [Option(Required = false)]
        public bool NugetPush { get; set; }

        [Option(Required = false, HelpText= "The target nuget source location")]
        public string NugetSource { get; set; }

        [Option(Required = false, Default = false)]
        public bool UseAssemblyFileVersion { get; set; }

        [Option(Required = false, Default = false)]
        public bool Strict { get; set; }

        public IEnumerable<string> LanguagesEnumerable
        {
            get
            {
                if (string.IsNullOrEmpty(Languages))
                {
                    return Enumerable.Empty<string>();
                }
                return Languages.Split(';');
            }
        }

        [Option(HelpText = "Verbose Log output")]
        public bool Verbose { get; set; }

        [Option]
        public string NugetApiKey { get; set; }
    }
}