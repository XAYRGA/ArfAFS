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

        static void Main(string[] args)
        {
            cmdarg.cmdargs = args;

            var inAfs = cmdarg.assertArg(0, "AFS File");
            var outFolder = cmdarg.tryArg(1, "Output Folder");

            if (outFolder==null)
            {
                outFolder = $"AFS_{Path.GetFileName(inAfs)}";
            }


            bool force_numeric_output = cmdarg.findDynamicFlagArgument("--ignore-filename");
            bool filenumbers = cmdarg.findDynamicFlagArgument("--nopadname");
            cmdarg.assert(!File.Exists(inAfs), $"'{inAfs}' does not exist.");

            if (!Directory.Exists(outFolder))
                Directory.CreateDirectory(outFolder);

            AFSFile dataFile = null;
            FileStream fdata = null;

            try
            {
                fdata = File.OpenRead(inAfs);
            }
            catch (Exception E)
            {
                cmdarg.assert($"Failed to open '{inAfs}': '{E.Message}'");
            }

            try
            {
                dataFile = AFSFile.load(new BinaryReader(fdata));
            }
            catch (Exception E) { cmdarg.assert($"Failed decode AFS '{inAfs}': '{E.Message}'"); }

            for (int i = 0; i < dataFile.sectionCount; i++)
                if (dataFile.sections[i].descriptor != null && !force_numeric_output)
                    File.WriteAllBytes($"{outFolder}/{dataFile.sections[i].descriptor.name}", dataFile.sections[i].data);
                else if (!filenumbers)
                    File.WriteAllBytes($"{outFolder}/{i:D4}.dat", dataFile.sections[i].data);
                else
                    File.WriteAllBytes($"{outFolder}/{i}.dat", dataFile.sections[i].data);
        }
    }
}