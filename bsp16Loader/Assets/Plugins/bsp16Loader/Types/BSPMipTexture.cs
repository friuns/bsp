using System;
using System.Collections.Generic;
using UnityEngine;

namespace bsp
{
   
    public class MipModel2
    {
        // public MipModel(int verts, int tris, int uvs)
        // {
        //     
        // }
        public string name;
        // public Texture2D  texture {get {return mip.texture; } }
        // public BSPMipTexture mip;
        public Mesh mesh = new Mesh();
        public Material material;
        public ArrayOffset<Vector3> verts;
        public ArrayOffset<int> tris     ;
        public ArrayOffset<Vector2> uvs  ;
        public ArrayOffset<Vector2> uvs2  ;
        public ArrayOffset<Vector4> uvs3  ;
        // public ArrayOffset<BSPFace> faces;        
        public int vertsCount;
        public int faceCount;
        public int trianglesCount;
        // public int uvsCount;
        public void Init()
        {
            verts = new ArrayOffset<Vector3>(vertsCount);
            tris = new ArrayOffset<int>(trianglesCount); //vertsCount * 3
            uvs = new ArrayOffset<Vector2>(vertsCount);
            uvs2 = new ArrayOffset<Vector2>(vertsCount);
            uvs3 = new ArrayOffset<Vector4>(vertsCount);
            // faces = new BSPFace[faceCount];
        }
    }
    public class BSPMipTexture
    {
        static string[] disable = new string[] { "sky" };
        static string[] hide = new string[] { "aaatrigger", "black", "white" };
        
        public string name;
        public Int32 width;
        public Int32 height;
        public UInt32[] offset;
        public Texture2D texture;
        public bool handled;
        public bool disabled;
        public BSPMipTexture(string Name, UInt32 Width, UInt32 Height, UInt32[] offset)
        {
            
            //this.name = RemoveControlCharacters(Name);
            this.name = Name;
            disabled = disable.Any(a => string.Equals(name, a, StringComparison.OrdinalIgnoreCase));
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


