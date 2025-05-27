using SkiaSharp;

namespace OpenTibiaUnity.Core.Assets
{
    public class ContentSprites
    {
        IO.BinaryStream _binaryReader;

        int _clientVersion = 0;

        public uint Signature { get; private set; } = 0;
        public uint SpritesCount { get; private set; } = 0;
        public long SpritesOffset { get; private set; } = 0;

        public ContentSprites(byte[] buffer, int clientVersion)
        {
            _binaryReader = new IO.BinaryStream(buffer);
            _clientVersion = clientVersion;

            Parse();
        }

        public static uint ClientVersionToSprSignature(int version)
        {
            switch (version)
            {
                case 770: return 0x439852be;
                case 1098: return 0x57bbd603;
                default: return 0;
            }
        }

        public byte[] ConvertTo(int newVersion)
        {
            var binaryWriter = new IO.BinaryStream();
            binaryWriter.WriteUnsignedInt(ClientVersionToSprSignature(newVersion));

            binaryWriter.WriteUnsignedShort((ushort)SpritesCount);

            _binaryReader.Seek(SpritesOffset, System.IO.SeekOrigin.Begin);
            for (uint i = 0; i < SpritesCount; i++)
            {
                var spriteAddress = _binaryReader.ReadUnsignedInt();
                if (spriteAddress == 0)
                    binaryWriter.WriteUnsignedInt(0);
                else
                    binaryWriter.WriteUnsignedInt(spriteAddress);
            }

            var pixels = _binaryReader.ReadRemaining();
            binaryWriter.Write(pixels, 0, pixels.Length);

            return binaryWriter.GetBuffer();
        }

        private void Parse()
        {
            Signature = _binaryReader.ReadUnsignedInt();
            SpritesCount = _binaryReader.ReadUnsignedShort();
            SpritesOffset = _binaryReader.Position;
        }

        public SKBitmap GetSprite(uint id)
        {
            lock (_binaryReader)
                return RawGetSprite(id);
        }

        private SKBitmap RawGetSprite(uint id)
        {
            if (id == 0 || _binaryReader == null)
                return null;

            _binaryReader.Seek((int)((id - 1) * 4) + SpritesOffset, System.IO.SeekOrigin.Begin);

            uint spriteAddress = _binaryReader.ReadUnsignedInt();
            if (spriteAddress == 0)
                return null;

            _binaryReader.Seek((int)spriteAddress, System.IO.SeekOrigin.Begin);
            _binaryReader.Skip(3); // color values

            ushort pixelDataSize = _binaryReader.ReadUnsignedShort();

            int writePos = 0;
            int read = 0;
            byte channels = 3;

            var bitmap = new SKBitmap(32, 32);
            var transparentColor = SKColors.Transparent;

            while (read < pixelDataSize && writePos < 4096)
            {
                ushort transparentPixels = _binaryReader.ReadUnsignedShort();
                ushort coloredPixels = _binaryReader.ReadUnsignedShort();

                for (int i = 0; i < transparentPixels && writePos < 4096; i++)
                {
                    int pixel = writePos / 4;
                    int x = pixel % 32;
                    int y = pixel / 32;

                    bitmap.SetPixel(x, y, transparentColor);
                    writePos += 4;
                }

                for (int i = 0; i < coloredPixels && writePos < 4096; i++)
                {
                    var r = _binaryReader.ReadUnsignedByte();
                    var g = _binaryReader.ReadUnsignedByte();
                    var b = _binaryReader.ReadUnsignedByte();
                    var a = (byte)0xFF;

                    int pixel = writePos / 4;
                    int x = pixel % 32;
                    int y = pixel / 32;

                    bitmap.SetPixel(x, y, new SKColor(r, g, b, a));
                    writePos += 4;
                }

                read += 4 + (channels * coloredPixels);
            }

            while (writePos < 4096)
            {
                int pixel = writePos / 4;
                int x = pixel % 32;
                int y = pixel / 32;

                bitmap.SetPixel(x, y, transparentColor);
                writePos += 4;
            }

            return bitmap;
        }
    }
}
