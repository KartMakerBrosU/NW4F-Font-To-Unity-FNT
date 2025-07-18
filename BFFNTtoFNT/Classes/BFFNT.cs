using Snowberry.IO.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace BFFNTtoFNT.Classes
{
    public enum MapMethod : int
    {
        Direct = 0x0000,
        Table = 0x0001,
        Scan = 0x0002
    }
    public enum FontType : int
    {
        Glyph = 0x01,
        Texture = 0x02,
        PackedTexture = 0x03
    }
    public enum GlyphCode : int
    {
        UFT8 = 0x00,
        UniCode = 0x01,
        ShiftJIS = 0x02,
        CP1252 = 0x03
    }
    public class BFFNT
    {
        public string fileDirectory;
        public FFNT header;
        public FINF fontInfo;
        public TGLP texInfo;
        public CWDH charWidthInfo;
        public CMAP[] charMapInfo;
        public Character[] charInfo;
        public BFFNT(string filename)
        {
            fileDirectory = filename;
            header = new FFNT();
            fontInfo = new FINF();
            texInfo = new TGLP();
            charWidthInfo = new CWDH();
            charMapInfo = Array.Empty<CMAP>();
            charInfo = Array.Empty<Character>();
        }
        public void ReadBFFNT()
        {
            ReadBFFNT readBFFNT = new ReadBFFNT(this);
            readBFFNT.ParseBFFNT();
        }
        /// <summary>
        /// Uses binary search to find the index of a given charactercode. 
        /// </summary>
        public int getCharInfoIndex(int charCode)
        {
            int low = 0, high = charInfo.Length-1;
            while (low <= high)
            {
                int mid = low + (high-low)/2;

                if (charInfo[mid].charCode == charCode)
                    return mid;
                if (charInfo[mid].charCode < charCode)
                    low = mid + 1;
                else
                    high = mid - 1;
            }
            return -1;
        }
    }
    public class FFNT
    {
        public string magic { get; set; } = "";
        public EndianType endian { get; set; }
        public int secSize { get; set; }
        public int[] version { get; set; } = [0, 0, 0, 0];
        public int fileSize { get; set; }
        public int secNum { get; set; }
    }
    public class FINF
    {
        public string magic { get; set; } = "";
        public int secSize { get; set; }
        public FontType fntType { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int ascent { get; set; }
        public int lineFeed { get; set; }
        public int invalidCharSym { get; set; }
        public int defLeftDist { get; set; }
        public int defGlyphWidth { get; set; }
        public int defCharWidth { get; set; }
        public GlyphCode charEncoding { get; set; }
        public int tglpOffset { get; set; }
        public int cwdhOffset { get; set; }
        public int cmapOffset { get; set; }

    }
    public class TGLP
    {
        public string magic { get; set; } = "";
        public int secSize { get; set; }
        public int cellWidth { get; set; }
        public int cellHeight { get; set; }
        public int sheetNum { get; set; }
        public int maxCharWidth { get; set; }
        public int sheetSize { get; set; }
        public int baseLinePos { get; set; }
        public int texFormat { get; set; }
        public int cellPerRow { get; set; }
        public int cellPerCol { get; set; }
        public int texWidth { get; set; }
        public int texHeight { get; set; }
        public int texDataOffset { get; set; }
    }
    public class CWDH
    {
        public string magic { get; set; } = "";
        public int secSize { get; set; }
        public int firstCharIndex { get; set; }
        public int lastCharIndex { get; set; }
        public int widthDataSize { get; set; }
        public CharWidth[] charWidths { get; set; } = Array.Empty<CharWidth>();
    }
    public class CMAP
    {
        public string magic { get; set; } = "";
        public int secSize { get; set; }
        public int firstCharCode { get; set; }
        public int lastCharCode { get; set; }
        public MapMethod mapType { get; set; }
        public int mapDataSize { get; set; }
        public CharMap[] charMaps { get; set; } = Array.Empty<CharMap>();
    }
    public class CharMap
    {
        public int charCode { get; set; } = 0;
        public int index { get; set; } = 0;
    }
    public class CharWidth
    {
        public int leftDistance { get; set; }
        public int charWidth { get; set; }
        public int glyphWidth { get; set; }
    }
    public class KernSecondWord
    {
        public int charCode_Second { get; set; } = 0;
        public int kernOffset { get; set; } = 0;
    }
    public class Character
    {
        public int charCode { get; set; } = 0;
        public int charWidth { get; set; } = 0;
        public int glyphWidth { get; set; } = 0;
        public int leftDistance { get; set; } = 0;
        public KernSecondWord[] kernWords { get; set; } = Array.Empty<KernSecondWord>();
    }
}