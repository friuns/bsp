using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Rendering;
using Random = System.Random;

namespace bsp
{
    public class BspGenerateMapVis : BSP30Map
    {
        public override void Awake()
        {
            base.Awake();
            mat = Instantiate(mat);
            matTrans = Instantiate(matTrans);
            _BspGenerateMapVis = this;
        }
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
            Plane plane = planesLump[nodesLump[node].planeNum].plane;
            var side = plane.GetSide(CameraMainTransform.position / scale);
            return nodesLump[node].children[side ? 1 : 0];
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
            if (settings.disablePvs || planesLump == null) return;
                
            var i = WalkBSP();
            Leaf walkBsp = leafs[i];

            if (oldWalkBsp != walkBsp && walkBsp.renderer != null)
            {
                walkBsp.renderer.enabled = true;
                if(oldWalkBsp!=null)
                    oldWalkBsp.renderer.enabled = false;
                oldWalkBsp = walkBsp;
            }
        }
        void GenerateVisObjects()
        {
            using (ProfilePrint("CombTextures"))
                CombTextures();
            
            if(!settings.disablePvs)
            using (ProfilePrint("leafsCollect"))
            for (var index = 1; index < leafs.Length; index++)
            {
                var leafRoot = leafs[index];
                var m  = new CombinedModel();
                
                m.name = leafRoot.print();
                foreach (var leaf in leafRoot.pvsList)
                {
                    for (int i = 0; i < leaf.NumMarkSurfaces; i++)
                    {
                        BSPFace f = faces[markSurfaces[leaf.FirstMarkSurface + i]];
                        // if (f.mip.disabled) continue;
                        m.faceCount++;
                        m.vertsCount += f.numedges;
                        m.trianglesCount += (f.numedges - 2) * 3;
                    }
                }
                m.Init();
                // Parallel.ForEach(leafRoot.pvsList, (leaf,_,ix) =>  
                foreach (var leaf in leafRoot.pvsList)
                {
                    
                    for (int i = 0; i < leaf.NumMarkSurfaces; i++)
                    {
                        
                        BSPFace f = faces[markSurfaces[leaf.FirstMarkSurface + i]];
                        // if (f.mip.disabled) continue;
                        m.AddFace(f); //adds face to m
                    }
                    
                }
                leafRoot.renderer = m.GenerateMesh(false);
                leafRoot.renderer.enabled = false;
                // );
            }
        


            for (var index = 0; index < models.Length; index++)
            {
                BSPModel model = models[index];
                var limit = 64000 / 6;
                for (int j = 0; j < model.numberOfFaces; j += limit)
                {
                    CombinedModel combined = new CombinedModel();
                    CombinedModel combinedTrans = new CombinedModel();

                    var start = model.indexOfFirstFace+j;
                    var end = Mathf.Min(model.indexOfFirstFace + j + limit, model.indexOfFirstFace + model.numberOfFaces);

                    for (int i = start; i < end; i++)
                        if (!faces[i].mip.disabled)
                            (faces[i].mip.transparent?combinedTrans:combined).PreAddFace(faces[i]);
                    combined.Init();
                    combinedTrans.Init();
                    
                    for (int i = start; i < end; i++)
                        if (!faces[i].mip.disabled)
                            (faces[i].mip.transparent?combinedTrans:combined).AddFace(faces[i]);

                    if (combined.faceCount > 0)
                        model.Add(combined,false);
                    if (combinedTrans.faceCount > 0)
                        model.Add(combinedTrans, true);

                    if (index == 0 && !settings.disablePvs)
                        foreach(var a in model.renders)
                            a.enabled = false;

                    

                }
            }

            if (!Application.isEditor)
            {
                foreach (var a in texturesLump)
                    DestroyImmediate(a.texture);
                
            }

        }
        public Material mat;
        public Material matTrans;
    
        
        
