using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace bsp
{
    public class BSPFace
    {

        public int faceId;
        public UInt16 plane_id; // Plane the face is parallel to
        public UInt16 side;     // Set if different normals orientation
        public UInt32 firstEdgeIndex; // Index of the first surfedge
        public UInt16 numberEdges; // Number of consecutive surfedges
        public UInt16 texinfo_id; // Index of the texture info structure
        public byte[] styles;   // Specify lighting styles
        public UInt32 lightmapOffset; // Offsets into the raw lightmap data
        public Transform transform;
        public RendererCache renderer;
        public Leaf leaf;
        public BSPModel model;
        public Vector3[] vertex;
        public int[] triangles;
        public Vector2[] uv;
        public Vector2[] uv2;
        public int lightMapW;
        public int lightMapH;


        /// <summary>
        /// LUMP_FACES(7) MAX_MAP_FACES 65535
        ///The first number of this data structure is an index into the planes lump giving a plane which is parallel to this face 
        /// (meaning they share the same normal). The second value may be seen as a boolean.
        /// If nPlaneSide equals 0, then the normal vector of this face equals the one of the parallel plane exactly. 
        /// Otherwise, the normal of the plane has to be multiplied by -1 to point into the right direction. 
        /// Afterwards we have an index into the surfedges lump, as well as the count of consecutive surfedges from that position.
        /// Furthermore there is an index into the texture info lump, which is used to find the BSPTEXINFO structure needed to calculate the texture coordinates for this face.
        /// Afterwards, there are four bytes giving some lighting information (partly used by the renderer to hide sky surfaces). 
        /// Finally we have an offset in byes giving the beginning of the binary lightmap data of this face in the lighting lump.
        /// </summary>
        /// 
        /// <param name="PlaneID">Plane the face is parallel to.</param>
        /// <param name="Side">Set if different normals orientation.</param>
        /// <param name="firstEdge">Index of the first surfedge.</param>
        /// <param name="NumberEdges">Number of consecutive surfedges.</param>
        /// <param name="texInfoID">Index of the texture info structure.</param>
        /// <param name="Styles">Specify lighting styles.</param>
        /// <param name="LightMapOffSet">Offsets into the raw lightmap data.</param>

        public BSPFace(UInt16 PlaneID, UInt16 Side, UInt32 firstEdge, UInt16 NumberEdges, UInt16 texInfoID, byte[] Styles, UInt32 LightMapOffSet, int offset)
        {
            plane_id = PlaneID;
            side = Side;
            firstEdgeIndex = firstEdge;
            numberEdges = NumberEdges;
            texinfo_id = texInfoID;
            styles = Styles;
            lightmapOffset = LightMapOffSet;
            if (LightMapOffSet > offset) lightmapOffset = 0;//CANT FIND WHY THIS IS
           




        }
        public BSPFace()
        {

        }
        public override string ToString()
        {
            return "plane_id " + plane_id + " EdgeListIndex: " + firstEdgeIndex + " NumEdges: " + numberEdges + " TexinfoIndex: " + texinfo_id + "\r\n";
        }
    }
}

