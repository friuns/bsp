using System;
using UnityEngine;

namespace bsp
{
    public class BSPMipTexture
    {
        public string name;
        public Int32 width;
        public Int32 height;
        public UInt32[] offset;
        public Texture2D texture;

        public BSPMipTexture(string Name, UInt32 Width, UInt32 Height, UInt32[] offset)
        {
            //this.name = RemoveControlCharacters(Name);
            this.name = Name;
            this.width = (int)Width;
            this.height = (int)Height;
            this.offset = offset;
        }

        public int PixelCount()
        {
            return (int)(width * height);
        }
        public override string ToString()
        {
            return name;
        }
        //using bytes because read chars can move stream position depending on the text its reading
        //this removes  ascii control characters that mess up string tests when loading from wad

    }

#if console

    public class Texture2D 
    {
        public FilterMode filterMode;

        public Texture2D(int Width, int Height)
        {
        }

        public Texture2D(int Width, int Height, TextureFormat rGB24, bool v) : this(Width, Height)
        {
        }

        public void SetPixels(Color[] Pixels)
        {

        }
        public void Apply()
        {
        }
        public void SetPixels32(Color32[] Colors)
        {
        }
    }
#endif
}


