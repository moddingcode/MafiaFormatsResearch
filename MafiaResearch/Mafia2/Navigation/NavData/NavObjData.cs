using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Utils.Extensions;

namespace MafiaResearch.Mafia2.Navigation.NavData
{
    // Guessed type: ue::ai::nav::C_TopologyNode
    public class GraphVertex
    {
        private uint _idAndOverlapFlag;
        /// <summary>
        /// Unique ID of the vertex throughout all the SubGraphs (i.e. all the NAV_OBJ_DATA files) 
        /// </summary>
        public uint UniqueId { get { return _idAndOverlapFlag & 0x7FFFFFFF; } }
        /// <summary>
        /// Indicates is the vertex shared between the neighbor SubGraphs.
        /// Literally it means that exact the same vertex (ID and pos) is present in a neighbor SubGraph.
        /// </summary>
        public bool IsOnOverlap { get { return Convert.ToBoolean((_idAndOverlapFlag >> 31) & 1); } }
        /// <summary>
        /// Position in the world space coords
        /// </summary>
        public Vector3 Position { get; set; } = Vector3.Zero;
        public float Unkn3 { get; set; } // always 0
        public float Unkn4 { get; set; } // present only in the 0th vertex of the graph and is always 0 otherwise
        /// <summary>
        /// Points to the first outgoing GraphEdge (index is increased by 1).
        /// 0 means there is no outgoing edges. 
        /// </summary>
        public uint OutEdgesStartIndex { get; set; }
        public ushort Unkn5 { get; set; }
        public ushort Unkn6 { get; set; }
        // maybe 2 words but the second one is always 0
        public uint Unkn7 { get; set; }
        // a kind of flags, values 0, 1, 2, 32
        // probably GraphVertexTerrainType
        // 0  = 
        // 1  = generic (all AI walkable area, but not pavements and roads)
        // 2  = pavements
        // 32 = roads
        public ushort Unkn8 { get; set; } = 1;
        // has non-zero value when Unkn8 == 2, otherwise is always 0
        public ushort Unkn9 { get; set; }

        public GraphVertex() { }
        public GraphVertex(Stream input, bool isBigEndian = false)
        {
            Read(input, isBigEndian);
        }

        public void Read(Stream input, bool isBigEndian = false)
        {
            _idAndOverlapFlag = input.ReadUInt32(isBigEndian);
            Position = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Unkn3 = input.ReadSingle(isBigEndian);
            Unkn4 = input.ReadSingle(isBigEndian);
            OutEdgesStartIndex = input.ReadUInt32(isBigEndian);
            Unkn5 = input.ReadUInt16(isBigEndian);
            Unkn6 = input.ReadUInt16(isBigEndian);
            Unkn7 = input.ReadUInt32(isBigEndian);
            Unkn8 = input.ReadUInt16(isBigEndian);
            Unkn9 = input.ReadUInt16(isBigEndian);
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            output.Write(_idAndOverlapFlag, isBigEndian);
            output.Write(Position.X, isBigEndian);
            output.Write(Position.Y, isBigEndian);
            output.Write(Position.Z, isBigEndian);
            output.Write(Unkn3, isBigEndian);
            output.Write(Unkn4, isBigEndian);
            output.Write(OutEdgesStartIndex, isBigEndian);
            output.Write(Unkn5, isBigEndian);
            output.Write(Unkn6, isBigEndian);
            output.Write(Unkn7, isBigEndian);
            output.Write(Unkn8, isBigEndian);
            output.Write(Unkn9, isBigEndian);
        }
    }

    public class GraphEdge
    {
        private uint _costAndFlag;

        public uint UnknFlagAlways1 // always 1
        {
            get => (_costAndFlag >> 31) & 1;
            set => _costAndFlag = (value << 31) | (_costAndFlag & 0x7FFFFFFF);
        }
        /// <summary>
        /// g(n)
        /// </summary>
        public uint Cost
        {
            get => _costAndFlag & 0x7FFFFFFF;
            set => _costAndFlag = (value & 0x7FFFFFFF) | (_costAndFlag & 0x80000000);
        }
        public uint StartVertexIdx { get; set; }
        public uint EndVertexIdx { get; set; }

        public GraphEdge() { }
        public GraphEdge(Stream input, bool isBigEndian = false)
        {
            Read(input, isBigEndian);
        }

        public void Read(Stream input, bool isBigEndian = false)
        {
            _costAndFlag = input.ReadUInt32(isBigEndian);
            StartVertexIdx = input.ReadUInt32(isBigEndian);
            EndVertexIdx = input.ReadUInt32(isBigEndian);
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            output.Write(_costAndFlag, isBigEndian);
            output.Write(StartVertexIdx, isBigEndian);
            output.Write(EndVertexIdx, isBigEndian);
        }
    }

