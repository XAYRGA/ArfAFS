using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace arfafs
{
    public class JSRDemoBin
    {
        private BinaryReader reader;
        public uint[] rootPointers; 

        public JSRDemoBin(AFSSection sect)
        {
            reader = new BinaryReader(new MemoryStream(sect.data));
        }

        public JSRDemoBin(byte[] binary)
        {
            reader = new BinaryReader(new MemoryStream(binary));
        }

        private uint virtual2Physical(uint ptr)
        {
            return ptr - 0x8C800000;
        }

        private uint[] readPointerList(bool physical = false)
        {
            var scenePointers = new Stack<uint>();
            uint b = 0;

            while ( reader.BaseStream.Position < reader.BaseStream.Length && (((b = reader.ReadUInt32()) & 0xFF000000) == 0x8C000000) )// 
                if (b > 0x8CFFFFFF)
                    break;
                else
                    scenePointers.Push(b);

            var sections = new uint[scenePointers.Count];
            for (int i = scenePointers.Count - 1; i >= 0; i--)
                sections[i] = physical ? virtual2Physical(scenePointers.Pop()) : scenePointers.Pop();

            return sections;
        }

        public  uint[] readUInt32Array(int count, bool physical = false)
        {
            var b = new uint[count];
            for (int i = 0; i < count; i++)
                b[i] = physical ? virtual2Physical(reader.ReadUInt32()) : reader.ReadUInt32();
            return b;
        }


        private void readS3Section(uint address)
        {
            Console.WriteLine("==== TYPE 3 SECTION ====");
            reader.BaseStream.Position = address;
            var subPointers = readPointerList(true);

            for (int b = 0; b < subPointers.Length; b += 2)
            {

                var infoPointer = subPointers[b];
                var clusterPointer = subPointers[b + 1];

                reader.BaseStream.Position = infoPointer;
                var endOffsetOfPreviousList = reader.ReadUInt32();
                var pointerListCount = reader.ReadUInt32();
                reader.BaseStream.Position = clusterPointer;
                var nextPointerSet = readUInt32Array((int)pointerListCount, true);
                Console.WriteLine($"{infoPointer + 0x800:X} next, {virtual2Physical(endOffsetOfPreviousList) + 0x800:X} prevEnd, {pointerListCount:X} count, {clusterPointer + 0x800:X} listStart");

                for (int i = 0; i < nextPointerSet.Length; i++)
                    Console.WriteLine($"\tSection Offset {nextPointerSet[i] + 0x800:X}");
            }
        }

        public void parse()
        {
            var scenePointers = readPointerList(true);

            var data1 = scenePointers[0];
            var data2 = scenePointers[1];
            var data3 = scenePointers[2];


            Console.WriteLine($"XPTR 0x{data1:X}");
            Console.WriteLine($"YPTR 0x{data2:X}");
            reader.BaseStream.Position = data1;
           
            var subPointers = readPointerList(true);
            for (int i=0; i < subPointers.Length; i++)
            {
                reader.BaseStream.Position = subPointers[i];

                var clusterPointers = readPointerList(true);
                Console.WriteLine($"\tsptr 0x{subPointers[i]:X} == {subPointers[i] + 0x800:X}");
                for (int b=0; b < clusterPointers.Length; b++)
                {
                    reader.BaseStream.Position = clusterPointers[b];
                    Console.WriteLine($"\t\tcptr 0x{clusterPointers[b]:X} == {clusterPointers[b] + 0x800:X}");
                    var idkpointers = readPointerList(true);
                    for (int k=0; k < idkpointers.Length; k++)
                    {

                        Console.WriteLine($"\t\t\tcptr 0x{idkpointers[k]:X} == {idkpointers[k] + 0x800:X}");
                    }

                }
            }

            Console.WriteLine($"ZPTR 0x{data3}");
            

          

        }
    }
}
