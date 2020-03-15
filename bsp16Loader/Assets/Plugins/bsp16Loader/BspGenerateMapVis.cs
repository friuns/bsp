using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;

namespace bsp
{
    public class BspGenerateMapVis : BSP30Map
    {
        //public Texture2D missingtexture;
        
        //RendererCache[] allRenderers;
        //public Transform debugTransform;
        public static Vector3 playerPos;
        //private int oldPvs = 0;
        public bool useLightMaps;
        //public bool RenderAllFaces = false;
        public Transform level;
        public override IEnumerator Load(Stream ms)
        {

            level = new GameObject("level").transform;
            level.SetParent(transform, true); 
            yield return base.Load(ms);
            using (ProfilePrint("GenerateVisObjects"))
                GenerateVisObjects();
            if (useLightMaps)
            {
                LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional; // make sure you bake scene with substractive
                LightmapSettings.lightmaps = lightmapDatas.ToArray();
            }
            transform.localScale = scale * Vector3.one;
            //UpdatePvs(0);
            //loaded = true;

        }
        //bool loaded;
        public const float scale = Math2.itchToM;
  
        

        private int BSPlookup(int node)
        {
            var b = planesLump[nodesLump[node].planeNum].plane.GetSide(playerPos / scale);
            return nodesLump[node].children[b ? 1 : 0];
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
            CombTextures();
            for (var index = 1; index < leafs.Length; index++)
            {
                var leafRoot = leafs[index];
                var m = leafRoot.mip = new MipModel2();

                m.name = leafRoot.print();
                foreach (var leaf in leafRoot.pvsList)
                {
                    for (int i = 0; i < leaf.NumMarkSurfaces; i++)
                    {
                        BSPFace f = faces[markSurfaces[leaf.FirstMarkSurface + i]];
                        if (f.disabled) continue;
                        m.faceCount++;
                        m.vertsCount += f.numedges;
                    }
                }
                m.Init();


                foreach (var leaf in leafRoot.pvsList)
                {
                    for (int i = 0; i < leaf.NumMarkSurfaces; i++)
                    {
                        BSPFace f = faces[markSurfaces[leaf.FirstMarkSurface + i]];
                        if (f.disabled) continue;
                        GenerateFaceObject(f, m); //adds face to m
                    }
                }
            }
            foreach (var bspLeaf in leafs)
            {
                if (bspLeaf.mip != null)
                    bspLeaf.r = GenerateMesh(bspLeaf.mip);
            }
    

        }
        public Material mat;
        public Material matTrans;
    
        void GenerateFaceObject(BSPFace face,MipModel2 combined)
        {

            combined.faces.Add(face);
            ArraySegment<Vector3> verts = combined.verts.GetNextSegment(face.numedges);
            for (int j = 0; j < face.numedges; j++)
            {
                verts[j] = face.verts[j];
            }
      

            ArraySegment<int> tris = combined.tris.GetNextSegment((face.numedges - 2) * 3);
            int tristep = 1;
            for (int i = 1; i < face.numedges - 1; i++)
            {
                var i2 = i + verts.offset;
                tris[tristep - 1] = verts.offset;
                tris[tristep] = i2;
                tris[tristep + 1] = i2 + 1;
                tristep += 3;
            }

            

            ArraySegment<Vector2> uvs = combined.uvs.GetNextSegment(face.numedges);
            for (int j = 0; j < face.numedges; j++)
                uvs[j] = face.uv[j];
            
            ArraySegment<Vector2> uvs2 = combined.uvs2.GetNextSegment(face.numedges);
            for (int j = 0; j < face.numedges; j++)
                uvs2[j] = face.uv2[j];
            
            
            ArraySegment<Vector4> uvs3 = combined.uvs3.GetNextSegment(face.numedges);
            for (int j = 0; j < face.numedges; j++)
                uvs3[j] = face.uv3[j];

            

        }
        
