using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Utils.Extensions;

namespace MafiaResearch.Mafia2.Navigation.NavData
{
    public enum AiPathFindingObjectType
    {
        AiGroup = 1, 
        AiWordPart = 2, 
        AiWayPoint = 3, 
        AiCover = 4, 
        AiAnimPoint = 5, // unused in game
        AiUserArea = 6,
        AiPathObject = 7,
        AiSideWalk = 8,
        AiCrossing = 9, 
        AiStation = 10,
        AiHidingPlace = 11
        // note: there is 12th one, serializer deserializes it as an ActionPoint (base class for 8, 9 and 10),
        // but factory method does not know how to create object for the 12 type, also this type not found in game files
    }

    /// <summary>
    /// Based on game::ai::navpoints::C_AIObjectCreator
    /// </summary>
    public class AiPathFindingObjectFactory
    {
        public AiPathFindingObject ByType(AiPathFindingObjectType objectType)
        {
            switch (objectType)
            {
                case AiPathFindingObjectType.AiGroup:
                    return new AiGroup(this);
                case AiPathFindingObjectType.AiWordPart:
                    return new AiWorldPart(this);
                case AiPathFindingObjectType.AiWayPoint:
                    return new AiWayPoint();
                case AiPathFindingObjectType.AiCover:
                    return new AiCoverNavPoint();
                case AiPathFindingObjectType.AiUserArea:
                    return new AiNavArea();
                case AiPathFindingObjectType.AiPathObject:
                    return new AiPathObjectNavPoint();
                case AiPathFindingObjectType.AiSideWalk:
                    return new AiSideWalkNavPoint();
                case AiPathFindingObjectType.AiCrossing:
                    return new AiCrossingNavPoint();
                case AiPathFindingObjectType.AiStation: 
                    return new AiStationNavPoint();
                case AiPathFindingObjectType.AiHidingPlace:
                    return new AiHidingPlaceNavPoint();
                case AiPathFindingObjectType.AiAnimPoint:
                    // maybe unused in game (not found in files yet)
                    throw new NotImplementedException($"Type {objectType} is not supported/implemented");
                default:
                    throw new ArgumentOutOfRangeException(nameof(objectType), objectType, null);
            }
        }
    }

    public interface IAiPathFindingObjectSerializable
    {
        void Read(Stream input, bool isBigEndian = false);
        void Write(Stream output, bool isBigEndian = false);
    }

    public abstract class AiPathFindingObject: IAiPathFindingObjectSerializable
    {

        public AiPathFindingObjectType ObjectType { get; }

        protected AiPathFindingObject(AiPathFindingObjectType objectType)
        {
            ObjectType = objectType;
        }

        public abstract void Read(Stream input, bool isBigEndian = false);

