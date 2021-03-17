using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace label_translator
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Tell us what's going on.", Default = false)]
        public bool Verbose { get; set; }

        [Option('f', "folder", Required = false, HelpText = "Selects the source folder where the messages are being read.", Default = ".")]
        public string SourceFolder { get; set; }

        [Option('b', "base-file", Required = false, HelpText = "Base file to import, typically messages.xlf.", Default = "messages.xlf")]
        public string BaseFile { get; set; }

        [Option('l', "source-language", Required = false, HelpText = "Source language.", Default = "en-US")]
        public string SourceLanguage { get; set; }

        [Option('s', "translation-service", Required = false, HelpText = "Cognitive services translation service name.")]
        public string CsService { get; set; }

        [Option('k', "translation-key", Required = false, HelpText = "Cognitive services key.")]
        public string CsAPIKey { get; set; }

        [Option('q', "fix-source", Required = false, HelpText = "Overwrite source elements with the source from the original language file.")]
        public bool FixSource { get; set; }
    }
}
