using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace arfafs
{
    public class JSRSceneBin
    {
        private BinaryReader reader;
        private const uint VIRTUAL_ALLOCATION_ADDRESS = 0x8CB00000; // Supposed to be offset for section

        public JSRSceneBin(AFSSection sect)
        {
            reader = new BinaryReader(new MemoryStream(sect.data));
        }

        public JSRSceneBin(byte[] binary)
        {
            reader = new BinaryReader(new MemoryStream(binary));
        }

        private uint virtual2Physical(uint ptr)
        {
            return ptr - VIRTUAL_ALLOCATION_ADDRESS;
        }

        private uint[] readPointerList(bool physical = false)
        {
            var scenePointers = new Stack<uint>();
            uint b = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length && (((b = reader.ReadUInt32()) & 0xFF000000) == 0x8C000000))// 
                if (b > 0x8CFFFFFF)
                    break;
                else
                    scenePointers.Push(b);

            var sections = new uint[scenePointers.Count];
            for (int i = scenePointers.Count - 1; i >= 0; i--)
                sections[i] = physical ? virtual2Physical(scenePointers.Pop()) : scenePointers.Pop();

            return sections;
        }

        public uint[] readUInt32Array(int count, bool physical = false)
        {
            var b = new uint[count];
            for (int i = 0; i < count; i++)
                b[i] = physical ? virtual2Physical(reader.ReadUInt32()) : reader.ReadUInt32();
            return b;
        }


        public bool heuristicCheckForNode(BinaryReader rdr)
        {
           
            var oldPos = rdr.BaseStream.Position;
            rdr.BaseStream.Seek(-4, SeekOrigin.Current); // Seek back to node flags if valid.
            try
            {
                //Console.WriteLine($"START {rdr.BaseStream.Position + 0x80000:X} , RETURN {oldPos:X}");
                //Console.ReadLine();
                rdr.BaseStream.Seek(-4, SeekOrigin.Current);

                //Console.WriteLine($"FLAG {rdr.BaseStream.Position + 0x80000:X}");
                var flags = rdr.ReadUInt32();
            
                if (flags > 0x8C000000) // not a valid node because the flags are above what should be the flag range. 
                    goto restorePositionReturn;

                var next = rdr.ReadUInt32();
                var nodePtr = virtual2Physical(next); // drop 0x8XXX , no check needed because this should be called OUT of a check. 
                //Console.WriteLine($"Check phys point {next:X} {next & 0xFF000000:X}");
                if ((next & 0xFF000000) != 0x8C000000)
                    goto restorePositionReturn;  // Node doesn't have a pointer after the physical pointer.           

                for (int i=0; i < 6; i++)
                    if ((rdr.ReadUInt32() & 0xFF000000) == 0x8C000000)
                        goto restorePositionReturn;

                // Console.WriteLine("Check length");
                if (nodePtr > rdr.BaseStream.Length)
                    goto restorePositionReturn; // not a valid pointer because it's not within our address space. 

                rdr.BaseStream.Position = nodePtr;

                var vlistPtr = rdr.ReadUInt32();
               // Console.WriteLine($"Check vlist {vlistPtr:X}");
                if ((vlistPtr & 0xFF000000) != 0x8C000000) // not a valid model because it doesn't have a pointer to a vlist
                    goto restorePositionReturn;

                //Console.WriteLine("check plist");
                var plistPointer = rdr.ReadUInt32();
                if (plistPointer != 0 && (plistPointer & 0xFF000000) != 0x8c000000) // not a valid model because the vlist pointer isn't an address or zero
                    goto restorePositionReturn;

                var bounding = readUInt32Array(4, false);
                for (int i = 0; i < bounding.Length; i++)
                    if ((bounding[i] & 0xFF000000) == 0x8C000000)
                        goto restorePositionReturn;

                rdr.BaseStream.Position = oldPos;
                return true; // Probably a model / node 

            restorePositionReturn:
                rdr.BaseStream.Position = oldPos;
                return false;
            } catch
            {

                rdr.BaseStream.Position = oldPos;
                return false;
            }
        }

        public uint countChildren(uint nodeAddress, int level = 0)
        {
            var oldPos = reader.BaseStream.Position;
            reader.BaseStream.Position = nodeAddress + 0x2C;
            var sibling = reader.ReadUInt32();
            var childAddress = reader.ReadUInt32();

            var childrenCount = 0u;
            if ((sibling & 0xFF000000) == 0x8C000000)
                childrenCount += countChildren(virtual2Physical(sibling), level);
            else if (sibling != 0)
                goto notAModel;

            level++;
            if ((childAddress & 0xFF000000) == 0x8C000000)
            {
                childrenCount++;
                childrenCount += countChildren(virtual2Physical(childAddress), level);
            }
            else if (childAddress != 0)
                goto notAModel;
            else
                return 0;

            return childrenCount;

            notAModel:
                Console.WriteLine($"0x{nodeAddress + 0x80000:X} Not a model!");
                return 0xFF000000;

        }

        public void removeChildNodes(uint nodeAddress, Dictionary<uint, uint> nodeGrp)
        {
            reader.BaseStream.Position = nodeAddress + 0x2C;
            var sibling = reader.ReadUInt32();
            var childAddress = reader.ReadUInt32();


            if ((sibling & 0xFF000000) == 0x8C000000)
            {
                nodeGrp.Remove(sibling);
                removeChildNodes(virtual2Physical(sibling), nodeGrp);
            }
            else if (sibling != 0)
                goto notAModel;

            if ((childAddress & 0xFF000000) == 0x8C000000)
            {
                if (nodeGrp.ContainsKey(childAddress))
                    nodeGrp.Remove(childAddress);
                removeChildNodes(virtual2Physical(childAddress), nodeGrp);
            }
            else if (childAddress != 0)
                goto notAModel;

            return;
            notAModel:
            nodeGrp.Remove(nodeAddress); // Vertical remove. This isn't a model so it can't be a root node. 
        }

        public uint[] findNodes()
        {

            var nodes = new uint[0];
            var nodeStk = new Queue<uint>(); 

            while (true)
            {
                if (reader.BaseStream.Length - reader.BaseStream.Position <= 8)
                    break;
                var currentPosition = (uint)reader.BaseStream.Position;
                var readValue = reader.ReadUInt32();
                if ((readValue & 0xFF000000) == 0x8C000000)
                    if (heuristicCheckForNode(reader))
                        nodeStk.Enqueue(currentPosition - 4);
            }



            Dictionary<uint, uint> nodeGraph = new Dictionary<uint, uint>();

            while (nodeStk.Count > 0)
            {
                var nodeAddress = nodeStk.Dequeue();

                var childC = countChildren(nodeAddress);
                if (childC == 0xFF000000)
                    continue;
                nodeGraph.Add(nodeAddress, childC);
                Console.WriteLine($"0x{nodeAddress + 0x80000:X} has {childC} children.");
            }

            var newDictionary = nodeGraph.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);

            foreach (KeyValuePair<uint, uint> kvp in newDictionary)
            {
                removeChildNodes(kvp.Key, nodeGraph);
            }
            Console.WriteLine("=== Orphan Nodes ===");
            foreach (KeyValuePair<uint, uint> kvp in nodeGraph)
            {
                Console.WriteLine($"Orphan node: {kvp.Key:X}, {kvp.Value:X}");
            }

            foreach (KeyValuePair<uint, uint> kvp in nodeGraph)
            {
                if (kvp.Value > 0)
                {
                    Console.WriteLine($"Orphan node > 0: {kvp.Key:X}, {kvp.Value:X}");
                }
            }





            Console.ReadLine();
            return new uint[0];
        }
    }
}
