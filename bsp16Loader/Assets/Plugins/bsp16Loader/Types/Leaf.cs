#region

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#endregion

namespace bsp
{
    public class Leaf
    {
        public byte[] AmbientLevels;
        public int ContentsType;
        public int FirstMarkSurface;
        public Vector3 maxs;
        public Vector3 mins;
        public int NumMarkSurfaces;
        public List<Leaf> pvsList;
        public MipModel2 mip;
        public Renderer r;
        public bool used;
        public int VisOffset;
        public BSPFace[] faces;

        public Leaf(int type, int vislist, Vector3 Mins, Vector3 Maxs, ushort lface_index, ushort num_lfaces, byte[] ambientLevels)
        {
            ContentsType = type;
            VisOffset = vislist;
            mins = Mins;
            maxs = Maxs;
            FirstMarkSurface = lface_index;
            NumMarkSurfaces = num_lfaces;
            AmbientLevels = ambientLevels;
            used = false;
            pvsList = new List<Leaf>();
            faces = new BSPFace[0];
        }


        public string print()
        {

            return " Type: " + ContentsType + " Vislist: " + VisOffset + " Mins/Maxs: " + mins + " / " + maxs;
        }
    }
}