        private void FaceLightmap2(BSPFace face,Vector3[] verts)
        {
            dtexinfo_t texinfo = texinfoLump[face.texinfo];

            var fUs = new float[verts.Length];
            var fVs = new float[verts.Length];

            

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
            Color32[] colourarray = TempArray<Color32>.GetArray(lightW * lightH, 1);
            int tempCount = (int)face.lightmapOffset;

            for (int k = 0; k < lightW * lightH; k++)
            {
                
                if (tempCount + 3 > lightlump.Length) break;

                byte r = lightlump[tempCount];
                byte g = lightlump[tempCount + 1];
                byte b = lightlump[tempCount + 2];
                
                
                colourarray[k] = new Color32(Pow(r), Pow(g), Pow(b), 255);

                tempCount += 3;
            }

            lightTex.SetPixels32(colourarray);
            lightTex.filterMode = FilterMode.Bilinear;
            lightTex.wrapMode = TextureWrapMode.Repeat;
            lightTex.Apply();



            face.lightTex = lightTex;
        }
        
        public static byte Clamp(int b)
        {
            return b > 255 ? (byte) 255 : (byte) b;
        }
        private byte Pow(int f)
        {
            //return (byte)f;
            f += 128;
            if (f > 255) return 255;
            // var g = f > 255 ? 255 : f;
            return (byte) (f * f / 255);
        }
     
        public void CombTextures()
        {
            foreach (var face in faces)
            // Parallel.ForEach(faces, face =>
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
            // );

            {
                var main_tex = new Texture2D(1, 1,TextureFormat.RGB24,false);
                var texture2Ds = faces.Select(a => a.mainTex).ToArray();
                Rect[] rects = main_tex.PackTextures(texture2Ds, 1);
                using (ProfilePrint("Repack textures"))
                    main_tex = Repack(main_tex);

                for (var i = 0; i < faces.Length; i++)
                {
                    var face = faces[i];
                    face.uv3 = new Vector4[face.numedges];
                    var rect = rects[i];
                    for (int j = 0; j < face.uv.Length; j++)
                        face.uv3[j] = new Vector4(rect.x, rect.y, rect.width, rect.height);
                }
                
                // main_tex.Compress(true);
                matTrans.mainTexture = mat.mainTexture = main_tex;
            }

            {
                using (ProfilePrint("FaceLightmap"))
                foreach (var face in faces)
                {
                    if (face.lightmapOffset < lightlump.Length)
                        FaceLightmap2(face,face.verts);
                }


                //lightmap
                var Lightmap_tex = new Texture2D(1, 1);
                Rect[] rects = Lightmap_tex.PackTextures(faces.Select(a => a.lightTex).ToArray(), 1);
                // Lightmap_tex.Compress(true);
                for (var i = 0; i < faces.Length; i++)
                {
                    var bspFace = faces[i];
                    DestroyImmediate(bspFace.lightTex);
                    for (int j = 0; j < bspFace.uv2.Length; j++)
                        bspFace.uv2[j] = new Vector2(bspFace.uv2[j].x * rects[i].width + rects[i].x, bspFace.uv2[j].y * rects[i].height + rects[i].y);
                }
                using (ProfilePrint("Repack textures"))
                    Lightmap_tex = Repack(Lightmap_tex);
                
                mat.SetTexture(Tag._LightMap, Lightmap_tex);
                
                
                
                matTrans.SetTexture(Tag._LightMap, Lightmap_tex);

           
            }

        }
        private static Texture2D Repack(Texture2D main_tex)
        {

            var main_tex2 = TextureManager.Texture2D(main_tex.width, main_tex.height, TextureFormat.RGB24); 
                // new Texture2D(main_tex.width, main_tex.height, TextureFormat.RGB24, false, true);
            main_tex2.SetPixels32(main_tex.GetPixels32());
            main_tex2.Apply(true);
            main_tex2.Compress(false);
            DestroyImmediate(main_tex);
            main_tex = main_tex2;
            return main_tex;
        }


        [Obsolete]
        public new Renderer renderers;
        [Obsolete]
        public new Renderer renderer;
    }

}