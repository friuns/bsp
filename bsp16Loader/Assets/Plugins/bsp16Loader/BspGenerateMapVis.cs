using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;
using Random = System.Random;

namespace bsp
{
    public class BspGenerateMapVis : BSP30Map
    {
        //public Texture2D missingtexture;
        
        //RendererCache[] allRenderers;
        //public Transform debugTransform;
        //private int oldPvs = 0;
        //public bool RenderAllFaces = false;
        public Transform level;
        public override IEnumerator Load(Stream ms)
        {

            level = new GameObject("level").transform;
            level.SetParent(transform, true);
            using (ProfilePrint("base.Load"))
                yield return base.Load(ms);
            using (ProfilePrint("GenerateVisObjects"))
                GenerateVisObjects();
            
            level.localScale = scale * Vector3.one;
            //UpdatePvs(0);
            //loaded = true;

        }
        //bool loaded;
        public const float scale = Math2.itchToM;
  
        

        private int BSPlookup(int node)
        {
            
            var b = planesLump[nodesLump[node].planeNum].plane.GetSide(CameraMainTransform.position / scale);
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
        public Leaf oldWalkBsp;
        public void Update()
        {
            var i = WalkBSP();
            Leaf walkBsp = leafs[i];

            if (oldWalkBsp != walkBsp && walkBsp.r != null)
            {
                walkBsp.r.enabled = true;
                if(oldWalkBsp!=null)
                    oldWalkBsp.r.enabled = false;
                oldWalkBsp = walkBsp;
            }
        }
        void GenerateVisObjects()
        {
            using (ProfilePrint("CombTextures"))
                CombTextures();
            using (ProfilePrint("leafsCollect"))
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
                        if (f.mip.disabled) continue;
                        m.faceCount++;
                        m.vertsCount += f.numedges;
                        m.trianglesCount += (f.numedges - 2) * 3;
                    }
                }
                m.Init();

                foreach (var leaf in leafRoot.pvsList)
                {
                    for (int i = 0; i < leaf.NumMarkSurfaces; i++)
                    {
                        BSPFace f = faces[markSurfaces[leaf.FirstMarkSurface + i]];
                        if (f.mip.disabled) continue;
                        GenerateFaceObject(f, m); //adds face to m
                    }
                }
            }
            using (ProfilePrint("leafsCreate"))
            foreach (var bspLeaf in leafs)
            {
                if (bspLeaf.mip != null)
                {
                    bspLeaf.r = GenerateMesh(bspLeaf.mip);
                    bspLeaf.r.enabled = false;
                    // bspLeaf.mip = null;
                }
            }
    

        }
        public Material mat;
        public Material matTrans;
    
        void GenerateFaceObject(BSPFace face,MipModel2 combined)
        {
            // combined.faces.array[combined.faces.offset++] = face;
            
            var offset = combined.verts.offset;
            combined.verts.Add(face.verts);

            var tris = combined.tris;
            int tristep = 1;
            for (int i = 1; i < face.numedges - 1; i++)
            {
                var i2 = i + offset;
                tris.array[tris.offset+tristep - 1] = offset;
                tris.array[tris.offset+tristep] = i2;
                tris.array[tris.offset+tristep + 1] = i2 + 1;
                tristep += 3;
            }


            tris.offset += (face.numedges - 2) * 3;
            
            
            combined.uvs.Add(face.uv);
            combined.uvs2.Add(face.uv2);
            combined.uvs3.Add(face.uv3);

        }
        
        private void FaceLightmap2(BSPFace face)
        {
            var verts = face.verts;
            dtexinfo_t texinfo = texinfoLump[face.texinfo];
            var fUs = TempArray<float>.GetArray(verts.Length);
            var fVs = TempArray<float>.GetArray(verts.Length);

            for (int i = 0; i < verts.Length; i++)
            {
                fUs[i] = Vector3.Dot(texinfo.vec3s, verts[i]) + texinfo.offs;
                fVs[i] = Vector3.Dot(texinfo.vec3t, verts[i]) + texinfo.offt;
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

            var UVs2= face.uv2  = new Vector2[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                float fU = Vector3.Dot(verts[i], texinfo.vec3s) + texinfo.offs; // - textureminsW;
                float fV = Vector3.Dot(verts[i], texinfo.vec3t) + texinfo.offt; // - textureminsH;

                float fLightMapU = fMidTexU + (fU - fMidPolyU) / 16.0f;
                float fLightMapV = fMidTexV + (fV - fMidPolyV) / 16.0f;

                float x = fLightMapU / lightW;
                float y = fLightMapV / lightH;

                UVs2[i] = new Vector2(x, y);
            }

            Texture2D lightTex = TextureManager.Texture2D(lightW, lightH, TextureFormat.RGB24, false);

            Color32[] colourarray = TempArray<Color32>.GetArray(lightW * lightH);
            int tempCount = (int)face.lightmapOffset;

            for (int k = 0; k < lightW * lightH; k++)
            {
                
                if (tempCount + 3 > lightlump.Length) break;

                byte r = lightlump[tempCount];
                byte g = lightlump[tempCount + 1];
                byte b = lightlump[tempCount + 2];
                
                colourarray[k] = new Color32(Pow(r + 128), Pow(g+128), Pow(b+128), 255);


                tempCount += 3;
            }
            

            lightTex.SetPixels32(colourarray);
            
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Clamp;
            lightTex.Apply();



            face.lightTex = lightTex;
        }
        private byte Pow(int f)
        {
            if (f == 255) return 255;
            // var g = f > 255 ? 255 : f;
            return (byte) (f * f / 255);
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
                    face.verts[i].y += UnityEngine.Random.Range(0, 10);
                    face.verts[i].x += UnityEngine.Random.Range(0, 10);
                    face.verts[i].z += UnityEngine.Random.Range(0, 10);
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
                foreach (var a in texturesLump)
                    DestroyImmediate(a.texture);
                
                for (var i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    face.uv3 = new Vector4[face.numedges];
                    var rect = rects[i];
                    for (int j = 0; j < face.uv.Length; j++)
                        face.uv3[j] = new Vector4(rect.x, rect.y, rect.width, rect.height);
                }
                main_tex.Compress(true);
                mat.mainTexture = main_tex;

            }

            {
                using (ProfilePrint("FaceLightmap"))
                foreach (var face in faces)
                {
                    if (face.lightmapOffset < lightlump.Length)
                        FaceLightmap2(face);
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
        
        Renderer GenerateMesh(MipModel2 combined)
        {
            Mesh mesh = combined.mesh;
            mesh.name = combined.name;
            mesh.vertices = combined.verts.ToArray();
            mesh.SetIndices(combined.tris.ToArray(), MeshTopology.Triangles, 0);
            mesh.uv = combined.uvs.ToArray();
            mesh.uv2 = combined.uvs2.ToArray();
            mesh.SetUVs(2,combined.uvs3.ToArray());
            // AutoWeld.Weld(mesh,.1f,100);
            mesh.RecalculateNormals();
            // mesh.Optimize();
            var g = new GameObject(combined.name);
            g.AddComponent<MeshFilter>().mesh = mesh;
            g.transform.SetParent(level, true);
            var renderer = g.AddComponent<MeshRenderer>();
            renderer.material = mat;
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