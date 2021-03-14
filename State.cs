using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace label_translator
{
    public class State
    {
        public Dictionary<string, LanguageData> DataPerLanguage = new Dictionary<string, LanguageData>();

        public Dictionary<string, List<Label>> LabelsToBeTranslatedPerLangauge = new Dictionary<string, List<Label>>();
    }
}
