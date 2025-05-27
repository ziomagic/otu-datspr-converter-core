using OpenTibiaUnity.Core.Metaflags;
using System;
using System.Collections.Generic;

namespace OpenTibiaUnity.Core.Assets
{
    public class Light
    {
        public ushort intensity = 0;
        public ushort color = 0;
    }

    public sealed class MarketData
    {
        public string name;
        public ushort category;
        public ushort restrictLevel;
        public ushort restrictProfession;
        public ushort showAs;
        public ushort tradeAs;
    }

    public sealed class Vector2Int
    {
        public ushort x = 0;
        public ushort y = 0;

        public Vector2Int(ushort _x, ushort _y) { x = _x; y = _y; }
    }

    public sealed class ThingType
    {
        public ThingCategory Category { get; set; }
        public ushort ID { get; set; }
        public Dictionary<byte, object> Attributes { get; private set; } = new Dictionary<byte, object>();
        public Dictionary<FrameGroupType, FrameGroup> FrameGroups { get; private set; } = new Dictionary<FrameGroupType, FrameGroup>();

        public bool HasAttribute(byte attr)
        {
            return Attributes.TryGetValue(attr, out object _);
        }

        public void Serialize(IO.BinaryStream binaryWriter, int fromVersion, int newVersion)
        {
            Serialize854(binaryWriter);

            // the whole idea is how to animate outfits correctly in different versions
            if (Category != ThingCategory.Creature)
            {
                if (ID == 424 && Category == ThingCategory.Item)
                {
                    Console.WriteLine("Phases: " + FrameGroups[0].Phases);
                }

                FrameGroups[0].Serialize(binaryWriter, 0, FrameGroups[0].Phases);
                return;
            }

            FrameGroups[0].Serialize(binaryWriter, 0, FrameGroups[0].Phases);
        }

        public void Unserialize(IO.BinaryStream binaryReader, int clientVersion)
        {
            int lastAttr = 0, previousAttr = 0, attr = 0;
            bool done;
            try
            {
                done = Unserialize854(binaryReader, ref lastAttr, ref previousAttr, ref attr);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("Parsing Failed ({0}). (attr: 0x{1:X2}, previous: 0x{2:X2}, last: 0x{3:X2})", e, attr, previousAttr, lastAttr));
            }

            if (!done)
                throw new Exception("Couldn't parse thing [category: " + Category + ", ID: " + ID + "].");

            bool hasFrameGroups = Category == ThingCategory.Creature && clientVersion >= 1057;
            byte groupCount = hasFrameGroups ? binaryReader.ReadUnsignedByte() : (byte)1U;
            for (int i = 0; i < groupCount; i++)
            {
                FrameGroupType groupType = FrameGroupType.Default;
                if (hasFrameGroups)
                    groupType = (FrameGroupType)binaryReader.ReadUnsignedByte();

                FrameGroup frameGroup = new FrameGroup();
                frameGroup.Unserialize(binaryReader);
                FrameGroups[groupType] = frameGroup;
            }
        }

        private void ThrowUnknownFlag(int attr)
        {
            throw new ArgumentException(string.Format("Unknown flag (ID = {0}, Category = {1}): {2}", ID, Category, attr));
        }

