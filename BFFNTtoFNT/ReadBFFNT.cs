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
                        ParseFFNT(bffnt, file);
                        break;
                    case "FINF":
                        ParseFINF(bffnt, file);
                        break;
                    case "TGLP":
                        ParseTGLP(bffnt, file);
                        break;
                    case "CWDH":
                        ParseCWDH(bffnt, file);
                        break;
                    case "CMAP":
                        ParseCMAP(bffnt, file);
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
        private void ParseFFNT(BFFNT bffnt, EndianStreamReader file)
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
                    ThrowException(file, "Internal FileSize does not match actual FileSize");
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

            //TODO: Maybe add version check here. Really only tested with SMMU fonts. 

            //Set position to end of section.
            file.Stream.Position = initPos + bffnt.header.secSize;
        }

        /// <summary>
        /// Parses the FINF Header
        /// </summary>
        private void ParseFINF(BFFNT bffnt, EndianStreamReader file)
        {
            long initPos = file.Stream.Position-4;
            FontType getFntType()
            {
                uint fnt = file.ReadByte();
                //Check for unknown font types. 
                //Dont know how this affects the process, so accept every form. 
                if(fnt == 0x00 || fnt > 0x03)
                {
                    ThrowException(file, "Unknown Font Type");
                }
                return (FontType) fnt;
            }
            GlyphCode getCharEnc()
            {
                uint enc = file.ReadByte();
                //Continue only if Unicode Encoding is used.
                if (enc > 0x03)
                {
                    ThrowException(file, "Unknown Encoding");
                }
                if (enc == 0)
                {
                    ThrowException(file, "UTF 8 Encoding not supported.");
                }
                if (enc == 2)
                {
                    ThrowException(file, "ShiftJIS Encoding not supported.");
                }
                if (enc == 3)
                {
                    ThrowException(file, "CP1252 Encoding not supported.");
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
        private void ParseTGLP(BFFNT bffnt, EndianStreamReader file)
        {
            long initPos = file.Stream.Position-4;
            bffnt.texInfo = new TGLP
            {
                magic = "TGLP",
                secSize = file.ReadInt32(bffnt.header.endian),
                glyphWidth = file.ReadByte(),
                glyphHeight = file.ReadByte(),
                numOfSheets = file.ReadByte(),
                maxGlyphWidth = file.ReadByte(),
                sheetSize = file.ReadInt32(bffnt.header.endian),
                baseLinePos = file.ReadInt16(bffnt.header.endian),
                sheetFormat = file.ReadInt16(bffnt.header.endian),
                glyphsPerRow = file.ReadInt16(bffnt.header.endian),
                glyphsPerCol = file.ReadInt16(bffnt.header.endian),
                sheetWidth = file.ReadInt16(bffnt.header.endian),
                sheetHeight = file.ReadInt16(bffnt.header.endian),
                sheetDataOffset = file.ReadInt32(bffnt.header.endian)
            };
            //Set position to end of section.
            file.Stream.Position = initPos + bffnt.texInfo.secSize;
        }

        /// <summary>
        /// Parses the CWDH Header
        /// </summary>
        private void ParseCWDH(BFFNT bffnt, EndianStreamReader file)
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
                    xOffset = (sbyte)file.ReadByte(),
                    charWidth = file.ReadByte(),
                    charAdvance = file.ReadByte(),
                };
            }
            bffnt.charInfo = charInfo;
            
            file.DisableRegionView(); // Disable reading range after the character width section.
        }

        /// <summary>
        /// Parses the CMAP Header
        /// </summary>
        private void ParseCMAP(BFFNT bffnt, EndianStreamReader file)
        {
            MapMethod mapMethod = MapMethod.Direct;
            int firstCharCode;
            int lastCharCode;
            int nextCMAPOffset;
            MapMethod getMapMethod()
            {
                mapMethod = (MapMethod)file.ReadInt16(bffnt.header.endian);
                file.Position += 2; //Padding
                return mapMethod;
            }
             //Reads the CMAP Data
            void processCharMaps()
            {
                //NOTES: Writes directly into the charInfo array of the BFFNT object, rather than storing things in the CMAP array. 
                
                Character[] chars = bffnt.charInfo;
                switch (mapMethod)
                {
                    case MapMethod.Direct:
                        int baseIDX_Direct = file.ReadInt16(bffnt.header.endian);
                        for (int i = 0; i <= lastCharCode - firstCharCode; i++)
                        {
                            chars[baseIDX_Direct + i].charCode = firstCharCode + i;
                        }
                        file.Stream.Position += 2;
                        break;
                    case MapMethod.Table:
                        for (int i = 0; i <= lastCharCode - firstCharCode; i++)
                        {
                            int glyphIDX_Table = file.ReadInt16(bffnt.header.endian);
                            if (glyphIDX_Table == -1) continue; // Skip if glyph index is -1 (invalid)
                            chars[glyphIDX_Table].charCode = firstCharCode + i;
                        }
                        
                        break;

                    case MapMethod.Scan:
                        int entries = file.ReadUInt16(bffnt.header.endian);
                        for (int i = 0; i < entries; i++)
                        {
                            int charCode = file.ReadUInt16(bffnt.header.endian);
                            int glyphIDX_Scan = file.ReadUInt16(bffnt.header.endian);
                            chars[glyphIDX_Scan].charCode = charCode;
                        }
                        break;
                    default:
                        break;

                }
            }
            RegionRange cmapRange = new RegionRange
            {
                StartPosition = file.ActualPosition - 4,
                Size = file.ReadInt32(bffnt.header.endian),
            };
            file.EnableRegionView(cmapRange); // Enable reading range only for the CMAP section.
            file.Position += 8; // Skip the bytes we just read.
            firstCharCode = file.ReadUInt16(bffnt.header.endian);
            lastCharCode = file.ReadUInt16(bffnt.header.endian);
            mapMethod = getMapMethod();
            nextCMAPOffset = file.ReadInt32(bffnt.header.endian);
            processCharMaps();

            file.DisableRegionView(); // Disable reading range after the CMAP section.
        }

        /// <summary>
        /// Parses the KRNG Header
        /// </summary>
        private void ParseKerning(BFFNT bffnt, EndianStreamReader file)
        {
            //get initial variables. 
            long initPos = file.Stream.Position - 4;
            int secSize = file.ReadInt32(bffnt.header.endian);
            long startPos = file.Stream.Position;
            int numKernFirst = file.ReadInt16(bffnt.header.endian);

            for (int i = 0; i < numKernFirst; i++)
            {
                int charCode = file.ReadUInt16(bffnt.header.endian);
                int charIDX = bffnt.getCharInfoIndex(charCode);
                int scndWord_offset = file.ReadUInt16(bffnt.header.endian) * 2;
                long nextFirstWordPos = file.Stream.Position;

                file.Stream.Position = startPos + scndWord_offset;
                int countSecWord = file.ReadUInt16(bffnt.header.endian);
                List<KernSecondWord> kernSecondWords = new List<KernSecondWord>();
                for (int j = 0; j < countSecWord; j++)
                {
                    KernSecondWord thisSecWord = new KernSecondWord
                    {
                        charCode_Second = file.ReadUInt16(bffnt.header.endian),
                        kernOffset = file.ReadInt16(bffnt.header.endian)
                    };
                    kernSecondWords.Add(thisSecWord);
                }
                bffnt.charInfo[charIDX].kernWords = kernSecondWords.ToArray();
                file.Stream.Position = nextFirstWordPos;
            }
            file.Stream.Position = initPos + secSize;
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
            try
            {
                file?.Stream?.Close();
                MessageBox.Show(message, "Error when parsing BFFNT file.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error during fatal shutdown: " + ex.Message);

                Environment.Exit(-1);
            }
        }
    }
}
