﻿using System;
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

            int bminsU = (int)Mathf.Floor(fMinU / LM_SAMPLE_SIZE);
            int bminsV = (int)Mathf.Floor(fMinV / LM_SAMPLE_SIZE);

            int bmaxsU = (int)Mathf.Ceil(fMaxU / LM_SAMPLE_SIZE);
            int bmaxsV = (int)Mathf.Ceil(fMaxV / LM_SAMPLE_SIZE);

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

            Texture2D lightTex = new Texture2D(lightW, lightH, TextureFormat.RGB24, false);
            Color32[] colourarray = new Color32[lightW * lightH];
            int tempCount = (int)face.lightmapOffset;

            for (int k = 0; k < lightW * lightH; k++)
            {
                if (tempCount + 3 > lightlump.Length) break;
                //colourarray[k] = new Color32(lightlump[tempCount], lightlump[tempCount + 1], lightlump[tempCount + 2], 100);

                var r = lightlump[tempCount];
                var g = lightlump[tempCount + 1];
                var b = lightlump[tempCount + 2];
                colourarray[k] = new Color32((byte)(Mathf.Pow(r / 255.0f, 0.45f) * 255), (byte)(Mathf.Pow(g / 255.0f, 0.45f) * 255), (byte)(Mathf.Pow(b / 255.0f, 0.45f) * 255), 255);


                tempCount += 3;
            }

            lightTex.SetPixels32(colourarray);
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();



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

            if (mip.texture != null)
            {
                Material m = mip.material;
                if (m == null)
                    mip.material = m = new Material(mip.texture.format == TextureFormat.ARGB32 ? matTrans : mat);
                m.mainTexture = mip.texture;
                m.mainTexture.name = mip.name;
                renderer.sharedMaterial = m;


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

    }
}
