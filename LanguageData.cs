using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace label_translator
{
    public class LanguageData
    {
        // source file
        public FileInfo XlifFile;

        // the XML document for that source file
        public XmlDocument XmlDocument;

        // The list of labels for this language
        public SortedList<string, Label> Labels = new SortedList<string, Label>();

        internal XmlNamespaceManager NamespaceManager;
    }
}
