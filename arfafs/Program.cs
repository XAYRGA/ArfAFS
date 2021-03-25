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

        static void runTest(string filename)
        {

            Console.WriteLine($"Unit test for {filename}");
            var inFile = filename;
            cmdarg.assert(!File.Exists(inFile), $"Halt: {inFile} doesn't exist.");


            var fileStream = File.OpenRead(inFile);
            var fileReader = new BinaryReader(fileStream);
            var afsData = AFSFile.load(fileReader);

            var scene = afsData.sections[0];


            var wl = new JSRSceneBin(scene);
            wl.parse();




            /*
             * 
             * 
             * 
            // Scene Parse
            var sceneSectData = afsData.sections[0].data;
            var sceneSectReader = new MemoryStream(sceneSectData);
            var sceneSect = new BinaryReader(sceneSectReader);

            var scenePointers = new Stack<uint>();
            uint b = 0;
            uint largestPointer = 0;
            uint smallestPointer = 0xFFFFFFFF; 

            while (((b = sceneSect.ReadUInt32()) & 0x8C000000) == 0x8C000000)// bruh 
            {
                if (!((b & 0x8C000000) == 0x8C000000))
                    break;
                if (b > 0x8CFFFFFF)
                    break;
                scenePointers.Push(b);
                if (b > largestPointer)
                    largestPointer = b;
                if (b < smallestPointer)
                    smallestPointer = b;

            }
            // UNWRAP STACK HERE 
            var sections = new uint[scenePointers.Count];
            for (int i = scenePointers.Count - 1; i >= 0; i--)
                sections[i] = scenePointers.Pop();
            //

            Console.WriteLine();
            Console.WriteLine($"Offset high = 0x{largestPointer:X}, low = {smallestPointer:X}");
            Console.WriteLine($"Virtual allocation address = 0x8c800000");
            */

        }

        static void Main(string[] args)
        {
            /*
            args = new string[] {
                "demo04.afs",
                "out1",
            };
            cmdarg.cmdargs = args;

            var inFile = cmdarg.assertArg(0, "AFS File");
            var outFolder = cmdarg.assertArg(1, "Out Folder");
            */

            for (int i=2; i < 3; i++)
            {
                runTest($"DEMO0{i}.afs");
            }

            Console.ReadLine();


        }
    }
}
