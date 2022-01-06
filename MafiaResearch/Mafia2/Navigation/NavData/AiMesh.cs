using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Utils.Extensions;

namespace MafiaResearch.Mafia2.Navigation.NavData
{
    public class AiMeshObj8
    {
        public uint AdjacentCellPosX { get; set; }
        public uint AdjacentCellPosY { get; set; }
        public uint Unkn2 { get; set; }
        // edges of current navMesh that are neighbors of the adjacent navCell (in another navMesh)
        public readonly List<MeshBoundaryEdge> RelatedBoundaryEdges = new List<MeshBoundaryEdge>();

        public void Read(Stream input, AiMeshReadContext readContext, long ownerObjOffset, bool isBigEndian = false)
        {
            AdjacentCellPosX = input.ReadUInt32(isBigEndian);
            AdjacentCellPosY = input.ReadUInt32(isBigEndian);
            Unkn2 = input.ReadUInt32(isBigEndian);

            uint boundaryEdgesCount = input.ReadUInt32(isBigEndian);
            for (var i = 0; i < boundaryEdgesCount; i++)
            {
                uint edgeOffset = input.ReadUInt32(isBigEndian);
                var edge = readContext.FindMeshBoundaryEdgeByOffset(edgeOffset);
                RelatedBoundaryEdges.Add(edge);
            }
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, bool isBigEndian = false)
        {
            output.Write(AdjacentCellPosX, isBigEndian);
            output.Write(AdjacentCellPosY, isBigEndian);
            output.Write(Unkn2, isBigEndian);

            output.Write((uint)RelatedBoundaryEdges.Count, isBigEndian);
            for (var i = 0; i < RelatedBoundaryEdges.Count; i++)
            {
                uint serializedEdgeOffset = writeContext.FindMeshBoundaryEdgeOffset(RelatedBoundaryEdges[i]);
                output.Write(serializedEdgeOffset, isBigEndian);
            }
        }
    }

    public class AiMeshObj7
    {
        public uint AdjacentMeshId { get; set; }
        public readonly List<AiMeshObj8> Unkn1 = new List<AiMeshObj8>();

        public void Read(Stream input, AiMeshReadContext readContext, long ownerObjOffset, bool isBigEndian = false)
        {
            AdjacentMeshId = input.ReadUInt32(isBigEndian);
            uint unkn1Count = input.ReadUInt32(isBigEndian);
            uint unkn1Offset = input.ReadUInt32(isBigEndian);
            long objEndOffset = input.Position;

            if (unkn1Offset != 0xFFFFFFFF)
            {
                input.Seek(ownerObjOffset + unkn1Offset, SeekOrigin.Begin);

                for (var i = 0; i < unkn1Count; i++)
                {
                    AiMeshObj8 newAiMeshObj8 = new AiMeshObj8();
                    uint newAiMeshObj8Offset = input.ReadUInt32(isBigEndian);
                    long newAiMeshObj8RetOffset = input.Position;
                    input.Seek(ownerObjOffset + newAiMeshObj8Offset, SeekOrigin.Begin);
                    newAiMeshObj8.Read(input, readContext, ownerObjOffset, isBigEndian);
                    input.Seek(newAiMeshObj8RetOffset, SeekOrigin.Begin);
                    Unkn1.Add(newAiMeshObj8);
                }

                input.Seek(objEndOffset, SeekOrigin.Begin);
            }
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, long ownerObjOffset, long objStartOffset, bool isBigEndian = false)
        {
            output.Write(AdjacentMeshId, isBigEndian);
            output.Write((uint)Unkn1.Count, isBigEndian);
            output.Write((uint)(objStartOffset - ownerObjOffset), isBigEndian);

            output.Seek(objStartOffset, SeekOrigin.Begin);
            output.Write(new byte[4 * Unkn1.Count]); // refs placeholders
            long currentObjStartOffset = output.Position;
            for (var i = 0; i < Unkn1.Count; i++)
            {
                output.Seek(objStartOffset + i * 4, SeekOrigin.Begin);
                output.Write((uint) (currentObjStartOffset - ownerObjOffset), isBigEndian);
                output.Seek(currentObjStartOffset, SeekOrigin.Begin);
                Unkn1[i].Write(output, writeContext, isBigEndian);
                currentObjStartOffset = output.Position;
            }
        }
    }

    public class AiMeshObj6
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        // some direction/orientation
        public Vector3 Unkn1 { get; set; }

