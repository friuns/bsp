using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace bsp
{
    public class BspGenerateMapVis : BSP30Map
    {
        public Texture2D missingtexture;
        string[] disable = new string[] { "sky" };
        string[] hide = new string[] { "aaatrigger", "black" };
        public bool enableLightMap;
        //RendererCache[] allRenderers;
        public Transform debugTransform;
        public static Vector3 playerPos;
        //private int oldPvs = 0;
        public bool RenderAllFaces = false;
        public Transform level;
        public override IEnumerator Load(MemoryStream ms)
        {
            level = new GameObject("level").transform;
            level.SetParent(transform, true);
            yield return base.Load(ms);
            using (Profile("GenerateVisObjects"))
                GenerateVisObjects();
            transform.localScale = scale * Vector3.one;
            //UpdatePvs(0);
            //loaded = true;

        }
        //bool loaded;
        public const float scale = 0.03f;
        public void Update()
        {
            //if (!loaded) return;
            //if (debugTransform)
            //    playerPos = debugTransform.position;
            //if (Input.GetKeyDown(KeyCode.Return))
            //    RenderAllFaces = !RenderAllFaces;
            //int pvs = RenderAllFaces ? 0 : WalkBSP();
            //if (pvs != oldPvs)
            //    UpdatePvs(pvs);
            //oldPvs = pvs;

        }
        //private void UpdatePvs(int id)
        //{

        //for (int i = 0; i < renderers.Length; i++)
        //    renderers[i].enabled = id == 0;

        //if (id != 0)
        //    foreach (Leaf leaf in leafs[id].pvsList)
        //        AllTrue(leaf.renderers);

        //RefreshRenderers();

        //}
        //private void RefreshRenderers()
        //{

        //    for (int i = 0; i < renderers.Length; i++)
        //        if (renderers[i].enabled != renderers[i].oldEnabled)
        //            renderers[i].oldEnabled = renderers[i].renderer.enabled = renderers[i].enabled;
        //}
        //private static void AllTrue(RendererCache[] rendererCaches)
        //{
        //    for (int i = 0; i < rendererCaches.Length; i++)
        //        rendererCaches[i].enabled = true;
        //}

        //private int BSPlookup(int node)
        //{
        //    var b = planesLump[nodesLump[node].planeNum].plane.GetSide(playerPos / scale);
        //    return nodesLump[node].children[b ? 1 : 0];
        //}
        //private int WalkBSP(int headnode = 0)
        //{
        //    int child = BSPlookup(headnode);

        //    while (child >= 0)
        //        child = BSPlookup(child);

        //    child = -(child + 1);
        //    return child;
        //}

        void GenerateVisObjects()
        {
            //List<RendererCache> cull = new List<RendererCache>();
            foreach (var face in facesLump)
            {
                GenerateFaceObject(face);
                //var r = face.renderer = new RendererCache { renderer = GenerateFaceObject(face) };
                //if (!pvsDisable && face.leaf != null && face.leaf.used)
                //{
                //    //r.renderer.enabled = false;
                //    cull.Add(face.renderer);
                //}
            }
            foreach (var a in mipModels.Values)
                GenerateMesh(a);
            //renderers = cull.ToArray();

            //foreach (Leaf leaf in leafs)
            //{
            //    leaf.renderers = new RendererCache[leaf.NumMarkSurfaces];
            //    for (int j = 0; j < leaf.faces.Length; j++)
            //    {
            //        BSPFace f = leaf.faces[j];
            //        leaf.renderers[j] = f.renderer;
            //    }
            //}

        }
        public Material mat;
        public Material matTrans;
        public class MaterialMesh
        {
            public Material material;
            public Mesh mesh;
        }
        public Dictionary<uint, MaterialMesh> mt = new Dictionary<uint, MaterialMesh>();
        Dictionary<ModelMipKey, MipModel> mipModels = new Dictionary<ModelMipKey, MipModel>();
        void GenerateFaceObject(BSPFace face)
        {

#if !console
            BSPTexInfo bspTexInfo = texinfoLump[face.texinfo_id];

            BSPMipTexture mip = miptexLump[bspTexInfo.miptex];
            mip.handled = true;
            MipModel mip2;
            var key = new ModelMipKey(mip, face.model);
            if (!mipModels.TryGetValue(key, out mip2))
                mip2 = mipModels[key] = new MipModel() { mip = mip };
            ArraySegment<Vector3> verts = mip2.verts.GetNextSegment(face.numberEdges); ;
            int edgestep = (int)face.firstEdgeIndex;
            for (int i = 0; i < face.numberEdges; i++)
            {
                var edge = edgesLump[Mathf.Abs(SurfEdgesLump[edgestep])];
                var vert = SurfEdgesLump[face.firstEdgeIndex + i] < 0 ? edge.vert1 : edge.vert2;
                verts[i] = ext.ConvertScaleVertex(vertsLump[vert]);
                edgestep++;
            }

            ArraySegment<int> tris = mip2.tris.GetNextSegment((face.numberEdges - 2) * 3);
            int tristep = 1;
            for (int i = 1; i < face.numberEdges - 1; i++)
            {
                var i2 = i + verts.offset;
                tris[tristep - 1] = verts.offset;
                tris[tristep] = i2;
                tris[tristep + 1] = i2 + 1;
                tristep += 3;
            }



            float scales = mip.width;
            float scalet = mip.height;
            ArraySegment<Vector2> uvs = mip2.uvs.GetNextSegment(face.numberEdges);
            for (int i = 0; i < face.numberEdges; i++)
                uvs[i] = new Vector2((Vector3.Dot(verts[i], bspTexInfo.vec3s) + bspTexInfo.offs) / scales, (Vector3.Dot(verts[i], bspTexInfo.vec3t) + bspTexInfo.offt) / scalet);

#else
            return null;
#endif
        }

        void GenerateMesh(MipModel mip)
        {
            Mesh faceMesh = mip.mesh;
            faceMesh.name = mip.name;
            faceMesh.vertices = mip.verts.ToArray();
            //faceMesh.triangles = mip.tris.ToArray();
            faceMesh.SetIndices(mip.tris.ToArray(), MeshTopology.Triangles, 0);
            faceMesh.uv = mip.uvs.ToArray();
            Debug.Log(mip.name + " verts: " + faceMesh.vertices.Length + " tris: " + faceMesh.triangles.Length + " uvs:" + faceMesh.uv.Length);
            faceMesh.RecalculateNormals();
            var faceObject = new GameObject(mip.name);
            faceObject.AddComponent<MeshFilter>().mesh = faceMesh;
            faceObject.transform.SetParent(level, true);
            var renderer = faceObject.AddComponent<MeshRenderer>();

            //if (enableLightMap && face.texinfo_id >= 0 && face.lightmapOffset < lightlump.Length)
            //{
            //    renderer.sharedMaterial = new Material(mat);
            //    CreateLightMap(face, verts, faceMesh, renderer);
            //}
            //else
            {
                if (mip.texture != null)
                {
                    Material m = mip.material;
                    if (m == null)
                        mip.material = m = new Material(mip.texture.format == TextureFormat.ARGB32 ? matTrans : mat);
                    m.mainTexture = mip.texture;
                    m.mainTexture.name = mip.name;
                    renderer.sharedMaterial = m;

                }
            }


            faceObject.isStatic = true;


            if (disable.Any(a => string.Equals(mip.name, a, StringComparison.OrdinalIgnoreCase)))
                faceObject.SetActive(false);
            else
            {
                var trigger = hide.Any(a => string.Equals(mip.name, a, StringComparison.OrdinalIgnoreCase));
                if (trigger)
                    renderer.sharedMaterials = new Material[0];

                if (!disableTexturesAndColliders)
                {
                    var c = faceObject.AddComponent<MeshCollider>();
                    c.enabled = !trigger;
                }
                faceObject.layer = Layer.Level;
            }

        }

        //        private void CreateLightMap(BSPFace face, Vector3[] verts, Mesh faceMesh, Renderer faceObjectRenderer)
        //        {
        //#if !console
        //            Material bspMaterial = faceObjectRenderer.sharedMaterial;

        //            Vertex[] pVertexList = new Vertex[verts.Length];
        //            float fUMin = 100000.0f;
        //            float fUMax = -10000.0f;
        //            float fVMin = 100000.0f;
        //            float fVMax = -10000.0f;
        //            var bspTexInfo = texinfoLump[face.texinfo_id];
        //            float pMipTexheight = miptexLump[bspTexInfo.miptex].height;
        //            float pMipTexwidth = miptexLump[bspTexInfo.miptex].width;
        //            for (int nEdge = 0; nEdge < verts.Length; nEdge++)
        //            {
        //                Vertex vertex = new Vertex(verts[nEdge]);
        //                vertex.u = verts[nEdge].x * bspTexInfo.vec3s.x + verts[nEdge].y * bspTexInfo.vec3s.y + verts[nEdge].z * bspTexInfo.vec3s.z + bspTexInfo.offs;
        //                vertex.v = verts[nEdge].x * bspTexInfo.vec3t.x + verts[nEdge].y * bspTexInfo.vec3t.y + verts[nEdge].z * bspTexInfo.vec3t.z + bspTexInfo.offt;
        //                vertex.u /= pMipTexwidth;
        //                vertex.v /= pMipTexheight;
        //                vertex.lu = vertex.u;
        //                vertex.lv = vertex.v;
        //                fUMin = (vertex.u < fUMin) ? vertex.u : fUMin;
        //                fUMax = (vertex.u > fUMax) ? vertex.u : fUMax;
        //                fVMin = (vertex.v < fVMin) ? vertex.v : fVMin;
        //                fVMax = (vertex.v > fVMax) ? vertex.v : fVMax;
        //                pVertexList[nEdge] = vertex;
        //            }
        //            int lightMapWidth = (int)(Mathf.Ceil((fUMax * pMipTexwidth) / 16.0f) - Mathf.Floor((fUMin * pMipTexwidth) / 16.0f) + 1.0f);
        //            int lightMapHeight = (int)(Mathf.Ceil((fVMax * pMipTexheight) / 16.0f) - Mathf.Floor((fVMin * pMipTexheight) / 16.0f) + 1.0f);
        //            float cZeroTolerance = 1e-06f;
        //            float fUDel = (fUMax - fUMin);
        //            if (fUDel > cZeroTolerance)
        //                fUDel = 1.0f / fUDel;
        //            else
        //                fUDel = 1.0f;
        //            float fVDel = (fVMax - fVMin);
        //            if (fVDel > cZeroTolerance)
        //                fVDel = 1.0f / fVDel;
        //            else
        //                fVDel = 1.0f;
        //            for (int n = 0; n < pVertexList.Length; n++)
        //            {
        //                pVertexList[n].lu = (pVertexList[n].lu - fUMin) * fUDel;
        //                pVertexList[n].lv = (pVertexList[n].lv - fVMin) * fVDel;
        //            }

        //            Texture2D lightTex = new Texture2D(lightMapWidth, lightMapHeight);
        //            Color[] colourarray = new Color[lightMapWidth * lightMapHeight];
        //            int tempCount = (int)face.lightmapOffset;
        //            for (int k = 0; k < lightMapWidth * lightMapHeight; k++)
        //            {
        //                if (tempCount + 3 > lightlump.Length) break;
        //                colourarray[k] = new Color32(lightlump[tempCount], lightlump[tempCount + 1], lightlump[tempCount + 2], 100);
        //                tempCount += 3;
        //            }
        //            lightTex.SetPixels(colourarray);
        //            lightTex.wrapMode = TextureWrapMode.Clamp;
        //            lightTex.Apply();
        //            var lvs = new List<Vector2>(new Vector2[pVertexList.Length]);
        //            for (int a = 0; a < pVertexList.Length; a++)
        //                lvs[a] = new Vector2(pVertexList[a].lu, pVertexList[a].lv);
        //            faceMesh.SetUVs(1, lvs);
        //            bspMaterial.SetTexture("_LightMap", lightTex);

        //            if (miptexLump[bspTexInfo.miptex].texture == null)
        //            {
        //                bspMaterial.mainTexture = missingtexture;
        //            }
        //            else
        //            {
        //                bspMaterial.mainTexture = miptexLump[bspTexInfo.miptex].texture;
        //                bspMaterial.mainTexture.name = miptexLump[bspTexInfo.miptex].name;
        //            }
        //            bspMaterial.mainTexture.filterMode = FilterMode.Bilinear;
        //            faceObjectRenderer.sharedMaterial = bspMaterial;
        //#endif
        //        }
    }
}
