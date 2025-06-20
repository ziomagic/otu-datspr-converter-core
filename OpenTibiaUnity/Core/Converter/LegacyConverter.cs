﻿using Google.Protobuf;
using Google.Protobuf.Collections;
using OpenTibiaUnity.Core.Metaflags;
using OpenTibiaUnity.Protobuf.Appearances;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenTibiaUnity.Core.Converter
{
    // TODO, refactor this whole thing as it was done initially just
    // to fit the purpose and didn't gave much care about style
    public class LegacyConverter : IConverter
    {
        private struct FrameGroupDetail
        {
            public int Width;
            public int Height;

            public FrameGroupDetail(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        private int _clientVersion;
        private uint m_ReferencedSpriteID = 0;
        private uint m_ReferenceFrameGroupID = 0;
        private Dictionary<FrameGroup, FrameGroupDetail> m_FrameGroupDetails = new Dictionary<FrameGroup, FrameGroupDetail>();
        private List<SpriteTypeImpl> m_SpriteSheet = new List<SpriteTypeImpl>();

        public LegacyConverter(int clientVersion)
        {
            _clientVersion = clientVersion;
        }

        AppearanceFlags GenerateAppearanceFlags(Assets.ThingType thingType, Appearance appearance)
        {
            if (thingType.Attributes.Count == 0)
                return null;

            var appearanceFlags = new AppearanceFlags();

            if (thingType.HasAttribute(AttributesUniform.Ground)) appearanceFlags.Ground = new AppearanceFlagGround() { Speed = (ushort)thingType.Attributes[AttributesUniform.Ground] };
            if (thingType.HasAttribute(AttributesUniform.Writable)) appearanceFlags.Writable = new AppearanceFlagWritable() { MaxTextLength = (ushort)thingType.Attributes[AttributesUniform.Writable] };
            if (thingType.HasAttribute(AttributesUniform.WritableOnce)) appearanceFlags.WritableOnce = new AppearanceFlagWritableOnce() { MaxTextLengthOnce = (ushort)thingType.Attributes[AttributesUniform.WritableOnce] };
            if (thingType.HasAttribute(AttributesUniform.MinimapColor)) appearanceFlags.Automap = new AppearanceFlagAutomap() { Color = (ushort)thingType.Attributes[AttributesUniform.MinimapColor] };
            if (thingType.HasAttribute(AttributesUniform.Elevation)) appearanceFlags.Height = new AppearanceFlagHeight() { Elevation = (ushort)thingType.Attributes[AttributesUniform.Elevation] };
            if (thingType.HasAttribute(AttributesUniform.LensHelp)) appearanceFlags.LensHelp = new AppearanceFlagLensHelp() { ID = (ushort)thingType.Attributes[AttributesUniform.LensHelp] };
            if (thingType.HasAttribute(AttributesUniform.Cloth)) appearanceFlags.Clothes = new AppearanceFlagClothes() { Slot = (ushort)thingType.Attributes[AttributesUniform.Cloth] };

            // default action
            if (thingType.HasAttribute(AttributesUniform.DefaultAction))
            {
                var oldDefaultActionValue = (ushort)thingType.Attributes[AttributesUniform.DefaultAction];
                if (oldDefaultActionValue > 4)
                    Console.WriteLine("Invalid default action: " + oldDefaultActionValue + " for item id: " + thingType.ID);
                appearanceFlags.DefaultAction = new AppearanceFlagDefaultAction() { Action = (Protobuf.Shared.PlayerAction)oldDefaultActionValue };
            }

            if (thingType.HasAttribute(AttributesUniform.GroundBorder)) appearanceFlags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.Bottom)) appearanceFlags.Bottom = true;
            if (thingType.HasAttribute(AttributesUniform.Top)) appearanceFlags.Top = true;
            if (thingType.HasAttribute(AttributesUniform.Container)) appearanceFlags.Container = true;
            if (thingType.HasAttribute(AttributesUniform.Stackable)) appearanceFlags.Stackable = true;
            if (thingType.HasAttribute(AttributesUniform.Use)) appearanceFlags.Use = true;
            if (thingType.HasAttribute(AttributesUniform.ForceUse)) appearanceFlags.ForceUse = true;
            if (thingType.HasAttribute(AttributesUniform.MultiUse)) appearanceFlags.MultiUse = true;
            if (thingType.HasAttribute(AttributesUniform.FluidContainer)) appearanceFlags.FluidContainer = true;
            if (thingType.HasAttribute(AttributesUniform.Splash)) appearanceFlags.Splash = true;
            if (thingType.HasAttribute(AttributesUniform.Unpassable)) appearanceFlags.Unpassable = true;
            if (thingType.HasAttribute(AttributesUniform.Unmoveable)) appearanceFlags.Unmoveable = true;
            if (thingType.HasAttribute(AttributesUniform.Unsight)) appearanceFlags.Unsight = true;
            if (thingType.HasAttribute(AttributesUniform.BlockPath)) appearanceFlags.BlockPath = true;
            if (thingType.HasAttribute(AttributesUniform.NoMoveAnimation)) appearanceFlags.NoMoveAnimation = true;
            if (thingType.HasAttribute(AttributesUniform.Pickupable)) appearanceFlags.Pickupable = true;
            if (thingType.HasAttribute(AttributesUniform.Hangable)) appearanceFlags.Hangable = true;

            // can have only one hook //
            if (thingType.HasAttribute(AttributesUniform.HookSouth)) appearanceFlags.Hook = new AppearanceFlagHook() { Type = Protobuf.Shared.HookType.South };
            else if (thingType.HasAttribute(AttributesUniform.HookEast)) appearanceFlags.Hook = new AppearanceFlagHook() { Type = Protobuf.Shared.HookType.East };

            if (thingType.HasAttribute(AttributesUniform.Rotateable)) appearanceFlags.Rotateable = true;
            if (thingType.HasAttribute(AttributesUniform.DontHide)) appearanceFlags.DontHide = true;
            if (thingType.HasAttribute(AttributesUniform.Translucent)) appearanceFlags.Translucent = true;
            if (thingType.HasAttribute(AttributesUniform.LyingCorpse)) appearanceFlags.LyingCorpse = true;
            if (thingType.HasAttribute(AttributesUniform.AnimateAlways)) appearanceFlags.AnimateAlways = true;
            if (thingType.HasAttribute(AttributesUniform.FullGround)) appearanceFlags.FullGround = true;
            if (thingType.HasAttribute(AttributesUniform.Look)) appearanceFlags.IgnoreLook = true;
            if (thingType.HasAttribute(AttributesUniform.Wrapable)) appearanceFlags.Wrapable = true;
            if (thingType.HasAttribute(AttributesUniform.Unwrapable)) appearanceFlags.GroundBorder = true;
            if (thingType.HasAttribute(AttributesUniform.TopEffect)) appearanceFlags.TopEffect = true;

            if (thingType.HasAttribute(AttributesUniform.Light))
            {
                var lightInfo = (Assets.Light)thingType.Attributes[AttributesUniform.Light];

                appearanceFlags.Light = new AppearanceFlagLight()
                {
                    Intensity = lightInfo.intensity,
                    Color = lightInfo.color,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Offset))
            {
                var displacement = (Assets.Vector2Int)thingType.Attributes[AttributesUniform.Offset];
                appearanceFlags.Offset = new AppearanceFlagOffset()
                {
                    X = displacement.x,
                    Y = displacement.y,
                };
            }

            if (thingType.HasAttribute(AttributesUniform.Market))
            {
                var market = (Assets.MarketData)thingType.Attributes[AttributesUniform.Market];

                appearanceFlags.Market = new AppearanceFlagMarket()
                {
                    Category = (Protobuf.Shared.ItemCategory)market.category,
                    TradeAsObjectID = market.tradeAs,
                    ShowAsObjectID = market.showAs,
                    MinimumLevel = market.restrictLevel,
                };

                appearanceFlags.Market.RestrictToProfession.Add((Protobuf.Shared.PlayerProfession)market.restrictProfession);
                appearance.Name = market.name;
            }

            return appearanceFlags;
        }

        /// <summary>
        /// Generates protobuf Appearance from assets ThingType
        /// </summary>
        /// <param name="thingType">thing generated from tibia.dat (old revisions)</param>
        /// <returns></returns>
        Appearance GenerateAppearance(Assets.ThingType thingType)
        {
            var appearance = new Appearance();
            appearance.ID = thingType.ID;
            appearance.Flags = GenerateAppearanceFlags(thingType, appearance);

            foreach (var pair in thingType.FrameGroups)
            {
                var frameGroupType = pair.Key;
                var legacyFrameGroup = pair.Value;

                var frameGroup = new FrameGroup();
                var spriteInfo = new SpriteInfo();
                frameGroup.Type = pair.Key == 0 ? Protobuf.Shared.FrameGroupType.Idle : Protobuf.Shared.FrameGroupType.Walking;
                frameGroup.ID = m_ReferenceFrameGroupID++;
                frameGroup.SpriteInfo = spriteInfo;

                spriteInfo.PatternWidth = legacyFrameGroup.PatternWidth;
                spriteInfo.PatternHeight = legacyFrameGroup.PatternHeight;
                spriteInfo.PatternDepth = legacyFrameGroup.PatternDepth;
                spriteInfo.Layers = legacyFrameGroup.Layers;
                spriteInfo.Phases = legacyFrameGroup.Phases;
                spriteInfo.BoundingSquare = legacyFrameGroup.ExactSize;

                if (legacyFrameGroup.Animator != null)
                {
                    var animation = new SpriteAnimation();
                    spriteInfo.Animation = animation;
                    animation.DefaultStartPhase = (uint)legacyFrameGroup.Animator.StartPhase;
                    animation.Synchronized = !legacyFrameGroup.Animator.Async;
                    //animation.RandomStartPhase = false;

                    if (legacyFrameGroup.Animator.LoopCount < 0)
                    {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.PingPong;
                    }
                    else if (legacyFrameGroup.Animator.LoopCount == 0)
                    {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.Infinite;
                    }
                    else
                    {
                        animation.LoopType = Protobuf.Shared.AnimationLoopType.Counted;
                        animation.LoopCount = (uint)legacyFrameGroup.Animator.LoopCount;
                    }

                    foreach (var m in legacyFrameGroup.Animator.FrameGroupDurations)
                    {
                        var spritePhase = new SpritePhase();
                        spritePhase.DurationMin = (uint)m.Minimum;
                        spritePhase.DurationMax = (uint)m.Maximum;

                        animation.SpritePhases.Add(spritePhase);
                    }
                }

                foreach (var spriteID in legacyFrameGroup.Sprites)
                    spriteInfo.SpriteIDs.Add(spriteID);

                m_FrameGroupDetails.Add(frameGroup, new FrameGroupDetail(legacyFrameGroup.Width, legacyFrameGroup.Height));
                appearance.FrameGroups.Add(frameGroup);
            }

            return appearance;
        }

        Appearances GenerateAppearances(string targetDir)
        {
            try
            {
                var rawContentDat = File.ReadAllBytes(Path.Combine(targetDir, "wos.dat"));
                var contentData = new Assets.ContentData(rawContentDat, _clientVersion);

                var appearances = new Appearances();
                for (int i = 0; i < contentData.ThingTypeDictionaries.Length; i++)
                {
                    var dict = contentData.ThingTypeDictionaries[i];
                    foreach (var pair in dict)
                    {
                        var appearance = GenerateAppearance(pair.Value);
                        switch (i)
                        {
                            case 0: appearances.Objects.Add(appearance); break;
                            case 1: appearances.Outfits.Add(appearance); break;
                            case 2: appearances.Effects.Add(appearance); break;
                            case 3: appearances.Missles.Add(appearance); break;
                        }
                    }
                }

                return appearances;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                Environment.Exit(0);
            }

            return null;
        }

        public async Task<bool> BeginProcessing()
        {
            var targetPath = Path.Combine("/Users/slawek/Projects/otu-datspr-converter-core", _clientVersion.ToString());
            string datFile = Path.Combine(targetPath, "wos.dat");
            string sprFile = Path.Combine(targetPath, "wos.spr");
            if (!File.Exists(datFile) || !File.Exists(sprFile))
            {
                Console.WriteLine("Tibia.dat or Tibia.spr doesn't exist");
                return false;
            }

            Console.Write("Processing Appearances...");
            var appearances = GenerateAppearances(targetPath);
            Console.WriteLine("\rProcessing Appearances: Done!");

            // loading tibia.spr into chunks
            Assets.ContentSprites contentSprites;
            try
            {
                var rawContentSprites = File.ReadAllBytes(sprFile);
                contentSprites = new Assets.ContentSprites(rawContentSprites, _clientVersion);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message + '\n' + e.StackTrace);
                return false;
            }

            string resultPath = Path.Combine(targetPath, "result");

            Console.Write("Processing Spritesheets...");
            Directory.CreateDirectory(Path.Combine(resultPath, "sprites"));

            int start = 0;
            start = await SaveSprites(targetPath, appearances.Outfits, start, contentSprites, "outfits");
            start = await SaveSprites(targetPath, appearances.Effects, start, contentSprites, "effects");
            start = await SaveSprites(targetPath, appearances.Missles, start, contentSprites, "missiles");
            start = await SaveSprites(targetPath, appearances.Objects, start, contentSprites, "objects");

            Console.WriteLine("\rProcessing Spritesheets: Done!");

            // saving appearances.dat (with the respective version)
            using (var stream = File.Create(Path.Combine(resultPath, "appearances.otud")))
            {
                appearances.WriteTo(stream);
            }

            // save spritesheets
            using var spriteStream = new FileStream(Path.Combine(resultPath, "assets.otus"), FileMode.Create);
            using var binaryWriter = new BinaryWriter(spriteStream);

            m_SpriteSheet.Sort((a, b) =>
            {
                return a.FirstSpriteID.CompareTo(b.FirstSpriteID);
            });

            binaryWriter.Write((uint)m_SpriteSheet.Count);
            uint index = 0;
            foreach (var spriteType in m_SpriteSheet)
            {
                spriteType.AtlasID = index++;

                var buffer = File.ReadAllBytes(Path.Combine(resultPath, "sprites", spriteType.File));
                binaryWriter.Write(spriteType.AtlasID);
                binaryWriter.Write((byte)spriteType.WidthCount);
                binaryWriter.Write((byte)spriteType.HeightCount);
                binaryWriter.Write(spriteType.FirstSpriteID);
                binaryWriter.Write(spriteType.LastSpriteID);

                binaryWriter.Write((uint)buffer.Length);
                binaryWriter.Write(buffer);
            }

            // Directory.Delete(Path.Combine(resultPath, "sprites"), true);
            return true;
        }

        public List<SpriteTypeImpl> GetSpriteSheet()
        {
            return m_SpriteSheet;
        }

        static void DrawBitmap_Sprites32x32(AsyncGraphics gfx, SKBitmap[] bitmaps, int w, int h, int x = 0, int y = 0)
        {
            // if (bitmaps[3] != null) gfx.DrawImage(bitmaps[3], x, y, 32, 32);
            // if (bitmaps[2] != null) gfx.DrawImage(bitmaps[2], x + 32, y, 32, 32);
            // if (bitmaps[1] != null) gfx.DrawImage(bitmaps[1], x, y + 32, 32, 32);
            // if (bitmaps[0] != null) gfx.DrawImage(bitmaps[0], x + 32, y + 32, 32, 32);

            var c = bitmaps.Length;
            for (var i = 0; i < h; i++)
            {
                for (var k = 0; k < w; k++)
                {
                    c--;
                    if (bitmaps[c] != null)
                    {
                        gfx.DrawImage(bitmaps[c], x + (k * 32), y + (i * 32), 32, 32);
                    }
                }
            }
        }

        private async Task InternalSaveStaticBitmaps(
            string targetPath,
            RepeatedField<uint> sprites,
            int parts,
            int localStart,
            Assets.ContentSprites sprParser,
            int widthCount,
            int heightCount,
            string groupName)
        {
            var widthPx = widthCount * 32;
            var heightPx = heightCount * 32;
            int singleSizePx = widthPx * heightPx;

            var spritesheetWidth = Program.SEGMENT_DIMENTION;
            var spritesheetHeight = Program.SEGMENT_DIMENTION;

            if (spritesheetWidth % widthPx != 0)
            {
                spritesheetWidth = (spritesheetWidth / widthPx) * widthPx;
            }

            if (spritesheetHeight % heightPx != 0)
            {
                spritesheetHeight = (spritesheetHeight / heightPx) * heightPx;
            }

            var spritesheetSize = spritesheetWidth * spritesheetHeight;

            var gfx = new AsyncGraphics(new SKBitmap(spritesheetWidth, spritesheetHeight));
            string filename;

            int x = 0, y = 0, z = 0;
            for (int i = 0; i < sprites.Count;)
            {
                var bitmapParts = new SKBitmap[parts];
                for (int m = 0; m < parts; m++)
                {
                    if (i + m >= sprites.Count)
                        break;

                    bitmapParts[m] = sprParser.GetSprite(sprites[i + m]);
                }

                if (y >= spritesheetHeight)
                {
                    var localEnd = localStart + (spritesheetSize / singleSizePx);
                    filename = string.Format("{0}-{1}-{2}.png", groupName, localStart, localEnd - 1);
                    await gfx.SaveAndDispose(Path.Combine(targetPath, "result", "sprites", filename));

                    m_SpriteSheet.Add(new SpriteTypeImpl()
                    {
                        File = filename,
                        WidthCount = widthCount,
                        HeightCount = heightCount,
                        FirstSpriteID = (uint)localStart,
                        LastSpriteID = (uint)(localEnd - 1)
                    });

                    localStart = localEnd;

                    gfx = new AsyncGraphics(new SKBitmap(spritesheetWidth, spritesheetHeight));
                    x = y = z = 0;
                }

                DrawBitmap_Sprites32x32(gfx, bitmapParts, widthPx / 32, heightPx / 32, x, y);
                await gfx.DisposeOnDone(bitmapParts);

                x += widthPx;
                if (x >= spritesheetWidth)
                {
                    y += heightPx;
                    x = 0;
                }

                if (i == sprites.Count)
                    break;

                i = Math.Min(i + parts, sprites.Count);
                z++;
            }

            // save the last gfx
            int end = localStart + z;
            filename = string.Format("{0}-{1}-{2}.png", groupName, localStart, end - 1);
            await gfx.SaveAndDispose(Path.Combine(targetPath, "result", "sprites", filename));

            m_SpriteSheet.Add(new SpriteTypeImpl()
            {
                File = filename,
                WidthCount = widthCount,
                HeightCount = heightCount,
                FirstSpriteID = (uint)localStart,
                LastSpriteID = (uint)(end - 1)
            });
        }

        private async Task<int> SaveSprites(string targetPath, RepeatedField<Appearance> appearances, int start, Assets.ContentSprites sprParser, string groupName)
        {
            var output = DeploySprites(appearances);
            var keys = output.Keys.OrderBy(x => x);

            foreach (var key in keys)
            {
                var g = output[key];
                var first = g.First();
                var rf = new RepeatedField<uint>();

                foreach (var frame in g)
                {
                    rf.AddRange(frame.ids);
                }

                Console.WriteLine($"Processing {groupName} group. Size: {first.details.Width * 32}x{first.details.Height * 32}");

                int parts = first.details.Width * first.details.Height;
                if (rf.Count == 0)
                    return start;

                int localStart = start;
                start += rf.Count / parts;

                await InternalSaveStaticBitmaps(targetPath, rf, parts, localStart, sprParser, first.details.Width, first.details.Height, groupName);
            }

            return start;
        }

        private record SpriteRenderDetails(RepeatedField<uint> ids, FrameGroupDetail details);

        private Dictionary<int, List<SpriteRenderDetails>> DeploySprites(RepeatedField<Appearance> appearances)
        {
            var frameGroups = new List<(FrameGroupDetail, int, FrameGroup)>();
            var output = new Dictionary<int, List<SpriteRenderDetails>>();

            foreach (var appearance in appearances)
            {
                foreach (var frameGroup in appearance.FrameGroups)
                {
                    if (m_FrameGroupDetails.TryGetValue(frameGroup, out var detail))
                    {
                        if (detail.Width == 0 || detail.Height == 0)
                        {
                            continue;
                        }

                        var key = int.Parse($"{detail.Width}{detail.Height}");

                        frameGroups.Add((detail, key, frameGroup));
                        if (!output.ContainsKey(key))
                        {
                            output[key] = new List<SpriteRenderDetails>();
                        }

                        var rf = new RepeatedField<uint>();
                        rf.AddRange(frameGroup.SpriteInfo.SpriteIDs);
                        output[key].Add(new(rf, detail));
                    }
                }
            }

            frameGroups = frameGroups.OrderBy(x => x.Item2).ToList();
            foreach (var (detail, index, group) in frameGroups)
            {
                var parts = detail.Width * detail.Height;
                ChangeSpriteIDs(group, parts);
            }

            return output;
        }

        private void ChangeSpriteIDs(FrameGroup frameGroup, int parts)
        {
            /**
             * initialy, we save the sprite ids as per legacy sprites (only supports 32x32, aka parts)
             * so to correct this, we simply divide the total number of sprites by the number of parts
             * to obtain the sufficient number of required sprites
             */

            var spriteIDs = frameGroup.SpriteInfo.SpriteIDs;
            var newSpriteIDs = new RepeatedField<uint>();

            int total = (int)Math.Ceiling((double)spriteIDs.Count / parts);
            for (int i = 0; i < total; i++)
            {
                newSpriteIDs.Add(m_ReferencedSpriteID++);
            }

            frameGroup.SpriteInfo.SpriteIDs.Clear();
            frameGroup.SpriteInfo.SpriteIDs.AddRange(newSpriteIDs);
        }
    }
}
