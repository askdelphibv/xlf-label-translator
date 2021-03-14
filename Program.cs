using CommandLine;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace label_translator
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                if (o.Verbose)
                {
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    Trace.AutoFlush = true;
                    Trace.TraceInformation($"Options: {JsonConvert.SerializeObject(o)}");
                }

                var engine = new TranslationEngine(o);
                Task t = engine.Run();
                t.Wait();
            });
        }
    }
}
