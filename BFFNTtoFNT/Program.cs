using BFFNTtoFNT.Classes;

namespace BFFNTtoFNT
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Open the BFFNT File.
            string ffnt_filedir;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.Title = "Open BFFNT";
                ofd.Filter = "Binary caFe FoNT files (.bffnt)|*.bffnt";
                if(ofd.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                ffnt_filedir = ofd.FileName;
                
            }

            //Read and Parse the BFFNT File.
            BFFNT wiiU_file = new BFFNT(ffnt_filedir);
            wiiU_file.ReadBFFNT();



            //Get CharacterRect's for Unity Font from BFFNT. 
            CharacterRect[] getCharRects()
            {
                int length = wiiU_file.charInfo.Length;
                //int length = wiiU_file.texInfo.glyphsPerCol * wiiU_file.texInfo.glyphsPerRow;

                CharacterRect[] outChars = new CharacterRect[length];
                float xOffset;
                float glyphWidth;
                float glyphHeight;
                float sheetWidth;
                float sheetHeight;
                float charWidth;
                int glyphsPerRow;
                int glyphsPerCol;
                int ascent;
                int baselinepos;


                int charAdvance;

                for (int i = 0; i < length; i++)
                {
                    glyphWidth = wiiU_file.texInfo.glyphWidth + 1;
                    glyphHeight = wiiU_file.texInfo.glyphHeight;
                    xOffset = wiiU_file.charInfo[i].xOffset;
                    charWidth = wiiU_file.charInfo[i].charWidth;
                    charAdvance = wiiU_file.charInfo[i].charAdvance;
                    sheetWidth = wiiU_file.texInfo.sheetWidth;
                    sheetHeight = wiiU_file.texInfo.sheetHeight;
                    glyphsPerRow = wiiU_file.texInfo.glyphsPerRow;
                    glyphsPerCol = wiiU_file.texInfo.glyphsPerCol;
                    ascent = wiiU_file.fontInfo.ascent;
                    baselinepos = wiiU_file.texInfo.baseLinePos;

                    //the grid is offset from the top left by 1 px. 
                    //the glyphHeight is 1+glyphHeight

                    int xPos = i % glyphsPerRow;//Which cell in the row. 
                    int yPos = i / glyphsPerRow;//Which row the cell is in.

                    int sheetNo = i / (glyphsPerRow * glyphsPerCol); //Which sheet the glyph is in. 
                    int yOffset = (int)sheetHeight - (glyphsPerCol * (int)(glyphHeight+1)); //empty space between end of glyphs and end of texture. 

                    float uvX = ((glyphWidth * xPos) + 1) / sheetWidth; // Initial uvX calculation
                    float uvY = ((glyphHeight + 1) * (yPos+1) + 1) / sheetHeight;//Iniital uvY calculation

                    uvY += (yOffset * sheetNo)/sheetHeight;//Move the UV's to the nearest sheet.(integer values)

                    float unity_uvY = (sheetNo + 1) - uvY + sheetNo; //Remap to match unity's bottom-left origin from WiiU's top-right. 

                    outChars[i] = new CharacterRect
                    {
                        serializedVersion = 2,
                        index = wiiU_file.charInfo[i].charCode,
                        uv = new UV
                        {
                            serializedVersion = 2,
                            x = ((glyphWidth * xPos) + 1) / sheetWidth,
                            y = unity_uvY,

                            width = charWidth / sheetWidth,
                            height = glyphHeight / sheetHeight,
                        },
                        vert = new VERT
                        {
                            serializedVersion = 2,
                            x = xOffset,
                            y = baselinepos - ascent,
                            width = charWidth,
                            height = -glyphHeight
                        },
                        advance = (int)(charAdvance),
                        flipped = 0
                    };
                }

                return outChars;
            }

            //Get the KerningValues for Unity Font from BFFNT. 
            KerningValues[] getKerningValues()
            {
                List<KerningValues> kerningValues = new List<KerningValues>();
                foreach (Character character in wiiU_file.charInfo)
                {
                    foreach (KernSecondWord kernSecond in character.kernWords)
                    {

                        KerningValues thisKern = new KerningValues
                        {
                            first = new Dictionary<int, int>
                            {
                                {character.charCode, kernSecond.charCode_Second}
                            },
                            second = kernSecond.kernOffset,
                        };
                        kerningValues.Add(thisKern);
                    }
                }
                return kerningValues.ToArray();
            }

            //Create the UnityFont.
            var Font = new FontSettings
            {
                Font = new FontYAML
                {
                    m_OjectHideFlags = 0,
                    m_CorrespondingSourceObject = new Dictionary<string, string>
                    {
                        { "fileID", "0" },
                    },
                    m_PrefabInstance = new Dictionary<string, string>
                    {
                        { "fileID", "0" },
                    },
                    m_PrefabAsset = new Dictionary<string, string>
                    {
                        { "fileID", "0" },
                    },
                    m_Name = Path.GetFileNameWithoutExtension(wiiU_file.fileDirectory),
                    serializedVersion = 5,
                    m_LineSpacing = wiiU_file.fontInfo.height,
                    m_DefaultMaterial = new Dictionary<string, string>
                    {
                        { "fileID", "0" },
                    },
                    m_FontSize = wiiU_file.texInfo.glyphHeight,
                    m_Texture = new Dictionary<string, string>
                    {
                        { "fileID", "0" },
                    },
                    m_AsciiStartOffset = 0,
                    m_Tracking = 1,
                    m_CharacterSpacing = 0,
                    m_CharacterPadding = 1,
                    m_ConvertCase = 0,
                    m_CharacterRects = getCharRects(),
                    m_KerningValues = getKerningValues(),
                    m_PixelScale = 1f,
                    m_Ascent = 0f,
                    m_Descent = 0f,
                    m_DefaultStyle = 0,
                    m_FontNames = new string[]
                    {
                        Path.GetFileNameWithoutExtension(wiiU_file.fileDirectory),
                    },
                    m_FallbackFonts = new string[0],
                    m_FontRenderingMode = 0,
                    m_UseLegacyBoundsCalculation = 0,
                    m_ShouldRoundAdvanceValue = 1,
                    //TODO: Add material information here. 
                },
            };

            //Write the UnityFont
            string fnt_dileDir = ffnt_filedir.Replace(".bffnt", ".fontsettings");
            WriteFNT write = new WriteFNT(Font, fnt_dileDir);
            MessageBox.Show("File converted Sucessfully \n You must extract the textures on your own.");
        }
    }
}