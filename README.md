# WiiUBFFNTtoUnityFNT
 Converts Wii U's BFFNT to Unity's Bitmap Font format.
 
## How to Use
1. Download the release
2. Run the file and select your BFFNT File
   - The converted .fontsettings file will be alongside the BFFNT File.
3. Drag the .fontsettings file into your Unity Project
4. Extract the texture(s) from the BFFNT file using a method from your choosing, create a Texture 2D Array using them.
5. Create a custom shader to sample the Texture2DArray slice by `floor(uv.y);`
6. Create a material with its shader being the one in step 5, and the Texture2DArray being the one in step 5.
7. Select the font in Unity and drag the Material into the `Default Material` slot.
8. Done!
You can now use this font anywhere in your project.

## Notice
- The FontSettings file will only display the Main Texture of the material without shader effects.
If you want the shader to modify the text, add the Material into the `Material` slot of the Legacy Text GameObject.
- Unity may spit an error if it can't assign the `_MainTex` property to a Texture2DArray, in such a case create a dummy `_MainTex` propery and use a different name for the texture array.

## Credits
- kinnay for Nintendo-File-Format site used for Documentation.
- KillZXGaming for additional Documentation within Toolbox.
- Snowberry Software for helpful Binary IO Libraries and extentions.

## TODO
- Handle multiple texture sheets (Might have to combine them extenally) // Done. Create 2D Texture Array and custom shader.
- Handle multiple character maps. (Implemented but untested with current resources)
