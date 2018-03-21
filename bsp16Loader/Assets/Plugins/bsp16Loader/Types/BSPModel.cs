#pragma warning disable 414
using System;
using System.Text;
using UnityEngine;

namespace bsp
{
    [Serializable]
    public class BSPModel
    {
        public Vector3 nMins;// Defines bounding box
        public Vector3 nMaxs;   // Defines bounding box       

        public Vector3 vOrigin;                  // Coordinates to move the // coordinate system
        public Int32[] nodes;// [0]index of first BSP node, [1]index of the first Clip node, [2]index of the second Clip node, [3]  usually zero

        public Int32 numLeafs;// number of BSP leaves
        public Int32 indexOfFirstFace, numberOfFaces;// Index and count into faces lump
        public Bounds bounds = new Bounds();
        public Vector3 pos;

        public int node { get { return nodes[0]; } }
        public BSPModel()
        {

        }
        public BSPModel(Vector3 mins, Vector3 maxs, Vector3 origin, int[] nodes, int numLeafs, Int32 firstFace, Int32 numFaces)
        {
            this.nMins = mins;
            this.nMaxs = maxs;
            this.vOrigin = origin;
            this.nodes = nodes;
            this.numLeafs = numLeafs;
            this.indexOfFirstFace = firstFace;
            this.numberOfFaces = numFaces;
            bounds.SetMinMax(mins,maxs);
            pos = bounds.min;
        }
    }
}

