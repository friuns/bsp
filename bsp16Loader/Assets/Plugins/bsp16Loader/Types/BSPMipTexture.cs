using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace bsp
{
[Serializable]
public class BSPMipTexture
{
    static string[] disable = new string[] {"sky"};
    static string[] hide = new string[] {"aaatrigger", "black", "white"};

    public string name;
    public Int32 width;
    public Int32 height;
    public UInt32[] offset;
    public Texture2D texture;
    public bool handled;
    public bool disabled;
    public bool hidden;
    public bool transparent;
    public bool solid = true;
    public float soldProb;
    public BSPMipTexture(string Name, UInt32 Width, UInt32 Height, UInt32[] offset)
    {

        //this.name = RemoveControlCharacters(Name);
        name = Name;
        transparent = name.StartsWith("{");
        disabled = disable.Any(a => string.Equals(name, a, StringComparison.OrdinalIgnoreCase));
        hidden = hide.Any(a=> string.Equals(name, a, StringComparison.OrdinalIgnoreCase));
        width = (int) Width;
        height = (int) Height;
        this.offset = offset;
    }
    static Color32[] colourPalette = new Color32[256];
    
    public void ReadTexture(long textureOffset, BinaryReader BinaryReader)
    {
        
        BinaryReader.BaseStream.Position = ((width * height / 64) + offset[3] + textureOffset + 2);
        
        
        for (int j = 0; j < 256; j++)
        {

            Color32 c = new Color32(BinaryReader.ReadByte(), BinaryReader.ReadByte(), BinaryReader.ReadByte(), 255);
            if (j == 255)
                c.r = c.b = c.g = c.a = 0;

            colourPalette[j] = c;
        }

        

        BinaryReader.BaseStream.Position = (textureOffset + offset[0]);
        int NumberOfPixels = height * width;

        Color32[] colour = new Color32[NumberOfPixels];
        for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
        {
            var i = BinaryReader.ReadByte();
            colour[currentPixel] = colourPalette[i];
        }
        texture = new Texture2D(width, height, /*transparent ? TextureFormat.ARGB32 :*/ TextureFormat.RGB24 /*,!transparent*/,false);

        if (colour.Length > 20 && transparent)
        {
            int dd=0;
            for (int x = 0; x < 10; x++)
            {
                var i = colour.Length / 10 * x;
                if (Mathf.Abs((i + 5) % width) < 10)
                    i += width / 2;
                if (i + 30 > colour.Length) break;
                
                byte a = 0;
                for (int j = -5; j < 5; j++)
                {
                    byte b = colour[i + j].a;
                    a = b > a ? b : a;
                }
                dd += a;
            }
            solid = dd / 10f > 200;
            soldProb = dd / 10f;
        }
        texture.SetPixels32(colour);
//                if (transparent)
//                    texture.filterMode = FilterMode.Point;
        texture.Apply();
        // texture.Compress(false);
    }
    
    
    public override string ToString()
    {
        return name;
    }
    //using bytes because read chars can move stream position depending on the text its reading
    //this removes  ascii control characters that mess up string tests when loading from wad



}
}


