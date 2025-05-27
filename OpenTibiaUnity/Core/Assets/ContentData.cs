using System.Collections.Generic;

namespace OpenTibiaUnity.Core.Assets
{
    using ThingTypesDict = Dictionary<ushort, ThingType>;

    public enum ThingCategory : byte
    {
        Item = 0,
        Creature,
        Effect,
        Missile,
        InvalidCategory,
        LastCategory = InvalidCategory
    };

    public sealed class ContentData
    {
        IO.BinaryStream _binaryReader;
        int _clientVersion;

        public uint DatSignature { get; private set; }
        public ushort ContentRevision { get; private set; }
        public ThingTypesDict[] ThingTypeDictionaries { get; private set; } = new ThingTypesDict[(int)ThingCategory.LastCategory];

        public ContentData(byte[] buffer, int clientVersion)
        {
            _binaryReader = new IO.BinaryStream(buffer);
            _clientVersion = clientVersion;

            Parse();
        }

        private void Parse()
        {
            DatSignature = _binaryReader.ReadUnsignedInt();
            ContentRevision = (ushort)DatSignature;

            int[] counts = new int[(int)ThingCategory.LastCategory];
            for (int category = 0; category < (int)ThingCategory.LastCategory; category++)
            {
                int count = _binaryReader.ReadUnsignedShort() + 1;
                counts[category] = count;
            }

            for (int category = 0; category < (int)ThingCategory.LastCategory; category++)
            {
                ushort firstId = 1;
                if (category == (int)ThingCategory.Item)
                {
                    firstId = 100;
                }

                ThingTypeDictionaries[category] = new ThingTypesDict();
                for (ushort id = firstId; id < counts[category]; id++)
                {
                    ThingType thingType = new ThingType()
                    {
                        ID = id,
                        Category = (ThingCategory)category,
                    };

                    thingType.Unserialize(_binaryReader, _clientVersion);
                    ThingTypeDictionaries[category][id] = thingType;
                }
            }
        }
    }
}