    // nowadays this class is called GraphCell (judging by the documentation) 
    public class SubGraph
    {
        public uint Version { get; set; }
        public uint Unkn5 { get; set; }
        public uint Unkn6 { get; set; }
        public uint Unkn7 { get; set; }

        public List<GraphVertex> Vertices { get; } = new List<GraphVertex>();
        public List<GraphEdge> Edges { get; } = new List<GraphEdge>();

        public void Read(Stream input, bool isBigEndian = false)
        {
            Version = input.ReadUInt32(isBigEndian);
            Unkn5 = input.ReadUInt32(isBigEndian);
            Unkn6 = input.ReadUInt32(isBigEndian);
            Unkn7 = input.ReadUInt32(isBigEndian);
            uint vertexCount = input.ReadUInt32(isBigEndian);
            uint edgeCount = input.ReadUInt32(isBigEndian);

            for (var i = 0; i < vertexCount; i++)
            {
                Vertices.Add(new GraphVertex(input, isBigEndian));
            }

            for (var i = 0; i < edgeCount; i++)
            {
                Edges.Add(new GraphEdge(input, isBigEndian));
            }
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            output.Write(Version, isBigEndian);
            output.Write(Unkn5, isBigEndian);
            output.Write(Unkn6, isBigEndian);
            output.Write(Unkn7, isBigEndian);
            output.Write((uint)Vertices.Count, isBigEndian);
            output.Write((uint)Edges.Count, isBigEndian);

            for (var i = 0; i < Vertices.Count; i++)
            {
                Vertices[i].Write(output, isBigEndian);
            }

            for (var i = 0; i < Edges.Count; i++)
            {
                Edges[i].Write(output, isBigEndian);
            }
        }

        public void CalculateEdgeCosts()
        {
            for (var i = 0; i < Edges.Count; i++)
            {
                var startPos = GetVertexPos((int) Edges[i].StartVertexIdx);
                var endPos = GetVertexPos((int) Edges[i].EndVertexIdx);
                Edges[i].Cost = (uint)(Vector3.Distance(startPos, endPos) * 16);
            }
        }

        private Vector3 GetVertexPos(int index)
        {
            return Vertices[index].Position;
        }
    }

    public class NavObjData
    {
        public uint Magic { get; set; }
        // -temp-
        // TODO there are some strange bytes at the end, most likely it's just a garbage
        public byte[] _nameBytes = { };
        // -end temp-
        public string Name { get { return Encoding.ASCII.GetString(_nameBytes); } }
        
        public SubGraph Graph { get; set; } = new SubGraph();
        public AiMesh RuntimeMesh { get; set; } = new AiMesh();
        public string GenerationSourceName { get; set; }

        public NavObjData() { }
        public NavObjData(Stream input, bool isBigEndian = false)
        {
            Read(input, isBigEndian);
        }

        public void Read(Stream input, bool isBigEndian = false)
        {
            uint totalSize = input.ReadUInt32(isBigEndian);
            Magic = input.ReadUInt32(isBigEndian);
            int nameLength = input.ReadInt32(isBigEndian);
            _nameBytes = input.ReadBytes(nameLength);
            Graph.Read(input, false); // always little endian even on X360/PS3
            RuntimeMesh.Read(input, false); // always little endian even on X360/PS3

            input.Seek(-8, SeekOrigin.End);
            int sourceNameLength = input.ReadInt32(isBigEndian);
            uint endFileMagic = input.ReadUInt32(isBigEndian);
            if (endFileMagic != 0x1213F001)
            {
                throw new IOException("Unexpected magic value");
            }
            input.Seek(-8 - sourceNameLength, SeekOrigin.End);
            GenerationSourceName = input.ReadStringBuffer(sourceNameLength);
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            output.Write((uint)0, isBigEndian); // total size placeholder
            output.Write(Magic, isBigEndian);
            output.Write(_nameBytes.Length, isBigEndian);
            output.Write(_nameBytes);
            Graph.Write(output, false); // always little endian even on X360/PS3
            RuntimeMesh.Write(output, false); // always little endian even on X360/PS3

            output.Seek(0, SeekOrigin.End);
            output.Write((uint)0, isBigEndian);
            output.WriteString(GenerationSourceName);
            output.Write((uint)(GenerationSourceName.Length + 1), isBigEndian);
            output.Write((uint)0x1213F001, isBigEndian);

            output.Seek(0, SeekOrigin.Begin);
            output.Write((uint)(output.Length - 4), isBigEndian); // total size
        }
    }
}
