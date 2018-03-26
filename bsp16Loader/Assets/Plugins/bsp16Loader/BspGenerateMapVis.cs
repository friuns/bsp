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
            //foreach (var face in facesLump)
            //    GenerateFace(face);

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
            dtexinfo_t bspTexInfo = texinfoLump[face.texinfo];

            BSPMipTexture mip = texturesLump[bspTexInfo.miptex];
            mip.handled = true;
            MipModel mip2;
            var key = new ModelMipKey(mip, face.model);
            if (!mipModels.TryGetValue(key, out mip2))
                mip2 = mipModels[key] = new MipModel() { mip = mip };
            mip2.faces.Add(face);
            ArraySegment<Vector3> verts = mip2.verts.GetNextSegment(face.numedges); ;
            int edgestep = (int)face.firstedge;
            for (int i = 0; i < face.numedges; i++)
            {
                var edge = edgesLump[Mathf.Abs(surfedgesLump[edgestep])];
                var vert = surfedgesLump[face.firstedge + i] < 0 ? edge.vert1 : edge.vert2;
                verts[i] = ext.ConvertScaleVertex(vertexesLump[vert]);
                edgestep++;
            }

            ArraySegment<int> tris = mip2.tris.GetNextSegment((face.numedges - 2) * 3);
            int tristep = 1;
            for (int i = 1; i < face.numedges - 1; i++)
            {
                var i2 = i + verts.offset;
                tris[tristep - 1] = verts.offset;
                tris[tristep] = i2;
                tris[tristep + 1] = i2 + 1;
                tristep += 3;
            }
            if (face.lightmapOffset < lightlump.Length)
            {
                FaceLightmap2(face, verts);
            }


            float scales = mip.width;
            float scalet = mip.height;
            ArraySegment<Vector2> uvs = mip2.uvs.GetNextSegment(face.numedges);
            for (int i = 0; i < face.numedges; i++)
                uvs[i] = new Vector2((Vector3.Dot(verts[i], bspTexInfo.vec3s) + bspTexInfo.offs) / scales, (Vector3.Dot(verts[i], bspTexInfo.vec3t) + bspTexInfo.offt) / scalet);

#else
            return null;
