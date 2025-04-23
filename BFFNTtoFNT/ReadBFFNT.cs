using Snowberry.IO;
using BFFNTtoFNT.Classes; 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snowberry.IO.Common;
using Snowberry.IO.Reader;
using System.Reflection.Metadata.Ecma335;
using Snowberry.IO.Common.Reader;
using System.Reflection;

namespace BFFNTtoFNT
{
    internal class ReadBFFNT
    {
        string filedirectory;
        BFFNT bffnt;
        public ReadBFFNT(BFFNT inBffnt)
        {
            filedirectory = inBffnt.fileDirectory;
            bffnt = inBffnt;
        }
        /// <summary>
        /// Parses the BFFNT File
        /// </summary>
        public void ParseBFFNT()
        {
            EndianStreamReader file = new EndianStreamReader(File.OpenRead(filedirectory));
            while (file.Position < file.Length)
            {
                string fileMagic = file.ReadSizedCString(4)??"";
                switch (fileMagic)
                {
                    case "FFNT":
                        ParseHeader(bffnt, file);
                        break;
                    case "FINF":
                        ParseFontInfo(bffnt, file);
                        break;
                    case "TGLP":
                        ParseTextureInfo(bffnt, file);
                        break;
                    case "CWDH":
                        ParseCharWidhtInfo(bffnt, file);
                        break;
                    case "CMAP":
                        ParseCodeMap(bffnt, file);
                        break;
                    case "KRNG":
                        ParseKerning(bffnt, file);
                        break;
                    default:
                        ThrowException(file,"Unkonwn Section Magic: " + fileMagic);
                        break;
                }
                RoundCursor(file);
            }
            //Closes the BFFNT.
            file.Stream.Close();
        }

        /// <summary>
        /// Parses the FFNT Header
        /// </summary>
        private void ParseHeader(BFFNT bffnt, EndianStreamReader file)
        {
            EndianType bom = EndianType.LITTLE;
            //Interprets the Byte Order of the file (Might allow for Switch Versions of file)
            EndianType getByteOrder()
            {
                int[] byteOrder = [file.ReadByte(), file.ReadByte()];
                if (byteOrder.SequenceEqual([0xFE, 0xFF]))
                {
                    return bom = EndianType.BIG;
                }
                else if (byteOrder.SequenceEqual([0xFF, 0xFE]))
                {
                    return bom = EndianType.LITTLE;
                }
                else
                {
                    ThrowException(file, "Unknown Endian Type");
                    return bom = EndianType.BIG;
                }
            }
            //Check if the internal file size meets actual file size.
            int checkFileSize()
            {
                int size = file.ReadInt32(bom);
                if(size != (file.Stream.Length))
                {
                    throw new Exception("Internal FileSize does not match actual FileSize");
                }
                return size;
            }

            long initPos = file.Position-4;
            bffnt.header = new FFNT
            {
                magic = "FFNT",
                endian = getByteOrder(),
                secSize = file.ReadInt16(bom),
                version = [file.ReadByte(), file.ReadByte(), file.ReadByte(), file.ReadByte()],
                fileSize = checkFileSize(),
                secNum = file.ReadInt16(bom)
            };
            //Set position to end of section.
            file.Stream.Position = initPos + bffnt.header.secSize;
        }

        /// <summary>
        /// Parses the FINF Header
        /// </summary>
        private void ParseFontInfo(BFFNT bffnt, EndianStreamReader file)
        {
            long initPos = file.Stream.Position-4;
            FontType getFntType()
            {
                uint fnt = file.ReadByte();
                //Check for unknown font types. 
                //Dont know how this affects the process, so accept every form. 
                if(fnt == 0x00 || fnt > 0x03)
                {
                    throw new Exception("Unknown Font Type");
                }
                return (FontType) fnt;
            }
            GlyphCode getCharEnc()
            {
                uint enc = file.ReadByte();
                //Continue only if Unicode Encoding is used.
                if (enc > 0x03)
                {
                    throw new Exception("Unknown Encoding");
                }
                if (enc == 0)
                {
                    throw new Exception("UTF 8 Encoding not supported.");
                }
                if (enc == 2)
                {
                    throw new Exception("ShiftJIS Encoding not supported.");
                }
                if (enc == 3)
                {
                    throw new Exception("CP1252 Encoding not supported.");
                }
                return (GlyphCode) enc;
            }
            bffnt.fontInfo = new FINF
            {
                magic = "FINF",
                secSize = file.ReadInt32(bffnt.header.endian),
                fntType = getFntType(),
                width  = file.ReadByte(),
                height = file.ReadByte(),
                ascent = file.ReadByte(),
                lineFeed = file.ReadInt16(),
                invalidCharSym = file.ReadInt16(),
                defLeftDist = file.ReadByte(),
                defGlyphWidth = file.ReadByte(),
                defCharWidth = file.ReadByte(),
                charEncoding = getCharEnc(),
                tglpOffset = file.ReadInt32(),
                cwdhOffset = file.ReadInt32(),
                cmapOffset = file.ReadInt32(),
            };
            //Set position to end of section.
            file.Stream.Position = initPos + bffnt.fontInfo.secSize;
        }

