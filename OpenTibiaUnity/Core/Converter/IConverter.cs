using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenTibiaUnity.Core.Converter
{
    public class SpriteTypeImpl
    {
        public string File;
        public int WidthCount;
        public int HeightCount;
        public uint FirstSpriteID;
        public uint LastSpriteID;
        public uint AtlasID;
    }

    interface IConverter
    {
        Task<bool> BeginProcessing();

        List<SpriteTypeImpl> GetSpriteSheet();
    }
}