#endif
        }
        private void FaceLightmap2(BSPFace face, ArraySegment<Vector3> verts)
        {
            dtexinfo_t texinfo = texinfoLump[face.texinfo];
            List<float> fUs = new List<float>();
            List<float> fVs = new List<float>();

            for (int i = 0; i < verts.len; i++)
            {
                fUs.Add(Vector3.Dot(texinfo.vec3s, verts[i]) + texinfo.offs);
                fVs.Add(Vector3.Dot(texinfo.vec3t, verts[i]) + texinfo.offt);
            }

            //For lightmaps
            float fMinU = fUs.Min();
            float fMinV = fVs.Min();
            float fMaxU = fUs.Max();
            float fMaxV = fVs.Max();

            int bminsU = (int) Mathf.Floor(fMinU / LM_SAMPLE_SIZE);
            int bminsV = (int) Mathf.Floor(fMinV / LM_SAMPLE_SIZE);

            int bmaxsU = (int) Mathf.Ceil(fMaxU / LM_SAMPLE_SIZE);
            int bmaxsV = (int) Mathf.Ceil(fMaxV / LM_SAMPLE_SIZE);

            int extentsW = (bmaxsU - bminsU) * LM_SAMPLE_SIZE;
            int extentsH = (bmaxsV - bminsV) * LM_SAMPLE_SIZE;

            int lightW = (extentsW / LM_SAMPLE_SIZE) + 1;
            int lightH = (extentsH / LM_SAMPLE_SIZE) + 1;

            float fMidPolyU = (fMinU + fMaxU) / 2f;
            float fMidPolyV = (fMinV + fMaxV) / 2f;
            float fMidTexU = (lightW) / 2f;
            float fMidTexV = (lightH) / 2f;
            List<Vector2> UVs2 = new List<Vector2>();
            for (int i = 0; i < verts.len; i++)
            {
                float fU = Vector3.Dot(verts[i], texinfo.vec3s) + texinfo.offs; // - textureminsW;
                float fV = Vector3.Dot(verts[i], texinfo.vec3t) + texinfo.offt; // - textureminsH;
                //UVs2.Add(new Vector2(fU/ scales, fV/scalet));

                //fU += LM_SAMPLE_SIZE >> 1;
                //fV += LM_SAMPLE_SIZE >> 1;

                //fU /= 128 * LM_SAMPLE_SIZE;
                //fV /= 128 * LM_SAMPLE_SIZE;

                float fLightMapU = fMidTexU + (fU - fMidPolyU) / 16.0f;
                float fLightMapV = fMidTexV + (fV - fMidPolyV) / 16.0f;

                //fU/=(extentsW/LM_SAMPLE_SIZE)+1;
                //fV/=(extentsH/LM_SAMPLE_SIZE)+1;

                float x = fLightMapU / lightW;
                float y = fLightMapV / lightH;

                UVs2.Add(new Vector2(x, y));
            }

            face.uv2 = UVs2.ToArray();

            Texture2D lightTex = new Texture2D(lightW, lightH);
            Color32[] colourarray = new Color32[lightW * lightH];
            int tempCount = (int) face.lightmapOffset;

            for (int k = 0; k < lightW * lightH; k++)
            {
                if (tempCount + 3 > lightlump.Length) break;
                colourarray[k] = new Color32(lightlump[tempCount], lightlump[tempCount + 1], lightlump[tempCount + 2], 100);
                tempCount += 3;
            }

            lightTex.SetPixels32(colourarray);
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();
            face.lightTex = lightTex;
        }
        private void FaceLightmap(BSPFace face, ArraySegment<Vector3> verts)
        {
            Vertex[] pVertexList = new Vertex[verts.len];
            float fUMin = 100000.0f;
            float fUMax = -10000.0f;
            float fVMin = 100000.0f;
            float fVMax = -10000.0f;
            dtexinfo_t texInfo = texinfoLump[face.texinfo];

            float pMipTexheight = texturesLump[texInfo.miptex].height;
            float pMipTexwidth = texturesLump[texInfo.miptex].width;
            for (int i = 0; i < verts.len; i++)
            {
                // Add vertex information
                Vertex vertex = new Vertex(verts[i]);
                // Generate texture coordinates for face
                vertex.u = verts[i].x * texInfo.vec3s.x + verts[i].y * texInfo.vec3s.y + verts[i].z * texInfo.vec3s.z + texInfo.offs;
                vertex.v = verts[i].x * texInfo.vec3t.x + verts[i].y * texInfo.vec3t.y + verts[i].z * texInfo.vec3t.z + texInfo.offt;
                vertex.u /= pMipTexwidth;
                vertex.v /= pMipTexheight;
                vertex.lu = vertex.u;
                vertex.lv = vertex.v;
                fUMin = (vertex.u < fUMin) ? vertex.u : fUMin;
                fUMax = (vertex.u > fUMax) ? vertex.u : fUMax;
                fVMin = (vertex.v < fVMin) ? vertex.v : fVMin;
                fVMax = (vertex.v > fVMax) ? vertex.v : fVMax;
                pVertexList[i] = vertex;
            }

            int lightMapWidth = (int)(Mathf.Ceil((fUMax * pMipTexwidth) / 16.0f) - Mathf.Floor((fUMin * pMipTexwidth) / 16.0f) + 1.0f);
            int lightMapHeight = (int)(Mathf.Ceil((fVMax * pMipTexheight) / 16.0f) - Mathf.Floor((fVMin * pMipTexheight) / 16.0f) + 1.0f);
            // Update light-map vertex u, v coordinates.  These should range from [0.0 -> 1.0] over face.
            float fUDel = (fUMax - fUMin);
            if (fUDel > 1e-06f)
                fUDel = 1f / fUDel;
            else
                fUDel = 1f;
            float fVDel = (fVMax - fVMin);
            if (fVDel > 1e-06f)
                fVDel = 1f / fVDel;
            else
                fVDel = 1f;
            for (int n = 0; n < pVertexList.Length; n++)
            {
                (pVertexList)[n].lu = Mathf.Clamp((pVertexList[n].lu - fUMin) * fUDel, 0, 1);
                (pVertexList)[n].lv = Mathf.Clamp((pVertexList[n].lv - fVMin) * fVDel, 0, 1);
            }

            //    Debug.Log(lightMapWidth+" "+ lightMapHeight);
            Texture2D lightTex = new Texture2D(lightMapWidth, lightMapHeight);
            Color32[] colourarray = new Color32[lightMapWidth * lightMapHeight];
            int tempCount = (int)face.lightmapOffset;

            for (int k = 0; k < lightMapWidth * lightMapHeight; k++)
            {
                if (tempCount + 3 > lightlump.Length) break;
                colourarray[k] = new Color32(lightlump[tempCount], lightlump[tempCount + 1], lightlump[tempCount + 2], 100);
                tempCount += 3;
            }

            lightTex.SetPixels32(colourarray);
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();
            //lt[(int)face.lightmapOffset].lightmapFar = lt[(int)face.lightmapOffset].lightmapNear =
            //var lt = new LightmapData();
            //lt.lightmapNear = lt.lightmapFar = lightTex;
            //lt2.Add(lt);
            Vector2[] lvs = new Vector2[pVertexList.Length];
            for (int a = 0; a < pVertexList.Length; a++)
                lvs[a] = new Vector2(pVertexList[a].lu, pVertexList[a].lv);

            face.uv2 = lvs;
            face.lightTex = lightTex;
        }

        void GenerateMesh(MipModel mip)
        {
            Mesh faceMesh = mip.mesh;
            faceMesh.name = mip.name;
            faceMesh.vertices = mip.verts.ToArray();
            //faceMesh.triangles = mip.tris.ToArray();
            faceMesh.SetIndices(mip.tris.ToArray(), MeshTopology.Triangles, 0);
            faceMesh.uv = mip.uvs.ToArray();
            //Debug.Log(mip.name + " verts: " + faceMesh.vertices.Length + " tris: " + faceMesh.triangles.Length + " uvs:" + faceMesh.uv.Length);
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
            //{
            if (mip.texture != null)
            {
                Material m = mip.material;
                if (m == null)
                    mip.material = m = new Material(mip.texture.format == TextureFormat.ARGB32 ? matTrans : mat);
                m.mainTexture = mip.texture;
                m.mainTexture.name = mip.name;
                renderer.sharedMaterial = m;

                //Texture2D lm;
                //Vector2[] uv2;
                //Debug.Log("Generate lightmap for " + mip.mip.name + " faces:" + mip.faces.Count);
                //CreateLightmap2(mip.faces, out lm, out uv2);
                //faceMesh.uv2 = uv2;

                var Lightmap_tex = new Texture2D(1, 1);
                var inpFaces = mip.faces;
                var lMs = inpFaces.Select(a => a.lightTex).ToArray();
                Rect[] rects = Lightmap_tex.PackTextures(lMs, 2);
                var UV2 = new List<Vector2>();
                for (var i = 0; i < inpFaces.Count; i++)
                {
                    DestroyImmediate(lMs[i]);
                    for (int j = 0; j < inpFaces[i].uv2.Length; j++)
                        UV2.Add(new Vector2(inpFaces[i].uv2[j].x * rects[i].width + rects[i].x, inpFaces[i].uv2[j].y * rects[i].height + rects[i].y));
                }


                m.SetTexture("_LightMap", Lightmap_tex);
                faceMesh.SetUVs(1, UV2);
            }
            //}


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


        private void CreateLightMap1(BSPFace face, Vector3[] verts, Mesh faceMesh, Renderer faceObjectRenderer)
        {
#if !console
            Material bspMaterial = faceObjectRenderer.sharedMaterial;
            Vertex[] pVertexList = new Vertex[verts.Length];
            float fUMin = 100000.0f;
            float fUMax = -10000.0f;
            float fVMin = 100000.0f;
            float fVMax = -10000.0f;
            var bspTexInfo = texinfoLump[face.texinfo];
            float pMipTexheight = texturesLump[bspTexInfo.miptex].height;
            float pMipTexwidth = texturesLump[bspTexInfo.miptex].width;
            for (int i = 0; i < verts.Length; i++)
            {
                Vertex vertex = new Vertex(verts[i]);
                vertex.u = verts[i].x * bspTexInfo.vec3s.x + verts[i].y * bspTexInfo.vec3s.y + verts[i].z * bspTexInfo.vec3s.z + bspTexInfo.offs;
                vertex.v = verts[i].x * bspTexInfo.vec3t.x + verts[i].y * bspTexInfo.vec3t.y + verts[i].z * bspTexInfo.vec3t.z + bspTexInfo.offt;
                vertex.u /= pMipTexwidth;
                vertex.v /= pMipTexheight;
                vertex.lu = vertex.u;
                vertex.lv = vertex.v;
                fUMin = (vertex.u < fUMin) ? vertex.u : fUMin;
                fUMax = (vertex.u > fUMax) ? vertex.u : fUMax;
                fVMin = (vertex.v < fVMin) ? vertex.v : fVMin;
                fVMax = (vertex.v > fVMax) ? vertex.v : fVMax;
                pVertexList[i] = vertex;
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

            if (texturesLump[bspTexInfo.miptex].texture == null)
            {
                bspMaterial.mainTexture = missingtexture;
            }
            else
            {
                bspMaterial.mainTexture = texturesLump[bspTexInfo.miptex].texture;
                bspMaterial.mainTexture.name = texturesLump[bspTexInfo.miptex].name;
            }
            bspMaterial.mainTexture.filterMode = FilterMode.Bilinear;
            faceObjectRenderer.sharedMaterial = bspMaterial;
#endif
        }
    }
}