        /// <summary>
        /// Parses the TGLP Header
        /// </summary>
        private void ParseTextureInfo(BFFNT bffnt, EndianStreamReader file)
        {
            long initPos = file.Stream.Position-4;
            bffnt.texInfo = new TGLP
            {
                magic = "TGLP",
                secSize = file.ReadInt32(bffnt.header.endian),
                cellWidth = file.ReadByte(),
                cellHeight = file.ReadByte(),
                sheetNum = file.ReadByte(),
                maxCharWidth = file.ReadByte(),
                sheetSize = file.ReadInt32(bffnt.header.endian),
                baseLinePos = file.ReadInt16(bffnt.header.endian),
                texFormat = file.ReadInt16(bffnt.header.endian),
                cellPerRow = file.ReadInt16(bffnt.header.endian),
                cellPerCol = file.ReadInt16(bffnt.header.endian),
                texWidth = file.ReadInt16(bffnt.header.endian),
                texHeight = file.ReadInt16(bffnt.header.endian),
                texDataOffset = file.ReadInt32(bffnt.header.endian)
            };
            //Only continue if ther is one texture sheet. 
            //TODO: Handle Multiple Sheets
            if(bffnt.texInfo.sheetNum != 1)
            {
                throw new Exception("Expected one Texture Sheet, got: " +  bffnt.texInfo.sheetNum);
            }
            //Set position to end of section.
            file.Stream.Position = initPos + bffnt.texInfo.secSize;
        }

        /// <summary>
        /// Parses the CWDH Header
        /// </summary>
        private void ParseCharWidhtInfo(BFFNT bffnt, EndianStreamReader file)
        {
            RegionRange charWidthRange = new RegionRange
            {
                StartPosition = file.ActualPosition - 4,
                Size = file.ReadInt32(bffnt.header.endian),
            };
            file.EnableRegionView(charWidthRange); // Enable reding range only for the character width section.

            file.Position += 8; // Skip header and size info (already processed)

            int firstGlyphIDX = file.ReadInt16(bffnt.header.endian);
            int lastGlyphIDX = file.ReadInt16(bffnt.header.endian);
            file.Position += 4; // Skip the next 4 bytes (unused sizeOfWidthData?)
            Character[] charInfo = new Character[lastGlyphIDX - firstGlyphIDX+1];
            for (int i = 0; i <= lastGlyphIDX - firstGlyphIDX; i++)
            {
                charInfo[i] = new Character
                {
                    leftDistance = (sbyte)file.ReadByte(),
                    charWidth = file.ReadByte(),
                    glyphWidth = file.ReadByte(),
                };
            }
            bffnt.charInfo = charInfo;
            
            file.DisableRegionView(); // Disable reading range after the character width section.
        }

