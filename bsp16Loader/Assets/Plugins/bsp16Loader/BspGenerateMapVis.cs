using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
namespace bsp
{
    public class BspGenerateMapVis : BSP30Map
    {
        public Texture2D missingtexture;
        string[] hide = new string[] { "sky", "aaatrigger", "black" };
        public bool renderlights = true;
        RendererCache[] allRenderers;
        //private int faceCount = 0;
        //private RendererCache[][] leafRoots;
        public Transform debugTransform;
        public static Vector3 pos;
        private int oldPvs = 0;
        public bool RenderAllFaces = false;

        public bool combine = true;
        public override IEnumerator Load(MemoryStream ms)
        {
            yield return base.Load(ms);
            GenerateVisObjects();
            transform.localScale = scale * Vector3.one;
            RenderPVS(0);
            if (combine)
                StaticBatchingUtility.Combine(gameObject);
            loaded = true;

        }
        bool loaded;
        public const float scale = 0.03f;
        public void Update()
        {
            if (!loaded) return;
            if (debugTransform)
                pos = debugTransform.position;
            if (Input.GetKeyDown(KeyCode.Return))
                RenderAllFaces = !RenderAllFaces;
            int pvs = RenderAllFaces ? 0 : WalkBSP();
            if (pvs != oldPvs)
                RenderPVS(pvs);
            oldPvs = pvs;

        }
        private void RenderPVS(int id)
        {
            if (id == 0) return;
            foreach (var a in leafs)
                for (int i = 0; i < a.renderers.Length; i++)                    
                    a.renderers[i].enabled = id == 0;

            if (id != 0)
                foreach (var a in leafs[id].pvsList)
                    AllTrue(a.renderers);

            RefreshRenderers();

        }
        private void RefreshRenderers()
        {
            foreach (var a in leafs)
                for (int i = 0; i < a.renderers.Length; i++)
                    if (a.renderers[i].enabled != a.renderers[i].oldEnabled)
                        a.renderers[i].oldEnabled = a.renderers[i].renderer.enabled = a.renderers[i].enabled;
        }
        private static void AllTrue(RendererCache[] rendererCaches)
        {
            for (int i = 0; i < rendererCaches.Length; i++)
                rendererCaches[i].enabled = true;
        }

        private int BSPlookup(int node)
        {
            var b = planeLump.planes[nodeLump.nodes[node].planeNum].plane.GetSide(pos / scale);
            return nodeLump.nodes[node].children[b ? 1 : 0];
        }
        private int WalkBSP(int headnode = 0)
        {
            int child = BSPlookup(headnode);

            while (child >= 0)
                child = BSPlookup(child);

            child = -(child + 1);
            return child;
        }

        void GenerateVisObjects()
        {

            foreach (Leaf leaf in leafs)
            {
                leaf.renderers = new RendererCache[leaf.NumMarkSurfaces];
                for (int j = 0; j < leaf.NumMarkSurfaces; j++)
                {
                    BSPFace f = faces[markSurfacesLump.markSurfaces[leaf.FirstMarkSurface + j]];
                    var r = GenerateFaceObject(f);
                    r.enabled = false;
                    leaf.renderers[j] = new RendererCache { renderer = r };
                    //r.name += " " + leaf.print();
                }
            }

        }
        public Material mat;
        public Material matTrans;
        public Dictionary<uint, Material> mt = new Dictionary<uint, Material>();
        Renderer GenerateFaceObject(BSPFace face)
        {

#if !console
            GameObject faceObject = new GameObject("BSPface " + face.faceId);
            
            faceObject.transform.parent = gameObject.transform;
            Mesh faceMesh = new Mesh();
            faceMesh.name = "BSPmesh";
            Vector3[] verts = new Vector3[face.numberEdges];
            int edgestep = (int)face.firstEdgeIndex;
            for (int i = 0; i < face.numberEdges; i++)
            {
                var edge = edgeLump.edges[Mathf.Abs(edgeLump.SURFEDGES[edgestep])];
                var vert = edgeLump.SURFEDGES[face.firstEdgeIndex + i] < 0 ? edge.vert1 : edge.vert2;
                verts[i] = vertLump.ConvertScaleVertex(vertLump.verts[vert]);
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
            var bspTexInfo = texinfoLump.texinfo[face.texinfo_id];
            var bspMipTexture = miptexLump[bspTexInfo.miptex];


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
            var renderer = face.renderer = faceObject.AddComponent<MeshRenderer>();

            if (face.texinfo_id >= 0 && renderlights && face.lightmapOffset < lightlump.Length)
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
            return renderer;
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
            var bspTexInfo = texinfoLump.texinfo[face.texinfo_id];
            float pMipTexheight = miptexLump[bspTexInfo.miptex].height;
            float pMipTexwidth = miptexLump[bspTexInfo.miptex].width;
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
                pVertexList[n].lu = (pVertexList[n].lu - fUMin) * fUDel;
                pVertexList[n].lv = (pVertexList[n].lv - fVMin) * fVDel;
            }

            Texture2D lightTex = new Texture2D(lightMapWidth, lightMapHeight);
            Color[] colourarray = new Color[lightMapWidth * lightMapHeight];
            int tempCount = (int)face.lightmapOffset;
            for (int k = 0; k < lightMapWidth * lightMapHeight; k++)
            {
                if (tempCount + 3 > lightlump.Length) break;
                colourarray[k] = new Color32(lightlump[tempCount], lightlump[tempCount + 1], lightlump[tempCount + 2], 100);
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

            if (miptexLump[bspTexInfo.miptex].texture == null)
            {
                bspMaterial.mainTexture = missingtexture;
            }
            else
            {
                bspMaterial.mainTexture = miptexLump[bspTexInfo.miptex].texture;
                bspMaterial.mainTexture.name = miptexLump[bspTexInfo.miptex].name;
            }
            bspMaterial.mainTexture.filterMode = FilterMode.Bilinear;
            faceObjectRenderer.sharedMaterial = bspMaterial;
#endif
        }
    }
}