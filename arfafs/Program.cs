using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using xayrga;

namespace arfafs
{
    class Program
    {
        static string last_text;
        public static void consoleProgress(string txt, int progress, int max, bool show_progress = false)
        {
            if (last_text == txt && !show_progress)
                return;
            last_text = txt;
            var flt_total = (float)progress / max;
            Console.CursorLeft = 0;
            //Console.WriteLine(flt_total);
            Console.Write($"{txt} [");
            for (float i = 0; i < 32; i++)
                if (flt_total > (i / 32f))
                    Console.Write("#");
                else
                    Console.Write(" ");
            Console.Write("]");
            if (show_progress)
                Console.Write($" ({progress}/{max})");
        }

        static void Main(string[] args)
        {

            cmdarg.cmdargs = args;

            var inAfs = cmdarg.assertArg(0, "AFS File");
            var outFolder = cmdarg.tryArg(1, "Output Folder (Optional)");
            bool force_numeric_output = cmdarg.findDynamicFlagArgument("--ignore-filename");
            bool filenumbers = cmdarg.findDynamicFlagArgument("--nopadname");

            if (outFolder==null)
                outFolder = $"AFS_{Path.GetFileName(inAfs)}";
            cmdarg.assert(!File.Exists(inAfs), $"'{inAfs}' does not exist.");

            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            AFSFile dataFile = null;
            FileStream fdata = null;

            try { fdata = File.OpenRead(inAfs); }
            catch (Exception E) { cmdarg.assert($"Failed to open '{inAfs}': '{E.Message}'"); }

            try {dataFile = AFSFile.load(new BinaryReader(fdata));}
            catch (Exception E) { cmdarg.assert($"Failed decode AFS '{inAfs}': '{E.Message}'"); }

            for (int i = 0; i < dataFile.sectionCount; i++)
            {
                consoleProgress("Extracting AFS", i + 1, dataFile.sectionCount ,true);
                if (dataFile.sections[i].descriptor != null && !force_numeric_output)
                    File.WriteAllBytes($"{outFolder}/{dataFile.sections[i].descriptor.name}", dataFile.sections[i].data);
                else if (!filenumbers)
                    File.WriteAllBytes($"{outFolder}/{i:D4}.dat", dataFile.sections[i].data);
                else
                    File.WriteAllBytes($"{outFolder}/{i}.dat", dataFile.sections[i].data);
            }
        }
    }
}