        private void Serialize854(IO.BinaryStream binaryWriter)
        {
            foreach (var pair in Attributes)
            {
                switch (pair.Key)
                {
                    case AttributesUniform.Ground:
                        binaryWriter.WriteUnsignedByte(Attributes854.Ground);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.GroundBorder:
                        binaryWriter.WriteUnsignedByte(Attributes854.GroundBorder);
                        break;
                    case AttributesUniform.Bottom:
                        binaryWriter.WriteUnsignedByte(Attributes854.Bottom);
                        break;
                    case AttributesUniform.Top:
                        binaryWriter.WriteUnsignedByte(Attributes854.Top);
                        break;
                    case AttributesUniform.Container:
                        binaryWriter.WriteUnsignedByte(Attributes854.Container);
                        break;
                    case AttributesUniform.Stackable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Stackable);
                        break;
                    case AttributesUniform.ForceUse:
                        binaryWriter.WriteUnsignedByte(Attributes854.ForceUse);
                        break;
                    case AttributesUniform.MultiUse:
                        binaryWriter.WriteUnsignedByte(Attributes854.MultiUse);
                        break;
                    case AttributesUniform.Charges:
                        binaryWriter.WriteUnsignedByte(Attributes854.Charges);
                        break;
                    case AttributesUniform.Writable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Writable);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.WritableOnce:
                        binaryWriter.WriteUnsignedByte(Attributes854.WritableOnce);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.FluidContainer:
                        binaryWriter.WriteUnsignedByte(Attributes854.FluidContainer);
                        break;
                    case AttributesUniform.Splash:
                        binaryWriter.WriteUnsignedByte(Attributes854.Splash);
                        break;
                    case AttributesUniform.Unpassable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Unpassable);
                        break;
                    case AttributesUniform.Unmoveable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Unmoveable);
                        break;
                    case AttributesUniform.Unsight:
                        binaryWriter.WriteUnsignedByte(Attributes854.Unsight);
                        break;
                    case AttributesUniform.BlockPath:
                        binaryWriter.WriteUnsignedByte(Attributes854.BlockPath);
                        break;
                    case AttributesUniform.Pickupable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Pickupable);
                        break;
                    case AttributesUniform.Hangable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Hangable);
                        break;
                    case AttributesUniform.HookSouth:
                        binaryWriter.WriteUnsignedByte(Attributes854.HookSouth);
                        break;
                    case AttributesUniform.HookEast:
                        binaryWriter.WriteUnsignedByte(Attributes854.HookEast);
                        break;
                    case AttributesUniform.Rotateable:
                        binaryWriter.WriteUnsignedByte(Attributes854.Rotateable);
                        break;
                    case AttributesUniform.Light:
                        var data = (Light)pair.Value;
                        binaryWriter.WriteUnsignedByte(Attributes854.Light);
                        binaryWriter.WriteUnsignedShort(data.intensity);
                        binaryWriter.WriteUnsignedShort(data.color);
                        break;
                    case AttributesUniform.DontHide:
                        binaryWriter.WriteUnsignedByte(Attributes854.DontHide);
                        break;
                    case AttributesUniform.FloorChange:
                        binaryWriter.WriteUnsignedByte(Attributes854.FloorChange);
                        break;
                    case AttributesUniform.Offset:
                        var offset = (Vector2Int)pair.Value;
                        binaryWriter.WriteUnsignedByte(Attributes854.Light);
                        binaryWriter.WriteUnsignedShort(offset.x);
                        binaryWriter.WriteUnsignedShort(offset.y);
                        break;
                    case AttributesUniform.Elevation:
                        binaryWriter.WriteUnsignedByte(Attributes854.Elevation);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.LyingCorpse:
                        binaryWriter.WriteUnsignedByte(Attributes854.LyingCorpse);
                        break;
                    case AttributesUniform.AnimateAlways:
                        binaryWriter.WriteUnsignedByte(Attributes854.AnimateAlways);
                        break;
                    case AttributesUniform.MinimapColor:
                        binaryWriter.WriteUnsignedByte(Attributes854.MinimapColor);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.LensHelp:
                        binaryWriter.WriteUnsignedByte(Attributes854.LensHelp);
                        binaryWriter.WriteUnsignedShort((ushort)pair.Value);
                        break;
                    case AttributesUniform.FullGround:
                        binaryWriter.WriteUnsignedByte(Attributes854.FullGround);
                        break;
                    default:
                        break;
                }
            }

            binaryWriter.WriteUnsignedByte(Attributes854.Last);
        }

        private bool Unserialize854(IO.BinaryStream binaryReader, ref int lastAttr, ref int previousAttr, ref int attr)
        {
            attr = binaryReader.ReadUnsignedByte();
            while (attr < Attributes854.Last)
            {
                switch (attr)
                {
                    case Attributes854.Ground:
                        Attributes[AttributesUniform.Ground] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.GroundBorder:
                        Attributes[AttributesUniform.GroundBorder] = true;
                        break;
                    case Attributes854.Bottom:
                        Attributes[AttributesUniform.Bottom] = true;
                        break;
                    case Attributes854.Top:
                        Attributes[AttributesUniform.Top] = true;
                        break;
                    case Attributes854.Container:
                        Attributes[AttributesUniform.Container] = true;
                        break;
                    case Attributes854.Stackable:
                        Attributes[AttributesUniform.Stackable] = true;
                        break;
                    case Attributes854.ForceUse:
                        Attributes[AttributesUniform.ForceUse] = true;
                        break;
                    case Attributes854.MultiUse:
                        Attributes[AttributesUniform.MultiUse] = true;
                        break;
                    case Attributes854.Charges:
                        Attributes[AttributesUniform.Charges] = true;
                        break;
                    case Attributes854.Writable:
                        Attributes[AttributesUniform.Writable] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.WritableOnce:
                        Attributes[AttributesUniform.WritableOnce] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.FluidContainer:
                        Attributes[AttributesUniform.FluidContainer] = true;
                        break;
                    case Attributes854.Splash:
                        Attributes[AttributesUniform.Splash] = true;
                        break;
                    case Attributes854.Unpassable:
                        Attributes[AttributesUniform.Unpassable] = true;
                        break;
                    case Attributes854.Unmoveable:
                        Attributes[AttributesUniform.Unmoveable] = true;
                        break;
                    case Attributes854.Unsight:
                        Attributes[AttributesUniform.Unsight] = true;
                        break;
                    case Attributes854.BlockPath:
                        Attributes[AttributesUniform.BlockPath] = true;
                        break;
                    case Attributes854.Pickupable:
                        Attributes[AttributesUniform.Pickupable] = true;
                        break;
                    case Attributes854.Hangable:
                        Attributes[AttributesUniform.Hangable] = true;
                        break;
                    case Attributes854.HookSouth:
                        Attributes[AttributesUniform.HookSouth] = true;
                        break;
                    case Attributes854.HookEast:
                        Attributes[AttributesUniform.HookEast] = true;
                        break;
                    case Attributes854.Rotateable:
                        Attributes[AttributesUniform.Rotateable] = true;
                        break;
                    case Attributes854.Light:
                        Light data = new Light();
                        data.intensity = binaryReader.ReadUnsignedShort();
                        data.color = binaryReader.ReadUnsignedShort();
                        Attributes[AttributesUniform.Light] = data;
                        break;
                    case Attributes854.DontHide:
                        Attributes[AttributesUniform.DontHide] = true;
                        break;
                    case Attributes854.FloorChange:
                        Attributes[AttributesUniform.FloorChange] = true;
                        break;
                    case Attributes854.Offset:
                        ushort offsetX = binaryReader.ReadUnsignedShort();
                        ushort offsetY = binaryReader.ReadUnsignedShort();
                        Attributes[AttributesUniform.Offset] = new Vector2Int(offsetX, offsetY);
                        break;
                    case Attributes854.Elevation:
                        Attributes[AttributesUniform.Elevation] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.LyingCorpse:
                        Attributes[AttributesUniform.LyingCorpse] = true;
                        break;
                    case Attributes854.AnimateAlways:
                        Attributes[AttributesUniform.AnimateAlways] = true;
                        break;
                    case Attributes854.MinimapColor:
                        Attributes[AttributesUniform.MinimapColor] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.LensHelp:
                        Attributes[AttributesUniform.LensHelp] = binaryReader.ReadUnsignedShort();
                        break;
                    case Attributes854.FullGround:
                        Attributes[AttributesUniform.FullGround] = true;
                        break;
                    default:
                        ThrowUnknownFlag(attr);
                        break;
                }

                lastAttr = previousAttr;
                previousAttr = attr;
                attr = binaryReader.ReadUnsignedByte();
            }

            return true;
        }
    }
}