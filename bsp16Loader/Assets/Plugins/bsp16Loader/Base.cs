using System;
using UnityEngine;

namespace bsp
{
public class Base:MonoBehaviour
{
    public static IDisposable ProfilePrint(string readentities)
    {
        return null;
    }
    public static class settings
    {
        public const bool disablePvs = true;
    }
    public static BspGenerateMapVis _BspGenerateMapVis;
}

}