        private void FaceLightmap2(BSPFace face,Vector3[] verts)
        {
            dtexinfo_t texinfo = texinfoLump[face.texinfo];
            List<float> fUs = new List<float>();
            List<float> fVs = new List<float>();

            for (int i = 0; i < verts.Length; i++)
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

            List<Vector2> UVs2 = TempList<Vector2>.GetTempList();
            for (int i = 0; i < verts.Length; i++)
            {
                float fU = Vector3.Dot(verts[i], texinfo.vec3s) + texinfo.offs; // - textureminsW;
                float fV = Vector3.Dot(verts[i], texinfo.vec3t) + texinfo.offt; // - textureminsH;

                float fLightMapU = fMidTexU + (fU - fMidPolyU) / 16.0f;
                float fLightMapV = fMidTexV + (fV - fMidPolyV) / 16.0f;

                float x = fLightMapU / lightW;
                float y = fLightMapV / lightH;

                UVs2.Add(new Vector2(x, y));
            }

            face.uv2 = UVs2.ToArray();

            Texture2D lightTex = TextureManager.Texture2D(lightW, lightH, TextureFormat.RGB24, false);
            Color32[] colourarray = new Color32[lightW * lightH];
            int tempCount = (int)face.lightmapOffset;

            for (int k = 0; k < lightW * lightH; k++)
            {
                
                if (tempCount + 3 > lightlump.Length) break;

                byte r = lightlump[tempCount];
                byte g = lightlump[tempCount + 1];
                byte b = lightlump[tempCount + 2];
                
                colourarray[k] = new Color32(Clamp(r + 128), Clamp(g + 128), Clamp(b + 128), 255);

                colourarray[k] = new Color32((byte)(Mathf.Pow(colourarray[k].r / 255.0f, 2) * 255), (byte)(Mathf.Pow(colourarray[k].g / 255.0f, 2) * 255), (byte)(Mathf.Pow(colourarray[k].b / 255.0f, 2) * 255), 255);


                tempCount += 3;
            }

            lightTex.SetPixels32(colourarray);
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();



            face.lightTex = lightTex;
        }
        public static byte Clamp(int b)
        {
            return b > 255 ? (byte) 255 : (byte) b;
        }
        public void CombTextures()
        {
            foreach (var face in faces)
            {
                
                int edgestep = (int) face.firstedge;
                face.verts = new Vector3[face.numedges];
                for (int i = 0; i < face.numedges; i++)
                {
                    BSPEdge edge = edgesLump[Mathf.Abs(surfedgesLump[edgestep])];
                    int vert = surfedgesLump[face.firstedge + i] < 0 ? edge.vert1 : edge.vert2;
                    face.verts[i] = bspExt.ConvertScaleVertex(vertexesLump[vert]);
                    edgestep++;
                }
                
                dtexinfo_t bspTexInfo = texinfoLump[face.texinfo];

                BSPMipTexture mip = texturesLump[bspTexInfo.miptex];
                mip.handled = true;

                float scales = mip.width;
                float scalet = mip.height;
            
                face.uv = new Vector2[face.numedges];
                
                for (int i = 0; i < face.numedges; i++)
                    face.uv[i] = new Vector2((Vector3.Dot(face.verts[i], bspTexInfo.vec3s) + bspTexInfo.offs) / scales, (Vector3.Dot(face.verts[i], bspTexInfo.vec3t) + bspTexInfo.offt) / scalet);
                face.mip = mip;
                face.mainTex = mip.texture;
            }


            {
                var main_tex = new Texture2D(1, 1);
                Rect[] rects = main_tex.PackTextures(faces.Select(a => a.mainTex).ToArray(), 1);
                
                for (var i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    DestroyImmediate(face.mainTex);
                    face.uv3 = new Vector4[face.numedges];
                    var rect = rects[i];
                    for (int j = 0; j < face.uv.Length; j++)
                        face.uv3[j] = new Vector4(rect.x, rect.y, rect.width, rect.height);
                }
                main_tex.Compress(true);
                mat.mainTexture = main_tex;

            }

            {
                foreach (var face in faces)
                {
                    if (face.lightmapOffset < lightlump.Length)
                        FaceLightmap2(face, face.verts);
                }


                //lightmap
                var Lightmap_tex = new Texture2D(1, 1);
                Rect[] rects = Lightmap_tex.PackTextures(faces.Select(a => a.lightTex).ToArray(), 1);
                Lightmap_tex.Compress(true);
                for (var i = 0; i < faces.Length; i++)
                {
                    var bspFace = faces[i];
                    DestroyImmediate(bspFace.lightTex);
                    for (int j = 0; j < bspFace.uv2.Length; j++)
                        bspFace.uv2[j] = new Vector2(bspFace.uv2[j].x * rects[i].width + rects[i].x, bspFace.uv2[j].y * rects[i].height + rects[i].y);
                }
                mat.SetTexture("_LightMap", Lightmap_tex);

                // if (useLightMaps)
                // {
                //     lightmapDatas.Add(new LightmapData {lightmapDir = Lightmap_tex, lightmapColor = Lightmap_tex, shadowMask = Lightmap_tex});
                //     renderer.lightmapIndex = lightmapDatas.Count - 1;
                //     renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
                // }
                // else
                //     renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

        }
        List<LightmapData> lightmapDatas = new List<LightmapData>();
        Renderer GenerateMesh(MipModel2 combined)
        {
            Mesh mesh = combined.mesh;
            mesh.name = combined.name;
            mesh.vertices = combined.verts.ToArray();
            mesh.SetIndices(combined.tris.ToArray(), MeshTopology.Triangles, 0);
            mesh.uv = combined.uvs.ToArray();
            mesh.uv2 = combined.uvs2.ToArray();
            mesh.SetUVs(2,combined.uvs3.ToArray());
            mesh.RecalculateNormals();
            var g = new GameObject(combined.name);
            g.AddComponent<MeshFilter>().mesh = mesh;
            g.transform.SetParent(level, true);
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.material = mat;
            foreach (var f in combined.faces)
                f.transform = g.transform;


            g.isStatic = true;

            
            // else
            // {
            //     var trigger = hide.Any(a => string.Equals(combined.name, a, StringComparison.OrdinalIgnoreCase));
            //     if (trigger)
            //         renderer.sharedMaterials = new Material[0];
            //
            //     if (!disableTexturesAndColliders)
            //     {
            //         Collider c = trigger ? g.AddComponent<BoxCollider>() : (Collider)g.AddComponent<MeshCollider>();
            //         c.isTrigger = trigger;
            //     }
            //     
            //     
            //     g.layer = /*transparent?Layer.ignoreRayCast:*/ trigger?Layer.trigger: Layer.level;
            // }
            
            return renderer;
        }
        [Obsolete]
        public new Renderer renderers;
        [Obsolete]
        public new Renderer renderer;
    }

}