        /// <summary>
        /// Parses the CMAP Header
        /// </summary>
        private void ParseCodeMap(BFFNT bffnt, EndianStreamReader file)
        {
            List<CMAP> returnCMAPS = new List<CMAP>();
            MapMethod mapMethod = MapMethod.Direct;
            int firstCharCode;
            int lastCharCode;
            int nextCMAPOffset;
            MapMethod getMapMethod()
            {
                mapMethod = (MapMethod)file.ReadInt16(bffnt.header.endian);
                file.Position += 2;
                return mapMethod;
            }
             //Reads the CMAP Data
            void processCharMaps()
            {
                Character[] chars = bffnt.charInfo;
                switch (mapMethod)
                {
                    case MapMethod.Direct:
                        throw new Exception("Direct Mapping Method not Implemented yet");
                        //outCharMap = new CharMap[lastCharCode - firstCharCode];
                        //int startIndex = file.ReadInt16(bffnt.header.endian);
                        //file.Stream.Position += 2;
                        //for (int i = 0; i < outCharMap.Length; i++)
                        //{
                        //    CharMap charMap = new CharMap
                        //    {
                        //        charCode = firstCharCode + i,
                        //        index = startIndex + i,
                        //    };
                        //    outCharMap[i] = charMap;
                        //}
                        //break;
                    case MapMethod.Table:
                        throw new Exception("Table Mapping Method not Implemented yet");
                        //outCharMap = new CharMap[lastCharCode - firstCharCode];
                        //for (int i = 0; i < outCharMap.Length; i++)
                        //{
                        //    int index = file.ReadInt16(bffnt.header.endian);
                        //    if(index == 0xFFFF) // Invalid Character check
                        //    {
                        //        continue;
                        //    }
                        //    CharMap charMap = new CharMap
                        //    {
                        //        charCode = index + i,
                        //        index = i,
                        //    };
                        //    outCharMap[i] = charMap;
                        //}
                        //break;

                    case MapMethod.Scan:
                        // new Exception("Scan Mapping Method not Implemented yet");
                        int entries = file.ReadInt16(bffnt.header.endian);
                        for (int i = 0; i < entries; i++)
                        {
                            int charCode = file.ReadInt16(bffnt.header.endian);
                            int glyphIDX = file.ReadInt16(bffnt.header.endian);
                            chars[glyphIDX].charCode = charCode;
                        }
                        break;
                    default:
                        break;

                }
            }

            //Repeate while the next CMAP Offset is not 0. (0 means there are no more CMAP's) (OLD)
            //do
            //{
            //    long initPos = file.Stream.Position-4;
            //    CMAP cMAP = new CMAP
            //    {
            //        magic = "CMAP",
            //        secSize = file.ReadInt32(bffnt.header.endian),
            //        firstCharCode = firstChar = file.ReadInt16(bffnt.header.endian),
            //        lastCharCode = lastChar = file.ReadInt16(bffnt.header.endian),
            //        mapType = getMapMethod(),
            //        mapDataSize = nextCMAPOffset = file.ReadInt32(),
            //        charMaps = getCharMaps()
            //    };
            //    returnCMAPS.Add(cMAP);
            //    //Set position to end of section.
            //    file.Stream.Position = initPos + cMAP.secSize;
            //} while (nextCMAPOffset != 0);
            //bffnt.charMapInfo = returnCMAPS.ToArray();
            do
            {
                RegionRange cmapRange = new RegionRange
                {
                    StartPosition = file.ActualPosition - 4,
                    Size = file.ReadInt32(bffnt.header.endian),
                };
                file.EnableRegionView(cmapRange); // Enable reading range only for the CMAP section.
                file.Position += 8; // Skip the bytes we just read.
                firstCharCode = file.ReadInt16(bffnt.header.endian);
                lastCharCode = file.ReadInt16(bffnt.header.endian);
                mapMethod = getMapMethod();
                nextCMAPOffset = file.ReadInt32(bffnt.header.endian);
                processCharMaps();

                file.DisableRegionView(); // Disable reading range after the CMAP section.

            } while (nextCMAPOffset != 0);
        }

        /// <summary>
        /// Parses the KRNG Header
        /// </summary>
        private void ParseKerning(BFFNT bffnt, EndianStreamReader file)
        {
            long initPos = file.Stream.Position - 4;
            int size = file.ReadInt32(bffnt.header.endian);
            file.Stream.Position = initPos + size;
            //RegionRange kernRange = new RegionRange
            //{
            //    StartPosition = file.Position - 4,
            //    Size = file.ReadInt32(bffnt.header.endian),
            //};
            //file.EnableRegionView(kernRange); // Enable reading range only for the kerning section.
            //file.Position += 8; // Skip header and size info (already processed)

            //while(file.Position < file.Length)
            //{
            //    int glyphIDX = file.ReadInt16(bffnt.header.endian); //FIX Identifying charCode via glyphIDX;
            //}
        }

        /// <summary>
        /// Rounds the cursor to the nearest 4 bytes. 
        /// </summary>
        private void RoundCursor(EndianStreamReader file)
        {
            if(file.Position % 4 != 0)
            {
                file.Position = ((file.Position + 3) / 4) * 4;
            }
        }

        //Throws Custom exception and closes the file. 
        private void ThrowException(EndianStreamReader file, string message)
        {
            file.Stream.Close();
            throw new Exception(message);
        }
    }
}