        public virtual void Write(Stream output, bool isBigEndian = false)
        {
            output.Write((ushort)ObjectType, isBigEndian);
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_AIFakeGroup
    /// </summary>
    public class AiGroup : AiPathFindingObject
    {
        private readonly AiPathFindingObjectFactory _factory;

        public byte Unkn3 { get; set; }
        public List<AiPathFindingObject> Items { get; } = new List<AiPathFindingObject>();

        public AiGroup(AiPathFindingObjectFactory factory) : base(AiPathFindingObjectType.AiGroup)
        {
            _factory = factory;
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            Unkn3 = input.ReadByte8();

            var objectCount = input.ReadUInt32(isBigEndian);
            for (int i = 0; i < objectCount; i++)
            {
                var objectType = input.ReadUInt16(isBigEndian);
                var aiObject = _factory.ByType((AiPathFindingObjectType)objectType);
                aiObject.Read(input, isBigEndian);
                Items.Add(aiObject);
            }
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);

            output.Write((char)Unkn3);
            output.Write((uint)Items.Count, isBigEndian);
            for (var i = 0; i < Items.Count; i++)
            {
                Items[i].Write(output, isBigEndian);
            }
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_WorldPart
    /// </summary>
    public class AiWorldPart : AiPathFindingObject
    {
        private readonly AiPathFindingObjectFactory _factory;

        public string Name { get; set; }
        public uint WorldId { get; set; }
        public string BrnwString { get; set; }
        public string BrnbString { get; set; }
        public string BlobType { get; set; }
        public List<AiPathFindingObject> Items { get; } = new List<AiPathFindingObject>();

        public AiWorldPart(AiPathFindingObjectFactory factory) : base(AiPathFindingObjectType.AiWordPart)
        {
            _factory = factory;
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            Name = input.ReadString16(isBigEndian);
            WorldId = input.ReadUInt32(isBigEndian);
            BrnwString = input.ReadString16(isBigEndian);
            BrnbString = input.ReadString16(isBigEndian);
            BlobType = input.ReadString16(isBigEndian);

            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }

            var objectCount = input.ReadUInt32(isBigEndian);
            for (int i = 0; i < objectCount; i++)
            {
                var objectType = input.ReadUInt16(isBigEndian);
                var aiObject = _factory.ByType((AiPathFindingObjectType) objectType);
                aiObject.Read(input, isBigEndian);
                Items.Add(aiObject);
            }
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);

            output.WriteString16(Name, isBigEndian);
            output.Write(WorldId, isBigEndian);
            output.WriteString16(BrnwString, isBigEndian);
            output.WriteString16(BrnbString, isBigEndian);
            output.WriteString16(BlobType, isBigEndian);
            output.Write((char)1);
            output.Write((uint)Items.Count, isBigEndian);
            foreach (var item in Items)
            {
                item.Write(output, isBigEndian);
            }
        }
    }

    /// <summary>
    /// Guessed type: game::ai::navpoints::C_NavPointHandle
    /// </summary>
    public struct AiNavPointHandle
    {
        private uint _value;

        public ushort PointId
        {
            get => (ushort) (_value & 0xFFFF);
            set => _value = (_value & 0xFFFF0000) | value;
        }

        // In the original code the 3 hi bits also mean something (there are checks like: "value < 0xD0000000"), 
        // but it looks like in the game files they are always zeroed // TODO scan all files
        public ushort WorldId
        {
            get => (ushort)((_value >> 16) & 0xFFFF);
            set => _value = (_value & 0xFFFF) | ((uint)value << 16);
        }

        private AiNavPointHandle(uint value)
        {
            _value = value;
        }

        public static implicit operator uint(AiNavPointHandle ph) => ph._value;
        public static implicit operator AiNavPointHandle(uint v) => new AiNavPointHandle(v);
    }

    /// <summary>
    /// game::ai::navpoints::C_WayPoint
    /// base: game::ai::navpoints::C_NavPoint
    /// </summary>
    public class AiWayPoint : AiPathFindingObject
    {
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public AiNavPointHandle Id { get; set; }
        public AiNavPointHandle LeftId { get; set; }
        public AiNavPointHandle RightId { get; set; }

        protected AiWayPoint(AiPathFindingObjectType objectType) : base(objectType)
        {
        }

