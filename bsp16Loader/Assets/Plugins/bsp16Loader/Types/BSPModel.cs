#pragma warning disable 414
using System;
using System.Collections.Generic;
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

        public Int32 numLeafs;// number of BSP leaves VisLeaf
        public Int32 indexOfFirstFace, numberOfFaces;// Index and count into faces lump
        public Bounds bounds = new Bounds();
        public Vector3 pos;
        public List<Renderer> renders = new List<Renderer>();
        // public CombinedModel combined;

        public int node { get { return nodes[0]; } }
        public BSPModel()
        {
        }
        public BSPModel(Vector3 mins, Vector3 maxs, Vector3 origin, int[] Nodes, int NumLeafs, Int32 firstFace, Int32 numFaces)
        {
            nMins = mins;
            nMaxs = maxs;
            vOrigin = origin;
            nodes = Nodes;
            numLeafs = NumLeafs;
            indexOfFirstFace = firstFace;
            numberOfFaces = numFaces;
            bounds.SetMinMax(mins, maxs);
            bounds.size = new Vector3(Mathf.Abs(bounds.size.x), Mathf.Abs(bounds.size.y), Mathf.Abs(bounds.size.z));
            pos = bounds.min;
        }
        // public List<BSPMipTexture> mips = new List<BSPMipTexture>(); 
        public void Add(CombinedModel combined,bool trans)
        {
            // mips.AddRange(combined.mips2);
            Renderer r = combined.GenerateMesh(trans);
            renders.Add(r);

            if (combined.mip.solid)
            {
                var c = r.gameObject.AddComponent<MeshCollider>();
                if (combined.mip?.hidden == true)
                {
                    c.convex = true;
                    c.isTrigger = true;
                }
            }
            r.gameObject.layer = Layer.level;
            r.gameObject.name = "Model:" + combined.name;
            
        }
    }
}