        public void Read(Stream input, long endOffset, bool isBigEndian = false)
        {
            while (input.Position != endOffset)
            {
                Vertices.Add(new Vector3(
                    input.ReadSingle(isBigEndian),
                    input.ReadSingle(isBigEndian),
                    input.ReadSingle(isBigEndian)
                ));
            }
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            foreach (var point in Vertices)
            {
                output.Write(point.X, isBigEndian);
                output.Write(point.Y, isBigEndian);
                output.Write(point.Z, isBigEndian);
            }
        }
    }

    // TODO rename to Contour/Obstacle Contour ?
    public class AiMeshObj5
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();

        public void Read(Stream input, long endOffset, bool isBigEndian = false)
        {
            while (input.Position != endOffset)
            {
                Vertices.Add(new Vector3(
                    input.ReadSingle(isBigEndian),
                    input.ReadSingle(isBigEndian),
                    input.ReadSingle(isBigEndian)
                ));
            }
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            foreach (var point in Vertices)
            {
                output.Write(point.X, isBigEndian);
                output.Write(point.Y, isBigEndian);
                output.Write(point.Z, isBigEndian);
            }
        }
    }

    public class MeshBoundaryEdge
    {
        public Vector3 StartVertex { get; set; }
        public Vector3 EndVertex { get; set; }
        public int Unkn2 { get; set; } // always -1
        public int Unkn3 { get; set; } // always -1
        // some direction/orientation
        public Vector3 Unkn4 { get; set; }

        public void Read(Stream input, AiMeshReadContext readContext, long meshObjOffset, bool isBigEndian = false)
        {
            long thisOffset = input.Position;
            readContext.RegisterMeshBoundaryEdge((uint) (thisOffset - meshObjOffset), this);

            StartVertex = new Vector3(
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian)
            );
            EndVertex = new Vector3(
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian)
            );
            Unkn2 = input.ReadInt32(isBigEndian);
            Unkn3 = input.ReadInt32(isBigEndian);
            Unkn4 = new Vector3(
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian)
            );
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, long meshObjOffset, bool isBigEndian = false)
        {
            writeContext.RegisterSerializedMeshBoundary(this, (uint) (output.Position - meshObjOffset));
            output.Write(StartVertex.X, isBigEndian);
            output.Write(StartVertex.Y, isBigEndian);
            output.Write(StartVertex.Z, isBigEndian);
            output.Write(EndVertex.X, isBigEndian);
            output.Write(EndVertex.Y, isBigEndian);
            output.Write(EndVertex.Z, isBigEndian);
            output.Write(Unkn2, isBigEndian);
            output.Write(Unkn3, isBigEndian);
            output.Write(Unkn4.X, isBigEndian);
            output.Write(Unkn4.Y, isBigEndian);
            output.Write(Unkn4.Z, isBigEndian);
        }
    }

    public class CellBoundaryHalfEdge
    {
        public Vector3 StartVertex { get; set; }
        public Vector3 EndVertex { get; set; }

        public readonly WeakReference<AiMeshNavFloor> AdjacentFloor = new WeakReference<AiMeshNavFloor>(null);

        public void Read(Stream input, AiMeshReadContext readContext, bool isBigEndian = false)
        {
            StartVertex = new Vector3(
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian)
            );
            EndVertex = new Vector3(
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian),
                input.ReadSingle(isBigEndian)
            );

            uint adjacentFloorOffset = input.ReadUInt32(isBigEndian);
            uint adjacentFloorSize = input.ReadUInt32(isBigEndian);

            readContext.RegisterCellBoundaryEdgeForAdjacentFloorDelayedFixup(this, adjacentFloorOffset);
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, bool isBigEndian = false)
        {
            output.Write(StartVertex.X, isBigEndian);
            output.Write(StartVertex.Y, isBigEndian);
            output.Write(StartVertex.Z, isBigEndian);
            output.Write(EndVertex.X, isBigEndian);
            output.Write(EndVertex.Y, isBigEndian);
            output.Write(EndVertex.Z, isBigEndian);
            uint adjacentFloorOffset = (uint) output.Position;
            output.Write(0xFFFFFFFF, isBigEndian); // adjacentFloor offset placeholder
            output.Write(0xFFFFFFFF, isBigEndian); // adjacentFloor size placeholder

            AiMeshNavFloor floor;
            if (AdjacentFloor.TryGetTarget(out floor))
            {
                writeContext.RegisterFloorForDelayedFixupInCellBoundaryEdges(floor, adjacentFloorOffset);
            }
            else
            {
                throw new IOException("Unexpected empty AdjacentFloor reference during the serialization");
            }
        }
    }

    public class AiMeshNavFloor
    {
        public uint MeshId { get; set; } // always == AiMesh.MeshId
        public uint CellPosX { get; set; }
        public uint CellPosY { get; set; }
        public uint FloorIndex { get; set; }
        public float EntityRadius { get; set; }
        public float Unkn5 { get; set; } // always == AiMesh.Unkn6, ? m_rasterPrecision
        public float AltitudeMax { get; set; }
        public float AltitudeMin { get; set; }
        // cell boundary half-edges (are lying on the grid)
        public readonly List<CellBoundaryHalfEdge> CellBoundaryEdges = new List<CellBoundaryHalfEdge>();
        // ? mesh boundary edges (on places where two meshes are adjacent to each other (on overlaps))
        public readonly List<MeshBoundaryEdge> MeshBoundaryEdges = new List<MeshBoundaryEdge>();
        public readonly List<AiMeshObj5> Unkn11 = new List<AiMeshObj5>();
        // ? walkable area edges/contours 
        public readonly List<AiMeshObj5> Unkn12 = new List<AiMeshObj5>();
        public readonly List<AiMeshObj6> Unkn13 = new List<AiMeshObj6>();

        public void Read(Stream input, AiMeshReadContext readContext, long ownerObjOffset, bool isBigEndian = false)
        {
            long thisOffset = input.Position;

            MeshId = input.ReadUInt32(isBigEndian);
            CellPosX = input.ReadUInt32(isBigEndian);
            CellPosY = input.ReadUInt32(isBigEndian);
            FloorIndex = input.ReadUInt32(isBigEndian);
            
            EntityRadius = input.ReadSingle(isBigEndian);
            Unkn5 = input.ReadSingle(isBigEndian);
            AltitudeMax = input.ReadSingle(isBigEndian);
            AltitudeMin = input.ReadSingle(isBigEndian);

            uint unkn8Count = input.ReadUInt32(isBigEndian);
            uint unkn8Offset = input.ReadUInt32(isBigEndian);
            if (unkn8Count != 0 || unkn8Offset != 0xFFFFFFFF)
            {
                throw new IOException("Unexpected pointer value encountered");
            }

            uint cellBoundaryEdgesCount = input.ReadUInt32(isBigEndian);
            uint cellBoundaryEdgesOffset = input.ReadUInt32(isBigEndian);
            uint meshBoundaryEdgesCount = input.ReadUInt32(isBigEndian);
            uint meshBoundaryEdgesOffset = input.ReadUInt32(isBigEndian);
            uint unkn11Count = input.ReadUInt32(isBigEndian);
            uint unkn11Offset = input.ReadUInt32(isBigEndian);
            uint unkn12Count = input.ReadUInt32(isBigEndian);
            uint unkn12Offset = input.ReadUInt32(isBigEndian);
            uint unkn13Count = input.ReadUInt32(isBigEndian);
            uint unkn13Offset = input.ReadUInt32(isBigEndian);

            if (cellBoundaryEdgesOffset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + cellBoundaryEdgesOffset, SeekOrigin.Begin);

                for (int i = 0; i < cellBoundaryEdgesCount; i++)
                {
                    CellBoundaryHalfEdge newCellBoundaryHalfEdge = new CellBoundaryHalfEdge();
                    newCellBoundaryHalfEdge.Read(input, readContext, isBigEndian);
                    CellBoundaryEdges.Add(newCellBoundaryHalfEdge);
                }
            }
            if (meshBoundaryEdgesOffset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + meshBoundaryEdgesOffset, SeekOrigin.Begin);

                for (int i = 0; i < meshBoundaryEdgesCount; i++)
                {
                    MeshBoundaryEdge newMeshBoundaryEdge = new MeshBoundaryEdge();
                    newMeshBoundaryEdge.Read(input, readContext, ownerObjOffset, isBigEndian);

                    MeshBoundaryEdges.Add(newMeshBoundaryEdge);
                }
            }
            if (unkn11Offset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + unkn11Offset, SeekOrigin.Begin);

                for (int i = 0; i < unkn11Count; i++)
                {
                    AiMeshObj5 newAiMeshObj5 = new AiMeshObj5();

                    uint startOffset = input.ReadUInt32(isBigEndian);
                    long newAiMeshObj5RetOffset = input.Position;
                    uint endOffset = input.ReadUInt32(isBigEndian);

                    input.Seek(thisOffset + startOffset, SeekOrigin.Begin);
                    newAiMeshObj5.Read(input, thisOffset + endOffset, isBigEndian);
                    input.Seek(newAiMeshObj5RetOffset, SeekOrigin.Begin);

                    Unkn11.Add(newAiMeshObj5);
                }
            }
            if (unkn12Offset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + unkn12Offset, SeekOrigin.Begin);

                for (int i = 0; i < unkn12Count; i++)
                {
                    AiMeshObj5 newAiMeshObj5 = new AiMeshObj5();

                    uint startOffset = input.ReadUInt32(isBigEndian);
                    long newAiMeshObj5RetOffset = input.Position;
                    uint endOffset = input.ReadUInt32(isBigEndian);

                    input.Seek(thisOffset + startOffset, SeekOrigin.Begin);
                    newAiMeshObj5.Read(input, thisOffset + endOffset, isBigEndian);
                    input.Seek(newAiMeshObj5RetOffset, SeekOrigin.Begin);

                    Unkn12.Add(newAiMeshObj5);
                }
            }
            if (unkn13Offset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + unkn13Offset, SeekOrigin.Begin);

                for (int i = 0; i < unkn13Count; i++)
                {
                    AiMeshObj6 newAiMeshObj6 = new AiMeshObj6();

                    uint startOffset = input.ReadUInt32(isBigEndian);
                    newAiMeshObj6.Unkn1 = new Vector3(
                        input.ReadSingle(isBigEndian),
                        input.ReadSingle(isBigEndian),
                        input.ReadSingle(isBigEndian)
                    );
                    long newAiMeshObj6RetOffset = input.Position;
                    uint endOffset = input.ReadUInt32(isBigEndian);

                    input.Seek(thisOffset + startOffset, SeekOrigin.Begin);
                    newAiMeshObj6.Read(input, thisOffset + endOffset, isBigEndian);
                    input.Seek(newAiMeshObj6RetOffset, SeekOrigin.Begin);

                    Unkn13.Add(newAiMeshObj6);
                }
            }
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, long ownerObjOffset, bool isBigEndian = false)
        {
            long thisOffset = output.Position;

            output.Write(MeshId, isBigEndian);
            output.Write(CellPosX, isBigEndian);
            output.Write(CellPosY, isBigEndian);
            output.Write(FloorIndex, isBigEndian);
            output.Write(EntityRadius, isBigEndian);
            output.Write(Unkn5, isBigEndian);
            output.Write(AltitudeMax, isBigEndian);
            output.Write(AltitudeMin, isBigEndian);

            output.Write((uint)0, isBigEndian);
            output.Write(0xFFFFFFFF, isBigEndian);

            output.Write((uint)CellBoundaryEdges.Count, isBigEndian);
            long offsetOfCellBoundaryEdgesOffset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            output.Write((uint)MeshBoundaryEdges.Count, isBigEndian);
            long offsetOfMeshBoundaryEdgesOffset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            output.Write((uint)Unkn11.Count, isBigEndian);
            long offsetOfUnkn11Offset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            output.Write((uint)Unkn12.Count, isBigEndian);
            long offsetOfUnkn12Offset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            output.Write((uint)Unkn13.Count, isBigEndian);
            long offsetOfUnkn13Offset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            if (CellBoundaryEdges.Count > 0)
            {
                long cellBoundaryEdgesOffset = output.Position;
                output.Seek(offsetOfCellBoundaryEdgesOffset, SeekOrigin.Begin);
                output.Write((uint) (cellBoundaryEdgesOffset - thisOffset), isBigEndian);
                output.Seek(cellBoundaryEdgesOffset, SeekOrigin.Begin);

                for (var i = 0; i < CellBoundaryEdges.Count; i++)
                {
                    CellBoundaryEdges[i].Write(output, writeContext, isBigEndian);
                }
            }
            if (MeshBoundaryEdges.Count > 0)
            {
                long meshBoundaryEdgesOffset = output.Position;
                output.Seek(offsetOfMeshBoundaryEdgesOffset, SeekOrigin.Begin);
                output.Write((uint)(meshBoundaryEdgesOffset - thisOffset), isBigEndian);
                output.Seek(meshBoundaryEdgesOffset, SeekOrigin.Begin);

                for (var i = 0; i < MeshBoundaryEdges.Count; i++)
                {
                    MeshBoundaryEdges[i].Write(output, writeContext, ownerObjOffset, isBigEndian);
                }
            }
            if (Unkn11.Count > 0)
            {
                long unkn11Offset = output.Position;
                output.Seek(offsetOfUnkn11Offset, SeekOrigin.Begin);
                output.Write((uint)(unkn11Offset - thisOffset), isBigEndian);
                output.Seek(unkn11Offset, SeekOrigin.Begin);

                long unkn11PtrSize = 4 * (Unkn11.Count + 1);
                output.Write(new byte[unkn11PtrSize]); // placeholder for pointers

                for (var i = 0; i < Unkn11.Count; i++)
                {
                    long currentUnkn11ElementStartOffset = output.Position;
                    Unkn11[i].Write(output, isBigEndian);
                    long currentUnkn11ElementEndOffset = output.Position;

                    output.Seek(unkn11Offset + i * 4, SeekOrigin.Begin);
                    output.Write((uint)(currentUnkn11ElementStartOffset - thisOffset), isBigEndian);
                    output.Write((uint)(currentUnkn11ElementEndOffset - thisOffset), isBigEndian);
                    output.Seek(currentUnkn11ElementEndOffset, SeekOrigin.Begin);
                }
            }
            if (Unkn12.Count > 0)
            {
                long unkn12Offset = output.Position;
                output.Seek(offsetOfUnkn12Offset, SeekOrigin.Begin);
                output.Write((uint)(unkn12Offset - thisOffset), isBigEndian);
                output.Seek(unkn12Offset, SeekOrigin.Begin);

                long unkn12PtrSize = 4 * (Unkn12.Count + 1);
                output.Write(new byte[unkn12PtrSize]); // placeholder for pointers

                for (var i = 0; i < Unkn12.Count; i++)
                {
                    long currentUnkn12ElementStartOffset = output.Position;
                    Unkn12[i].Write(output, isBigEndian);
                    long currentUnkn12ElementEndOffset = output.Position;

                    output.Seek(unkn12Offset + i * 4, SeekOrigin.Begin);
                    output.Write((uint)(currentUnkn12ElementStartOffset - thisOffset), isBigEndian);
                    output.Write((uint)(currentUnkn12ElementEndOffset - thisOffset), isBigEndian);
                    output.Seek(currentUnkn12ElementEndOffset, SeekOrigin.Begin);
                }
            }
            if (Unkn13.Count > 0)
            {
                long unkn13Offset = output.Position;
                output.Seek(offsetOfUnkn13Offset, SeekOrigin.Begin);
                output.Write((uint)(unkn13Offset - thisOffset), isBigEndian);
                output.Seek(unkn13Offset, SeekOrigin.Begin);

                long unkn13PtrSize = 16 * (Unkn13.Count + 1);
                output.Write(new byte[unkn13PtrSize]); // placeholder for pointers

                for (var i = 0; i < Unkn13.Count; i++)
                {
                    long currentUnkn13ElementStartOffset = output.Position;
                    Unkn13[i].Write(output, isBigEndian);
                    long currentUnkn13ElementEndOffset = output.Position;

                    output.Seek(unkn13Offset + i * 16, SeekOrigin.Begin);
                    output.Write((uint)(currentUnkn13ElementStartOffset - thisOffset), isBigEndian);
                    output.Write(Unkn13[i].Unkn1.X, isBigEndian);
                    output.Write(Unkn13[i].Unkn1.Y, isBigEndian);
                    output.Write(Unkn13[i].Unkn1.Z, isBigEndian);
                    output.Write((uint)(currentUnkn13ElementEndOffset - thisOffset), isBigEndian);
                    output.Seek(currentUnkn13ElementEndOffset, SeekOrigin.Begin);
                }
            }

        }
    }

    public class AiMeshNavFloorPtr
    {
        public readonly AiMeshNavFloor Floor = new AiMeshNavFloor();
        public float AltitudeMin { get; set; }
        public float AltitudeMax { get; set; }

        public void Read(Stream input, AiMeshReadContext readContext, long ownerObjOffset,
            bool isBigEndian = false)
        {
            uint floorOffset = input.ReadUInt32(isBigEndian);
            AltitudeMin = input.ReadSingle(isBigEndian);
            AltitudeMax = input.ReadSingle(isBigEndian);

            long retOffset = input.Position;
            input.Seek(ownerObjOffset + floorOffset, SeekOrigin.Begin);

            Floor.Read(input, readContext, ownerObjOffset, isBigEndian);

            input.Seek(retOffset, SeekOrigin.Begin);

            readContext.RegisterNavFloor(floorOffset, Floor);
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, long ownerObjOffset, long floorStartOffset, bool isBigEndian = false)
        {
            output.Write((uint)(floorStartOffset - ownerObjOffset), isBigEndian);
            output.Write(AltitudeMin, isBigEndian);
            output.Write(AltitudeMax, isBigEndian);

            long currentFloorPtrEndOffset = output.Position;
            output.Seek(floorStartOffset, SeekOrigin.Begin);
            Floor.Write(output, writeContext, ownerObjOffset, isBigEndian);
            long currentFloorEndOffset = output.Position;

            output.Seek(currentFloorPtrEndOffset, SeekOrigin.Begin);
            output.Write((uint)(currentFloorEndOffset - ownerObjOffset), isBigEndian);
            output.Seek(currentFloorEndOffset, SeekOrigin.Begin);

            writeContext.RegisterSerializedFloor(
                Floor,
                (uint) (floorStartOffset - ownerObjOffset),
                (uint) (currentFloorEndOffset - floorStartOffset)
            );
        }
    }

    public class AiMeshNavCell
    {
        public readonly List<AiMeshNavFloorPtr> Floors = new List<AiMeshNavFloorPtr>();

        public void Read(Stream input, AiMeshReadContext readContext, long ownerObjOffset, bool isBigEndian = false)
        {
            uint count = input.ReadUInt32(isBigEndian);

            for (int i = 0; i < count; i++)
            {
                AiMeshNavFloorPtr newAiMeshNavFloorPtr = new AiMeshNavFloorPtr();
                newAiMeshNavFloorPtr.Read(input, readContext, ownerObjOffset, isBigEndian);

                Floors.Add(newAiMeshNavFloorPtr);
            }
        }

        public void Write(Stream output, AiMeshWriteContext writeContext, long ownerObjOffset, bool isBigEndian = false)
        {
            output.Write((uint)Floors.Count, isBigEndian);

            long floorRefsStartOffset = output.Position;
            long floorRefsSize = 12 * Floors.Count + 4;
            long floorRefsEndOffset = floorRefsStartOffset + floorRefsSize;

            output.Write(new byte[floorRefsSize]); // placeholders for floor refs
            output.Seek(floorRefsStartOffset, SeekOrigin.Begin);

            long currentFloorStartOffset = floorRefsEndOffset;
            for (var i = 0; i < Floors.Count; i++)
            {
                output.Seek(floorRefsStartOffset + 12 * i, SeekOrigin.Begin);
                Floors[i].Write(output, writeContext, ownerObjOffset, currentFloorStartOffset, isBigEndian);
                currentFloorStartOffset = output.Position;
            }
        }
    }

    public class AiMeshReadContext
    {
        private readonly Dictionary<uint, AiMeshNavFloor> _offsetToNavFloor = new Dictionary<uint, AiMeshNavFloor>();
        private readonly Dictionary<CellBoundaryHalfEdge, uint> _cellBoundaryHalfEdgeToAdjacentFloorOffset = new Dictionary<CellBoundaryHalfEdge, uint>();
        private readonly Dictionary<uint, MeshBoundaryEdge> _meshBoundaryEdges = new Dictionary<uint, MeshBoundaryEdge>();

        public void RegisterNavFloor(uint offset, AiMeshNavFloor floor)
        {
            _offsetToNavFloor.Add(offset, floor);
        }

        public void RegisterCellBoundaryEdgeForAdjacentFloorDelayedFixup(CellBoundaryHalfEdge cellBoundaryHalfEdge, uint floorOffset)
        {
            _cellBoundaryHalfEdgeToAdjacentFloorOffset.Add(cellBoundaryHalfEdge, floorOffset);
        }

        public void RegisterMeshBoundaryEdge(uint offset, MeshBoundaryEdge edge)
        {
            _meshBoundaryEdges.Add(offset, edge);
        }

        public void FixupAdjacentFloorReferencesInCellBoundaryEdges()
        {
            foreach (var edgeToFloorOffsetPair in _cellBoundaryHalfEdgeToAdjacentFloorOffset)
            {
                var edge = edgeToFloorOffsetPair.Key;
                var floor = _offsetToNavFloor[edgeToFloorOffsetPair.Value];
                edge.AdjacentFloor.SetTarget(floor);
            }
        }

        public MeshBoundaryEdge FindMeshBoundaryEdgeByOffset(uint edgeOffset)
        {
            return _meshBoundaryEdges[edgeOffset];
        }
    }

    public class AiMeshWriteContext
    {
        private readonly Dictionary<AiMeshNavFloor, Tuple<uint, uint>> _floorToOffset = new Dictionary<AiMeshNavFloor, Tuple<uint, uint>>();
        private readonly Dictionary<AiMeshNavFloor, List<uint>> _floorsReferencesToFixup = new Dictionary<AiMeshNavFloor, List<uint>>();
        private readonly Dictionary<MeshBoundaryEdge, uint> _serializedMeshBoundaryEdges = new Dictionary<MeshBoundaryEdge, uint>();

        public void RegisterSerializedFloor(AiMeshNavFloor floor, uint serializedOffset, uint serializedSize)
        {
            _floorToOffset.Add(floor, new Tuple<uint, uint>(serializedOffset, serializedSize));
        }

        public void RegisterFloorForDelayedFixupInCellBoundaryEdges(AiMeshNavFloor floor, uint serializedFloorOffset)
        {
            if (!_floorsReferencesToFixup.ContainsKey(floor))
            {
                _floorsReferencesToFixup.Add(floor, new List<uint>());
            }

            _floorsReferencesToFixup[floor].Add(serializedFloorOffset);
        }

        public void RegisterSerializedMeshBoundary(MeshBoundaryEdge edge, uint serializedOffset)
        {
            _serializedMeshBoundaryEdges.Add(edge, serializedOffset);
        }

        public void FixupAdjacentFloorReferencesInCellBoundaryEdges(Stream output, bool isBigEndian = false)
        {
            foreach (var floorToOffsetsPair in _floorsReferencesToFixup)
            {
                var floor = floorToOffsetsPair.Key;
                var offsetAndSize = _floorToOffset[floor];

                foreach (var offsetToFixup in floorToOffsetsPair.Value)
                {
                    output.Seek(offsetToFixup, SeekOrigin.Begin);
                    output.Write(offsetAndSize.Item1, isBigEndian);
                    output.Write(offsetAndSize.Item2, isBigEndian);
                }
            }
        }

        public uint FindMeshBoundaryEdgeOffset(MeshBoundaryEdge edge)
        {
            return _serializedMeshBoundaryEdges[edge];
        }
    }

    public class AiMesh
    {
        public char[ /*20*/] TypeStr { get; set; } = { };
        public uint Version { get; set; }
        public uint Unkn3 { get; set; } // always 0
        public uint MeshId { get; set; }
        public float CellSizeInv { get; set; } // = 1.0f / CellSize
        public float Unkn6 { get; set; } // raster precision?
        public float BbMinX { get; set; }
        public float BbMaxX { get; set; }
        public float BbMinY { get; set; }
        public float BbMaxY { get; set; }
        public uint Rows { get; set; }
        public uint Cols { get; set; }
        public float EntityRadius { get; set; }

        public readonly List<AiMeshNavCell> Cells = new List<AiMeshNavCell>();
        public readonly List<AiMeshObj7> Unkn15 = new List<AiMeshObj7>();

        public AiMesh() { }
        
        public void Read(Stream input, bool isBigEndian = false)
        {
            var readContext = new AiMeshReadContext();

            long thisOffset = input.Position;

            TypeStr = input.ReadChars(20);
            Version = input.ReadUInt32(isBigEndian);
            Unkn3 = input.ReadUInt32(isBigEndian);
            MeshId = input.ReadUInt32(isBigEndian);
            CellSizeInv = input.ReadSingle(isBigEndian);
            Unkn6 = input.ReadSingle(isBigEndian);
            BbMinX = input.ReadSingle(isBigEndian);
            BbMaxX = input.ReadSingle(isBigEndian);
            BbMinY = input.ReadSingle(isBigEndian);
            BbMaxY = input.ReadSingle(isBigEndian);
            Rows = input.ReadUInt32(isBigEndian);
            Cols = input.ReadUInt32(isBigEndian);
            EntityRadius = input.ReadSingle(isBigEndian);

            uint cellsOffset = input.ReadUInt32(isBigEndian);
            long cellsRetOffset = input.Position;
            input.Seek(thisOffset + cellsOffset, SeekOrigin.Begin);
            for (int i = 0; i < Rows * Cols; i++)
            {
                AiMeshNavCell newAiMeshNavCell = new AiMeshNavCell();

                uint startOffset = input.ReadUInt32(isBigEndian);
                long newCellRetOffset = input.Position;
                uint endOffset = input.ReadUInt32(isBigEndian);
                input.Seek(thisOffset + startOffset, SeekOrigin.Begin);
                newAiMeshNavCell.Read(input, readContext, thisOffset, isBigEndian);
                input.Seek(newCellRetOffset, SeekOrigin.Begin);
                Cells.Add(newAiMeshNavCell);
            }
            input.Seek(cellsRetOffset, SeekOrigin.Begin);

            uint unkn15Count = input.ReadUInt32(isBigEndian);
            uint unkn15Offset = input.ReadUInt32(isBigEndian);
            long unkn15RetOffset = input.Position;
            if (unkn15Offset != 0xFFFFFFFF)
            {
                input.Seek(thisOffset + unkn15Offset, SeekOrigin.Begin);
                for (int i = 0; i < unkn15Count; i++)
                {
                    AiMeshObj7 newAiMeshObj7 = new AiMeshObj7();
                    newAiMeshObj7.Read(input, readContext, thisOffset, isBigEndian);
                    Unkn15.Add(newAiMeshObj7);
                }

                input.Seek(unkn15RetOffset, SeekOrigin.Begin);
            }

            readContext.FixupAdjacentFloorReferencesInCellBoundaryEdges();
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            var writeContext = new AiMeshWriteContext();

            long thisOffset = output.Position;

            output.Write(TypeStr);
            output.Write(Version, isBigEndian);
            output.Write(Unkn3, isBigEndian);
            output.Write(MeshId, isBigEndian);
            output.Write(CellSizeInv, isBigEndian);
            output.Write(Unkn6, isBigEndian);
            output.Write(BbMinX, isBigEndian);
            output.Write(BbMaxX, isBigEndian);
            output.Write(BbMinY, isBigEndian);
            output.Write(BbMaxY, isBigEndian);
            output.Write(Rows, isBigEndian);
            output.Write(Cols, isBigEndian);
            output.Write(EntityRadius, isBigEndian);

            long offsetOfCellsRefsStartOffset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian); // cells offset placeholder
            
            output.Write((uint)Unkn15.Count, isBigEndian);
            long offsetOfUnkn15StartOffset = output.Position;
            output.Write(0xFFFFFFFF, isBigEndian);

            long cellsRefsStartOffset = output.Position;
            output.Seek(offsetOfCellsRefsStartOffset, SeekOrigin.Begin);
            output.Write((uint)(cellsRefsStartOffset - thisOffset), isBigEndian);
            output.Seek(cellsRefsStartOffset, SeekOrigin.Begin);

            long cellsRefsEndOffset = cellsRefsStartOffset + 4 * (Cells.Count + 1);
            output.Seek(cellsRefsEndOffset, SeekOrigin.Begin);

            for (var i = 0; i < Cells.Count; i++)
            {
                long currentCellStartOffset = output.Position;
                Cells[i].Write(output, writeContext, thisOffset, isBigEndian);
                long currentCellEndOffset = output.Position;

                output.Seek(cellsRefsStartOffset + 4 * i, SeekOrigin.Begin);
                output.Write((uint)(currentCellStartOffset - thisOffset), isBigEndian);
                output.Write((uint)(currentCellEndOffset - thisOffset), isBigEndian);

                output.Seek(currentCellEndOffset, SeekOrigin.Begin);
            }
            writeContext.FixupAdjacentFloorReferencesInCellBoundaryEdges(output, isBigEndian);

            output.Seek(0, SeekOrigin.End);
            if (Cells.Last().Floors.Count != 0)
            {
                output.Write((uint)0, isBigEndian);
            }

            if (Unkn15.Count > 0)
            {
                long unkn15StartOffset = output.Position;
                output.Seek(offsetOfUnkn15StartOffset, SeekOrigin.Begin);
                output.Write((uint) (unkn15StartOffset - thisOffset), isBigEndian);
                output.Seek(unkn15StartOffset, SeekOrigin.Begin);
                output.Write(new byte[Unkn15.Count * 12]); // placeholders for refs
                long currentStartOffset = output.Position;
                for (var i = 0; i < Unkn15.Count; i++)
                {
                    output.Seek(unkn15StartOffset + i * 12, SeekOrigin.Begin);
                    Unkn15[i].Write(output, writeContext, thisOffset, currentStartOffset, isBigEndian);
                    currentStartOffset = output.Position;
                }
            }

            // AiMesh total size
            output.Write((uint)(output.Position - thisOffset), isBigEndian);
        }
    }
}