        public AiWayPoint() : this(AiPathFindingObjectType.AiWayPoint)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }
            Position = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Direction = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Id = input.ReadUInt32(isBigEndian);
            LeftId = input.ReadUInt32(isBigEndian);
            RightId = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write((char)1);
            output.Write(Position.X, isBigEndian);
            output.Write(Position.Y, isBigEndian);
            output.Write(Position.Z, isBigEndian);
            output.Write(Direction.X, isBigEndian);
            output.Write(Direction.Y, isBigEndian);
            output.Write(Direction.Z, isBigEndian);
            output.Write(Id, isBigEndian);
            output.Write(LeftId, isBigEndian);
            output.Write(RightId, isBigEndian);
        }
    }

    [Flags]
    public enum CoverFlags1 : byte
    {
        Flag0 = 1,
        Flag1 = 2,
        Flag2 = 4,
        Flag3 = 8,
        Flag4 = 16,  // ?
        Flag5 = 32,
//        Flag6 = 64, // cover type bit
//        Flag7 = 128 // cover type bit
    }

    [Flags]
    public enum CoverFlags2 : byte
    {
        Flag0 = 1,
        Flag1 = 2,
        Flag2 = 4,
        Flag3 = 8,
        LeftCorner = 16,  // see C_CoverCheck::IsLeftCorner
        RightCorner = 32, // see C_CoverCheck::IsRightCorner

        Flag6 = 64,  // todo remove? (never set in files)
        Flag7 = 128  // todo remove? (never set in files)
    }

    public enum CoverType : byte
    {
        Low = 1,  // player is crouching - the difference with Half is imperceptible yet (at least visual)
        Half = 2, // player is crouching, see C_CoverSlotPtr::IsHalfCover ( )
        Full = 3  // player is standing, see C_CoverSlotPtr::IsFullCover ()
    }

    /// <summary>
    /// game::ai::navpoints::C_CoverNavPoint
    /// base: game::ai::navpoints::C_NavPoint
    /// </summary>
    public class AiCoverNavPoint : AiWayPoint
    {
        public Vector3 Direction2 { get; set; } // always == Direction
        public float Width { get; set; }
        public CoverFlags1 Unkn9 { get; set; } // default value 0xA0
        public CoverFlags2 Unkn10 { get; set; } // default value 0xC
        public List<int> Unkn12 { get; set; } = new List<int>();
        // Guessed type: ue::ai::nav::C_TopologyNodeID
        public uint NearestGraphVertex { get; set; }

        public CoverType CoverType => (CoverType) ((byte) Unkn9 >> 6); // 2 hi bits


        public AiCoverNavPoint() : base(AiPathFindingObjectType.AiCover)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            base.Read(input, isBigEndian);
            
            Direction2 = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Width = input.ReadSingle(isBigEndian);
            Unkn9 = (CoverFlags1) input.ReadByte8();
            Unkn10 = (CoverFlags2) input.ReadByte8();
            var unkn12Count = input.ReadUInt16(isBigEndian);
            for (int i = 0; i < unkn12Count; i++)
            {
                Unkn12.Add(input.ReadInt32(isBigEndian));
            }
            NearestGraphVertex = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write(Direction2.X, isBigEndian);
            output.Write(Direction2.Y, isBigEndian);
            output.Write(Direction2.Z, isBigEndian);
            output.Write(Width, isBigEndian);
            output.Write((char)Unkn9);
            output.Write((char)Unkn10);
            output.Write((ushort)Unkn12.Count, isBigEndian);
            for (var i = 0; i < Unkn12.Count; i++)
            {
                output.Write(Unkn12[i], isBigEndian);
            }
            output.Write(NearestGraphVertex, isBigEndian);
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_NavArea
    /// </summary>
    public class AiNavArea : AiPathFindingObject
    {
        public string AreaName { get; set; }
        public List<Vector3> ContourPoints { get; set; } = new List<Vector3>(); // count 4..22

        public AiNavArea() : base(AiPathFindingObjectType.AiUserArea)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }

            AreaName = input.ReadString16(isBigEndian);

            var pointsCount = input.ReadUInt16(isBigEndian);
            for (int i = 0; i < pointsCount; i++)
            {
                ContourPoints.Add(new Vector3(
                    input.ReadSingle(isBigEndian), 
                    input.ReadSingle(isBigEndian), 
                    input.ReadSingle(isBigEndian)
                ));
            }
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write((char)1);
            output.WriteString16(AreaName, isBigEndian);
            output.Write((ushort)ContourPoints.Count, isBigEndian);
            for (var i = 0; i < ContourPoints.Count; i++)
            {
                output.Write(ContourPoints[i].X, isBigEndian);
                output.Write(ContourPoints[i].Y, isBigEndian);
                output.Write(ContourPoints[i].Z, isBigEndian);
            }
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_PathObjectNavPoint
    /// </summary>
    public class AiPathObjectNavPoint : AiPathFindingObject
    {
        public byte Unkn1 { get; set; } // 1, 2, 4 or 14
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 HalfExtents { get; set; }
        public AiNavPointHandle Id { get; set; }

        public AiPathObjectNavPoint() : base(AiPathFindingObjectType.AiPathObject)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }

            Unkn1 = input.ReadByte8();
            Position = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Direction = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            HalfExtents = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Id = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write((char)1);
            output.Write((char)Unkn1);
            output.Write(Position.X, isBigEndian);
            output.Write(Position.Y, isBigEndian);
            output.Write(Position.Z, isBigEndian);
            output.Write(Direction.X, isBigEndian);
            output.Write(Direction.Y, isBigEndian);
            output.Write(Direction.Z, isBigEndian);
            output.Write(HalfExtents.X, isBigEndian);
            output.Write(HalfExtents.Y, isBigEndian);
            output.Write(HalfExtents.Z, isBigEndian);
            output.Write(Id, isBigEndian);
        }
    }

    public abstract class AiActionPoint : AiPathFindingObject
    {
        public AiNavPointHandle Id { get; set; }
        public Vector3 Position { get; set; }
        // looks like a radius (but sometimes = 0.0, which plays not in favor of this assumption)
        public float Unkn3 { get; set; } //
        // for crossings always = 0.0,
        // for sidewalks almost always = 1.0 (only 26 points throughout the game = 0.0)
        // the working theory is that it is a some kind of "ped spawn probability", but this could all be wrong
        public float Unkn4 { get; set; } // 0.0 or 1.0
        public List<AiNavPointHandle> ConnectedPoints { get; } = new List<AiNavPointHandle>();

        protected AiActionPoint(AiPathFindingObjectType objectType) : base(objectType)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }

            Id = input.ReadUInt32(isBigEndian);
            Position = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Unkn3 = input.ReadSingle(isBigEndian);
            Unkn4 = input.ReadSingle(isBigEndian);

            var connectedPointsCount = input.ReadUInt16(isBigEndian);
            for (int i = 0; i < connectedPointsCount; i++)
            {
                ConnectedPoints.Add(input.ReadUInt32(isBigEndian));
            }
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);

            output.Write((char)1);
            output.Write(Id, isBigEndian);
            output.Write(Position.X, isBigEndian);
            output.Write(Position.Y, isBigEndian);
            output.Write(Position.Z, isBigEndian);
            output.Write(Unkn3, isBigEndian);
            output.Write(Unkn4, isBigEndian);
            output.Write((ushort)ConnectedPoints.Count, isBigEndian);
            foreach (var pointId in ConnectedPoints)
            {
                output.Write(pointId, isBigEndian);
            }
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_SideWalkNavPoint
    /// </summary>
    public class AiSideWalkNavPoint : AiActionPoint
    {
        public uint Unkn7 { get; set; }

        public AiSideWalkNavPoint() : base(AiPathFindingObjectType.AiSideWalk) {}

        public override void Read(Stream input, bool isBigEndian = false)
        {
            base.Read(input, isBigEndian);
            Unkn7 = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write(Unkn7, isBigEndian);
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_CrossingNavPoint
    /// </summary>
    public class AiCrossingNavPoint : AiActionPoint
    {
        public AiCrossingNavPoint() : base(AiPathFindingObjectType.AiCrossing) {}
    }

    /// <summary>
    /// game::ai::navpoints::C_StationNavPoint
    /// </summary>
    public class AiStationNavPoint : AiActionPoint
    {
        public uint Unkn7 { get; set; }

        public AiStationNavPoint() : base(AiPathFindingObjectType.AiStation)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            base.Read(input, isBigEndian);
            Unkn7 = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write(Unkn7, isBigEndian);
        }
    }

    /// <summary>
    /// game::ai::navpoints::C_HidingPlaceNavPoint
    /// </summary>
    public class AiHidingPlaceNavPoint : AiPathFindingObject
    {
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 HalfExtents { get; set; }
        public AiNavPointHandle Id { get; set; }

        public AiHidingPlaceNavPoint() : base(AiPathFindingObjectType.AiHidingPlace)
        {
        }

        public override void Read(Stream input, bool isBigEndian = false)
        {
            byte alwaysOneValue = input.ReadByte8();
            if (alwaysOneValue != 1)
            {
                throw new IOException($"Unexpected value encountered: {alwaysOneValue}, pos: {input.Position}");
            }
            Position = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Direction = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            HalfExtents = new Vector3(input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian), input.ReadSingle(isBigEndian));
            Id = input.ReadUInt32(isBigEndian);
        }

        public override void Write(Stream output, bool isBigEndian = false)
        {
            base.Write(output, isBigEndian);
            output.Write((char)1);
            output.Write(Position.X, isBigEndian);
            output.Write(Position.Y, isBigEndian);
            output.Write(Position.Z, isBigEndian);
            output.Write(Direction.X, isBigEndian);
            output.Write(Direction.Y, isBigEndian);
            output.Write(Direction.Z, isBigEndian);
            output.Write(HalfExtents.X, isBigEndian);
            output.Write(HalfExtents.Y, isBigEndian);
            output.Write(HalfExtents.Z, isBigEndian);
            output.Write(Id, isBigEndian);
        }
    }

    public class NavAiWorldData : IAiPathFindingObjectSerializable
    {
        private const uint StartFileMagic = 0x3ED;
        private const uint EndFileMagic = 0x1214F001;

        public uint WorldId { get; set; }
        public AiWorldPart WorldPart { get; set; }
        public string SourceFilename { get; set; }

        public NavAiWorldData(Stream input, bool isBigEndian = false)
        {
            Read(input, isBigEndian);
        }

        public void Read(Stream input, bool isBigEndian = false)
        {
            uint totalSize = input.ReadUInt32(isBigEndian);
            uint magic = input.ReadUInt32(isBigEndian);
            
            if (magic != StartFileMagic)
            {
                throw new IOException($"Unexpected magic value encountered: {magic}, pos: {input.Position}");
            }

            WorldId = input.ReadUInt32(isBigEndian);

            uint itemsCount = input.ReadUInt32(isBigEndian);
            // in theory there can be more than one root objects,
            // but in game files there is always only one object of type WorldPart 
            if (itemsCount != 1)
            {
                throw new IOException($"Unexpected root objects count value encountered: {itemsCount}, pos: {input.Position}");
            }
            ushort rootObjectType = input.ReadUInt16(isBigEndian);
            if (rootObjectType != (ushort)AiPathFindingObjectType.AiWordPart)
            {
                throw new IOException($"Unexpected root object type value encountered: {rootObjectType}, pos: {input.Position}");
            }

            WorldPart = new AiWorldPart(new AiPathFindingObjectFactory());
            WorldPart.Read(input, isBigEndian);

            SourceFilename = input.ReadString();
            uint sourceFilenameLength = input.ReadUInt32(isBigEndian);
            uint endFileMagic = input.ReadUInt32(isBigEndian);
            if (endFileMagic != EndFileMagic)
            {
                throw new IOException($"Unexpected end file magic value encountered: {endFileMagic}, pos: {input.Position}");
            }
        }

        public void Write(Stream output, bool isBigEndian = false)
        {
            output.Write(0, isBigEndian); // placeholder for total side

            output.Write(StartFileMagic, isBigEndian);
            output.Write(WorldId, isBigEndian);
            output.Write(1, isBigEndian); // count, always 1
            WorldPart.Write(output, isBigEndian);
            output.WriteString(SourceFilename);
            output.Write((uint)(SourceFilename.Length + 1), isBigEndian);
            output.Write(EndFileMagic, isBigEndian);

            output.Seek(0, SeekOrigin.Begin);
            output.Write((uint)(output.Length - 4), isBigEndian);
        }
    }
}
