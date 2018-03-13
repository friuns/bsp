using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
namespace bsp
{
    public class BspGenerateMapVis : MonoBehaviour
    {
        public Texture2D missingtexture;
        public int currentLeaf;
        public int model1tLeaf;
        public bool renderlights = true;
        private BSP30Map map;
        private int faceCount = 0;
        private GameObject[][] leafRoots;
        public Transform[] faces;
        public Transform player;
        private bool lockpvs = false;
        private int lastpvs = 0;
        public bool RenderAllFaces = false;
        public bool combine = true;
        public void Awake()
        {
            mt = new MyDictionary<uint, Material>(() => new Material(mat));
        }
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
                {
                    foreach (GameObject go in leafRoots[i])
                    {
                        go.GetComponent<Renderer>().enabled = true;
                        if (go.GetComponent<Renderer>().sharedMaterial.mainTexture.name == "sky")
                            go.GetComponent<Renderer>().enabled = false;
                        else
                        {
                            go.AddComponent<MeshCollider>();
                            go.layer = Layer.Level;
                        }
                    }
                }
                return;
            }
            for (int j = 0; j < map.leafLump.leafs[leaf].pvs.Length; j++)
            {
                if (map.leafLump.leafs[leaf].pvs[j] == true)
                {
                    foreach (GameObject go in leafRoots[j + 1])
                    {
                        go.GetComponent<Renderer>().enabled = true;
                        if (go.GetComponent<Renderer>().sharedMaterial.mainTexture.name == "sky")
                            go.GetComponent<Renderer>().enabled = false;
                    }
                }
            }
        }
        private int BSPlookup(int node)
        {
            int child;
            if (!map.planeLump.planes[map.nodeLump.nodes[node].planeNum].plane.GetSide(player.position))
            {
                child = map.nodeLump.nodes[node].children[0];
            }
            else
            {
                child = map.nodeLump.nodes[node].children[1];
            }
            return child;
        }
        private int WalkBSP(int headnode = 0)
        {
            int child = BSPlookup(headnode);
            while (child >= 0)
            {
                child = BSPlookup(child);
            }
            child = -(child + 1);
            return child;
        }
        void GenerateVisArrays()
        {
            leafRoots = new GameObject[map.leafLump.numLeafs][];
            for (int i = 0; i < map.leafLump.numLeafs; i++)
            {
                leafRoots[i] = new GameObject[map.leafLump.leafs[i].NumMarkSurfaces];
            }
        }
        void GenerateVisObjects()
        {
            for (int i = 0; i < map.leafLump.numLeafs; i++)
            {
                for (int j = 0; j < map.leafLump.leafs[i].NumMarkSurfaces; j++)
                {
                    leafRoots[i][j] = GenerateFaceObject(map.facesLump.faces[map.markSurfacesLump.markSurfaces[map.leafLump.leafs[i].FirstMarkSurface + j]]);
                    faceCount++;
                }
            }
        }
        public Material mat;
        public MyDictionary<uint, Material> mt;
        GameObject GenerateFaceObject(BSPFace face)
        {
            GameObject faceObject = face.gameObject = new GameObject("BSPface " + faceCount.ToString());
            faceObject.transform.parent = gameObject.transform;
            Mesh faceMesh = new Mesh();
            faceMesh.name = "BSPmesh";
            Vector3[] verts = new Vector3[face.numberEdges];
            int edgestep = (int)face.firstEdgeIndex;
            for (int i = 0; i < face.numberEdges; i++)
            {
                if (map.edgeLump.SURFEDGES[face.firstEdgeIndex + i] < 0)
                    verts[i] = map.vertLump.ConvertScaleVertex(map.vertLump.verts[map.edgeLump.edges[Mathf.Abs(map.edgeLump.SURFEDGES[edgestep])].vert1]);
                else
                    verts[i] = map.vertLump.ConvertScaleVertex(map.vertLump.verts[map.edgeLump.edges[map.edgeLump.SURFEDGES[edgestep]].vert2]);
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
            float scales = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].width;
            float scalet = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].height;
            Vector2[] uvs = new Vector2[face.numberEdges];
            for (int i = 0; i < face.numberEdges; i++)
                uvs[i] = new Vector2((Vector3.Dot(verts[i], map.texinfoLump.texinfo[face.texinfo_id].vec3s) + map.texinfoLump.texinfo[face.texinfo_id].offs) / scales, (Vector3.Dot(verts[i], map.texinfoLump.texinfo[face.texinfo_id].vec3t) + map.texinfoLump.texinfo[face.texinfo_id].offt) / scalet);
            faceMesh.vertices = verts;
            faceMesh.triangles = tris;
            faceMesh.uv = uvs;
            faceMesh.RecalculateNormals();
            faceObject.AddComponent<MeshFilter>();
            faceObject.GetComponent<MeshFilter>().mesh = faceMesh;
            faceObject.AddComponent<MeshRenderer>();
            var faceObjectRenderer = faceObject.GetComponent<Renderer>();
            faceObjectRenderer.sharedMaterial = renderlights ? new Material(mat) : mt[map.texinfoLump.texinfo[face.texinfo_id].miptex];
            
            if (face.texinfo_id >= 0 && renderlights && face.lightmapOffset < map.lightlump.Length)
            {
                RenderLights(face, verts, faceMesh, faceObjectRenderer);
            }
            else
            {
                if (map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].texture == null)
                {
                    faceObjectRenderer.sharedMaterial.mainTexture = missingtexture;
                    Debug.LogError(map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].name + "not loaded");
                }
                else
                {
                    faceObjectRenderer.sharedMaterial.mainTexture = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].texture;
                    faceObjectRenderer.sharedMaterial.mainTexture.name = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].name;
                }
            }
            if (combine)
                faceObject.isStatic = true;
            return faceObject;
        }
        private void RenderLights(BSPFace face, Vector3[] verts, Mesh faceMesh, Renderer faceObjectRenderer)
        {
            Material bspMaterial = faceObjectRenderer.sharedMaterial;

            Vertex[] pVertexList = new Vertex[verts.Length];
            float fUMin = 100000.0f;
            float fUMax = -10000.0f;
            float fVMin = 100000.0f;
            float fVMax = -10000.0f;
            float pMipTexheight = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].height;
            float pMipTexwidth = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].width;
            for (int nEdge = 0; nEdge < verts.Length; nEdge++)
            {
                Vertex vertex = new Vertex(verts[nEdge]);
                vertex.u = verts[nEdge].x*map.texinfoLump.texinfo[face.texinfo_id].vec3s.x + verts[nEdge].y*map.texinfoLump.texinfo[face.texinfo_id].vec3s.y + verts[nEdge].z*map.texinfoLump.texinfo[face.texinfo_id].vec3s.z + map.texinfoLump.texinfo[face.texinfo_id].offs;
                vertex.v = verts[nEdge].x*map.texinfoLump.texinfo[face.texinfo_id].vec3t.x + verts[nEdge].y*map.texinfoLump.texinfo[face.texinfo_id].vec3t.y + verts[nEdge].z*map.texinfoLump.texinfo[face.texinfo_id].vec3t.z + map.texinfoLump.texinfo[face.texinfo_id].offt;
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
            int lightMapWidth = (int) (Mathf.Ceil((fUMax*pMipTexwidth)/16.0f) - Mathf.Floor((fUMin*pMipTexwidth)/16.0f) + 1.0f);
            int lightMapHeight = (int) (Mathf.Ceil((fVMax*pMipTexheight)/16.0f) - Mathf.Floor((fVMin*pMipTexheight)/16.0f) + 1.0f);
            float cZeroTolerance = 1e-06f;
            float fUDel = (fUMax - fUMin);
            if (fUDel > cZeroTolerance)
                fUDel = 1.0f/fUDel;
            else
                fUDel = 1.0f;
            float fVDel = (fVMax - fVMin);
            if (fVDel > cZeroTolerance)
                fVDel = 1.0f/fVDel;
            else
                fVDel = 1.0f;
            for (int n = 0; n < pVertexList.Length; n++)
            {
                (pVertexList)[n].lu = ((pVertexList)[n].lu - fUMin)*fUDel;
                (pVertexList)[n].lv = ((pVertexList)[n].lv - fVMin)*fVDel;
            }

            Texture2D lightTex = new Texture2D(lightMapWidth, lightMapHeight);
            Color[] colourarray = new Color[lightMapWidth*lightMapHeight];
            int tempCount = (int) face.lightmapOffset;
            for (int k = 0; k < lightMapWidth*lightMapHeight; k++)
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

            if (map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].texture == null)
            {
                bspMaterial.mainTexture = missingtexture;
            }
            else
            {
                bspMaterial.mainTexture = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].texture;
                bspMaterial.mainTexture.name = map.miptexLump[map.texinfoLump.texinfo[face.texinfo_id].miptex].name;
            }
            bspMaterial.mainTexture.filterMode = FilterMode.Bilinear;
            faceObjectRenderer.sharedMaterial = bspMaterial;
        }
    }
}