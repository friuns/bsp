using System;
using System.Collections.Generic;
using UnityEngine;

namespace bsp
{
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
    public BSPMipTexture(string Name, UInt32 Width, UInt32 Height, UInt32[] offset)
    {

        //this.name = RemoveControlCharacters(Name);
        this.name = Name;
        disabled = disable.Any(a => string.Equals(name, a, StringComparison.OrdinalIgnoreCase));
        this.width = (int) Width;
        this.height = (int) Height;
        this.offset = offset;
    }

    public int PixelCount()
    {
        return (int) (width * height);
    }
    public override string ToString()
    {
        return name;
    }
    //using bytes because read chars can move stream position depending on the text its reading
    //this removes  ascii control characters that mess up string tests when loading from wad



}
}


