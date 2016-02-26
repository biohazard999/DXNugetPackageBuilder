using System.ComponentModel;
using Ookii.CommandLine;
using System;

namespace DXNugetPackageBuilder
{
    [Description("Builds Nuget Packages from DevExpress-Assemblies")]
    public class ProgramArguments
    {

        [CommandLineArgument(Position = 0, IsRequired = true), Description("The directory where the DevExpress Assemblies live")]
        public string SourceDirectory { get; set; }

        [CommandLineArgument(Position = 1, IsRequired = true), Description("The directory where the DevExpress PDB's live")]
        public string PdbDirectory { get; set; }

        [CommandLineArgument(Position = 2, IsRequired = true), Description("The directory where the Nuget-Packages should be written")]
        public string OutputDirectory { get; set; }

        [CommandLineArgument(Position = 3, IsRequired = false), Description("The supported languages, seperated by ;")]
        public string Languages { get; set; }

        [CommandLineArgument(IsRequired = false)]
        public bool NugetPushOnly { get; set; }

        [CommandLineArgument(IsRequired = false)]
        public bool NugetPush { get; set; }

        [Description("The target nuget source location")]
        [CommandLineArgument(Position = 4)]
        public string NugetSource { get; set; }

        [CommandLineArgument(IsRequired = false, DefaultValue = false)]
        public bool UseAssemblyFileVersion { get; set; }

        public string[] LanguagesAsArray
        {
            get
            {
                if(String.IsNullOrEmpty(Languages))
                    return new string[] { };

                return Languages.Split(';');
            }
        }

        [CommandLineArgument, Description("Verbose Log output")]
        public bool Verbose { get; set; }

        [CommandLineArgument(Position = 5)]
        public string NugetApiKey { get; set; }

        public static ProgramArguments Create(string[] args)
        {
            // Using a static creation function for a command line arguments class is not required, but it's a convenient
            // way to place all command-line related functionality in one place. To parse the arguments (eg. from the Main method)
            // you then only need to call this function.
            var parser = new CommandLineParser(typeof(ProgramArguments));
            // The ArgumentParsed event is used by this sample to stop parsing after the -Help argument is specified.
            parser.ArgumentParsed += CommandLineParser_ArgumentParsed;
            try
            {
                // The Parse function returns null only when the ArgumentParsed event handler cancelled parsing.
                var result = (ProgramArguments)parser.Parse(args);
                if(result != null)
                    return result;
            }
            catch(CommandLineArgumentException ex)
            {
                // We use the LineWrappingTextWriter to neatly wrap console output.
                using(var writer = LineWrappingTextWriter.ForConsoleError())
                {
                    // Tell the user what went wrong.
                    writer.WriteLine(ex.Message);
                    writer.WriteLine();
                }
            }

            // If we got here, we should print usage information to the console.
            // By default, aliases and default values are not included in the usage descriptions; for this sample, I do want to include them.
            var options = new WriteUsageOptions() { IncludeDefaultValueInDescription = true, IncludeAliasInDescription = true };
            // WriteUsageToConsole automatically uses a LineWrappingTextWriter to properly word-wrap the text.
            parser.WriteUsageToConsole(options);
            return null;
        }

        private static void CommandLineParser_ArgumentParsed(object sender, ArgumentParsedEventArgs e)
        {
            // When the -Help argument (or -? using its alias) is specified, parsing is immediately cancelled. That way, CommandLineParser.Parse will
            // return null, and the Create method will display usage even if the correct number of positional arguments was supplied.
            // Try it: just call the sample with "CommandLineSampleCS.exe foo bar -Help", which will print usage even though both the Source and Destination
            // arguments are supplied.
            if(e.Argument.ArgumentName == "Help") // The name is always Help even if the alias was used to specify the argument
                e.Cancel = true;
        }
    }
}