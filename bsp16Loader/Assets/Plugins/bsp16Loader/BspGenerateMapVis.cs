using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
namespace bsp
{
    public class BspGenerateMapVis : MonoBehaviour
    {
        public Texture2D missingtexture;
        string[] hide = new string[] { "sky", "aaatrigger", "black" };
        public int currentLeaf;
        public int model1tLeaf;
        public bool renderlights = true;
        private BSP30Map map;
        private int faceCount = 0;
        private GameObject[][] leafRoots;

        public Transform player;
        private bool lockpvs = false;
        private int lastpvs = 0;
        public bool RenderAllFaces = false;
        public bool combine = true;

        public void Load(BSP30Map map)
        {
            this.map = map;
            GenerateVisArrays();
            GenerateVisObjects();
            transform.localScale = scale * Vector3.one;
            RenderPVS(0);
            if (combine)
                StaticBatchingUtility.Combine(gameObject);
        }
        public const float scale = 0.03f;
        void Update2()
        {
            if (!lockpvs)
            {
                int pvs = WalkBSP();
                if (pvs != lastpvs)
                    currentLeaf = pvs;
                RenderPVS(pvs);
                lastpvs = pvs;
            }
            if (RenderAllFaces)
                RenderPVS(0);
            if (Input.GetKeyDown(KeyCode.Z))
            {
                lockpvs = !lockpvs;
                Debug.Log("PVS lock: " + lockpvs);
            }
        }
        private void RenderPVS(int leaf)
        {
            for (int i = 0; i < leafRoots.Length; i++)
                foreach (GameObject go in leafRoots[i])
                    go.GetComponent<Renderer>().enabled = false;
            if (leaf == 0)
            {
                for (int i = 0; i < leafRoots.Length; i++)
                    foreach (GameObject go in leafRoots[i])
                        go.GetComponent<Renderer>().enabled = true;
                return;
            }
            for (int j = 0; j < map.leafLump.leafs[leaf].pvs.Length; j++)
                if (map.leafLump.leafs[leaf].pvs[j] == true)
                    foreach (GameObject go in leafRoots[j + 1])
                        go.GetComponent<Renderer>().enabled = true;
        }
        private int BSPlookup(int node)
        {
            var b = map.planeLump.planes[map.nodeLump.nodes[node].planeNum].plane.GetSide(player.position);
            return map.nodeLump.nodes[node].children[b ? 1 : 0];
        }
        private int WalkBSP(int headnode = 0)
        {
            int child = BSPlookup(headnode);

            while (child >= 0)
                child = BSPlookup(child);

            child = -(child + 1);
            return child;
        }
        void GenerateVisArrays()
        {
            leafRoots = new GameObject[map.leafLump.numLeafs][];
            for (int i = 0; i < map.leafLump.numLeafs; i++)
                leafRoots[i] = new GameObject[map.leafLump.leafs[i].NumMarkSurfaces];
        }
        void GenerateVisObjects()
        {
            for (int i = 0; i < map.leafLump.numLeafs; i++)
                for (int j = 0; j < map.leafLump.leafs[i].NumMarkSurfaces; j++)
                {
                    leafRoots[i][j] = GenerateFaceObject(map.facesLump.faces[map.markSurfacesLump.markSurfaces[map.leafLump.leafs[i].FirstMarkSurface + j]]);
                    faceCount++;
                }
        }
        public Material mat;
        public Material matTrans;
        public Dictionary<uint, Material> mt = new Dictionary<uint, Material>();
        GameObject GenerateFaceObject(BSPFace face)
        {

#if !console
            GameObject faceObject =  new GameObject("BSPface " + faceCount.ToString());
            face.transform = faceObject.transform;
            faceObject.transform.parent = gameObject.transform;
            Mesh faceMesh = new Mesh();
            faceMesh.name = "BSPmesh";
            Vector3[] verts = new Vector3[face.numberEdges];
            int edgestep = (int)face.firstEdgeIndex;
            for (int i = 0; i < face.numberEdges; i++)
            {
                var edge = map.edgeLump.edges[Mathf.Abs(map.edgeLump.SURFEDGES[edgestep])];
                var vert = map.edgeLump.SURFEDGES[face.firstEdgeIndex + i] < 0 ? edge.vert1 : edge.vert2;
                verts[i] = map.vertLump.ConvertScaleVertex(map.vertLump.verts[vert]);
                edgestep++;
            }
            int[] tris = new int[(face.numberEdges - 2) * 3];
            int tristep = 1;
            for (int i = 1; i < verts.Length - 1; i++)
            {
                tris[tristep - 1] = 0;
                tris[tristep] = i;
                tris[tristep + 1] = i + 1;
                tristep += 3;
            }
            var bspTexInfo = map.texinfoLump.texinfo[face.texinfo_id];
            var bspMipTexture = map.miptexLump[bspTexInfo.miptex];


            float scales = bspMipTexture.width;
            float scalet = bspMipTexture.height;
            Vector2[] uvs = new Vector2[face.numberEdges];
            for (int i = 0; i < face.numberEdges; i++)
                uvs[i] = new Vector2((Vector3.Dot(verts[i], bspTexInfo.vec3s) + bspTexInfo.offs) / scales, (Vector3.Dot(verts[i], bspTexInfo.vec3t) + bspTexInfo.offt) / scalet);
            faceMesh.vertices = verts;
            faceMesh.triangles = tris;
            faceMesh.uv = uvs;
            faceMesh.RecalculateNormals();
            faceObject.AddComponent<MeshFilter>();
            faceObject.GetComponent<MeshFilter>().mesh = faceMesh;
            var renderer = faceObject.AddComponent<MeshRenderer>();


            if (face.texinfo_id >= 0 && renderlights && face.lightmapOffset < map.lightlump.Length)
            {
                renderer.sharedMaterial = new Material(mat);
                RenderLights(face, verts, faceMesh, renderer);
            }
            else
            {
                if (bspMipTexture.texture != null)
                {
                    Material m;
                    if (!mt.TryGetValue(bspTexInfo.miptex, out m))
                        m = mt[bspTexInfo.miptex] = new Material(bspMipTexture.texture.format == TextureFormat.ARGB32 ? matTrans : mat);
                    m.mainTexture = bspMipTexture.texture;
                    m.mainTexture.name = bspMipTexture.name;
                    renderer.sharedMaterial = m;
                }
            }



            if (combine)
                faceObject.isStatic = true;


            if (hide.Any(a => string.Equals(bspMipTexture.name, a, StringComparison.OrdinalIgnoreCase)))
                faceObject.SetActive(false);
            else
            {
                faceObject.AddComponent<MeshCollider>();
                faceObject.layer = Layer.Level;
            }
            return faceObject;
#else 
            return null;
#endif
        }
        private void RenderLights(BSPFace face, Vector3[] verts, Mesh faceMesh, Renderer faceObjectRenderer)
        {
#if !console
            Material bspMaterial = faceObjectRenderer.sharedMaterial;

            Vertex[] pVertexList = new Vertex[verts.Length];
            float fUMin = 100000.0f;
            float fUMax = -10000.0f;
            float fVMin = 100000.0f;
            float fVMax = -10000.0f;
            var bspTexInfo = map.texinfoLump.texinfo[face.texinfo_id];
            float pMipTexheight = map.miptexLump[bspTexInfo.miptex].height;
            float pMipTexwidth = map.miptexLump[bspTexInfo.miptex].width;
            for (int nEdge = 0; nEdge < verts.Length; nEdge++)
            {
                Vertex vertex = new Vertex(verts[nEdge]);
                vertex.u = verts[nEdge].x * bspTexInfo.vec3s.x + verts[nEdge].y * bspTexInfo.vec3s.y + verts[nEdge].z * bspTexInfo.vec3s.z + bspTexInfo.offs;
                vertex.v = verts[nEdge].x * bspTexInfo.vec3t.x + verts[nEdge].y * bspTexInfo.vec3t.y + verts[nEdge].z * bspTexInfo.vec3t.z + bspTexInfo.offt;
                vertex.u /= pMipTexwidth;
                vertex.v /= pMipTexheight;
                vertex.lu = vertex.u;
                vertex.lv = vertex.v;
                fUMin = (vertex.u < fUMin) ? vertex.u : fUMin;
                fUMax = (vertex.u > fUMax) ? vertex.u : fUMax;
                fVMin = (vertex.v < fVMin) ? vertex.v : fVMin;
                fVMax = (vertex.v > fVMax) ? vertex.v : fVMax;
                pVertexList[nEdge] = vertex;
            }
            int lightMapWidth = (int)(Mathf.Ceil((fUMax * pMipTexwidth) / 16.0f) - Mathf.Floor((fUMin * pMipTexwidth) / 16.0f) + 1.0f);
            int lightMapHeight = (int)(Mathf.Ceil((fVMax * pMipTexheight) / 16.0f) - Mathf.Floor((fVMin * pMipTexheight) / 16.0f) + 1.0f);
            float cZeroTolerance = 1e-06f;
            float fUDel = (fUMax - fUMin);
            if (fUDel > cZeroTolerance)
                fUDel = 1.0f / fUDel;
            else
                fUDel = 1.0f;
            float fVDel = (fVMax - fVMin);
            if (fVDel > cZeroTolerance)
                fVDel = 1.0f / fVDel;
            else
                fVDel = 1.0f;
            for (int n = 0; n < pVertexList.Length; n++)
            {
                (pVertexList)[n].lu = ((pVertexList)[n].lu - fUMin) * fUDel;
                (pVertexList)[n].lv = ((pVertexList)[n].lv - fVMin) * fVDel;
            }

            Texture2D lightTex = new Texture2D(lightMapWidth, lightMapHeight);
            Color[] colourarray = new Color[lightMapWidth * lightMapHeight];
            int tempCount = (int)face.lightmapOffset;
            for (int k = 0; k < lightMapWidth * lightMapHeight; k++)
            {
                if (tempCount + 3 > map.lightlump.Length) break;
                colourarray[k] = new Color32(map.lightlump[tempCount], map.lightlump[tempCount + 1], map.lightlump[tempCount + 2], 100);
                tempCount += 3;
            }
            lightTex.SetPixels(colourarray);
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();
            var lvs = new List<Vector2>(new Vector2[pVertexList.Length]);
            for (int a = 0; a < pVertexList.Length; a++)
                lvs[a] = new Vector2(pVertexList[a].lu, pVertexList[a].lv);
            faceMesh.SetUVs(1, lvs);
            bspMaterial.SetTexture("_LightMap", lightTex);

            if (map.miptexLump[bspTexInfo.miptex].texture == null)
            {
                bspMaterial.mainTexture = missingtexture;
            }
            else
            {
                bspMaterial.mainTexture = map.miptexLump[bspTexInfo.miptex].texture;
                bspMaterial.mainTexture.name = map.miptexLump[bspTexInfo.miptex].name;
            }
            bspMaterial.mainTexture.filterMode = FilterMode.Bilinear;
            faceObjectRenderer.sharedMaterial = bspMaterial;
#endif
        }
    }
}