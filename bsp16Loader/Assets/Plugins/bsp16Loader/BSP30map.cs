using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace bsp
{
    public class BSP30Map : Base
    {
        internal bool disableTexturesAndColliders = false;
        private int NumTexLoadFromWad;
        private BinaryReader br;
        public BSPHeader header;
        public EntityParser entityLump;
        public BSPFace[] facesLump;
        public RendererCache[] renderers;
        public BSPEdge[] edgesLump;
        public int[] SurfEdgesLump;

        public byte[] lightlump;
        public Vector3[] vertsLump;
        public BSPTexInfo[] texinfoLump;
        public BSPMipTexture[] miptexLump;
        public int[] markSurfacesLump;
        public BSPPlane[] planesLump;
        public BSPNode[] nodesLump;
        public BSPModel[] modelsLump;

        public Func<string, Action<MemoryStream>, IEnumerator> loadWad;
        public virtual IEnumerator Load(MemoryStream ms)
        {
            br = new BinaryReader(ms);
            header = new BSPHeader(br);
            Debug.Log(header.PrintInfo());
            //new BspInfo();
            using (Profile("ReadEntities"))
                ReadEntities();
            using (Profile("ReadFaces"))
                ReadFaces();
            using (Profile("ReadEdges"))
                ReadEdges();
            using (Profile("ReadVerts"))
                ReadVerts();
            using (Profile("ReadTexinfo"))
                ReadTexinfo();
            using (Profile("ReadLightLump"))
                ReadLightLump();
            using (Profile("Textures"))
                ReadTextures();
            using (Profile("ReadMarkSurfaces"))
                ReadMarkSurfaces();
            using (Profile("ReadLeafs"))
                ReadLeafs();
            using (Profile("ReadPlanes"))
                ReadPlanes();
            using (Profile("ReadNodes"))
                ReadNodes();
            using (Profile("ReadModels"))
                ReadModels();
            using (Profile("ReadPvsVisData"))
                ReadPvsVisData();

            Debug.Log2("data start ");
            Debug.Log2("lightmap length " + lightlump.Length);
            Debug.Log2("number of verts " + vertsLump.Length);
            Debug.Log2("number of Faces " + facesLump.Length);
            Debug.Log2("textures " + texinfoLump.Length);
            Debug.Log2("leafs " + leafs.Length);
            Debug.Log2("marksurf " + markSurfacesLump.Length);
            Debug.Log2("plane Length: " + planesLump.Length);
            Debug.Log2("node lump Length: " + nodesLump.Length);
            Debug.Log2("models " + modelsLump.Length);
            Debug.Log2("data end ");
            Debug.Log2("clipNodes " + header.directory[9].length / 8);
            br.BaseStream.Dispose();




            if (NumTexLoadFromWad > 0 && !disableTexturesAndColliders)
            {
                Debug.Log2("Reading in textures from wad");
                yield return findNullTextures();
            }
        }


        private void ReadNodes()
        {
            br.BaseStream.Position = header.directory[5].offset;
            int nodeCount = header.directory[5].length / BSPNode.Size;
            nodesLump = new BSPNode[nodeCount];
            for (int i = 0; i < nodeCount; i++)
                nodesLump[i] = new BSPNode(br.ReadUInt32(), br.ReadInt16(), br.ReadInt16(), br.ReadPoint3s(), br.ReadPoint3s(), br.ReadUInt16(), br.ReadUInt16());
        }
        public IEnumerator findNullTextures()
        {
            TexInfoClass[] texinfo = new TexInfoClass[NumTexLoadFromWad];
            int IndexOfTexinfo = 0;
            for (int j = 0; j < miptexLump.Length; j++)
            {
                if (miptexLump[j].texture == null)
                {
                    texinfo[IndexOfTexinfo] = new TexInfoClass(miptexLump[j].name, j);
                    IndexOfTexinfo++;
                }
            }
            string[] wadFileNames;
            Dictionary<string, string> mylist = entityLump.ReadEntity();
            string tempString;
            if (mylist.ContainsKey("wad"))
            {
                tempString = mylist["wad"];
                wadFileNames = tempString.Split(';');
                for (int i = 0; i < wadFileNames.Length; i++)
                {
                    wadFileNames[i] = wadFileNames[i].Substring(wadFileNames[i].LastIndexOf("\\") + 1);//remove unwanted text
                    if (wadFileNames[i].Length > 3)
                    {
                        Debug.Log2(wadFileNames[i]);
                        yield return LoadTextureFromWad(wadFileNames[i], texinfo);
                    }
                }
            }
            else
            {
                Debug.Log2("no textures to load from wad, or no wad key found in bsp");
            }
        }
        private void ReadPlanes()
        {
            br.BaseStream.Position = header.directory[1].offset;
            int planeCount = header.directory[1].length / 20;
            planesLump = new BSPPlane[planeCount];
            for (int i = 0; i < planeCount; i++)
                planesLump[i] = new BSPPlane(br.ReadVector3(), br.ReadSingle(), br.ReadInt32());
        }
        private void ReadVerts()
        {
            br.BaseStream.Position = header.directory[3].offset;
            int numVerts = header.directory[3].length / 12;
            vertsLump = new Vector3[numVerts];
            for (int i = 0; i < numVerts; i++)
                vertsLump[i] = br.ReadVector3();
        }
        private void ReadEntities()
        {
            br.BaseStream.Position = header.directory[0].offset;
            entityLump = new EntityParser(new string(br.ReadChars(header.directory[0].length)));
        }
        private void ReadEdges()
        {
            br.BaseStream.Position = header.directory[12].offset;
            int numEdges = header.directory[12].length / 4;
            edgesLump = new BSPEdge[numEdges];

            for (int i = 0; i < numEdges; i++)
                edgesLump[i] = new BSPEdge(br.ReadUInt16(), br.ReadUInt16());

            int numSURFEDGES = header.directory[13].length / 4;
            br.BaseStream.Position = header.directory[13].offset;
            SurfEdgesLump = new int[numSURFEDGES];

            for (int i = 0; i < numSURFEDGES; i++)
                SurfEdgesLump[i] = br.ReadInt32();
        }
        private void ReadFaces()
        {
            br.BaseStream.Position = header.directory[7].offset;
            int numFaces = header.directory[7].length / 20;
            facesLump = new BSPFace[numFaces];

            for (int i = 0; i < numFaces; i++)
            {
                BSPFace face = facesLump[i] = new BSPFace(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadBytes(4), br.ReadUInt32(), header.directory[8].length) { faceId = i };
                
            }

            Debug.Log2("faces read");
        }


        public void GenerateFace(BSPFace dfaceT)
        {

            List<Vector3> vertex = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<Vector2> uv2 = new List<Vector2>();

            for (var i = dfaceT.firstEdgeIndex; i < dfaceT.firstEdgeIndex + dfaceT.numberEdges; i++)
                vertex.Add(SurfEdgesLump[i] <= 0 ? vertsLump[edgesLump[Mathf.Abs(SurfEdgesLump[i])].vert2] : vertsLump[edgesLump[Mathf.Abs(SurfEdgesLump[i])].vert1]);

            List<int> indices = new List<int>();
            for (int i = 1; i < vertex.Count - 1; i++)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(i + 1);
            }

            var dtexinfoT = texinfoLump[dfaceT.texinfo_id];
            var dmiptexT = miptexLump[dtexinfoT.miptex];

            var scale = 0.03f;

            for (int i = 0; i < vertex.Count; i++)
            {
                float DecalS = Vector3.Dot(vertex[i] * scale, dtexinfoT.vec3s) + dtexinfoT.offs * scale;
                float DecalT = Vector3.Dot(vertex[i] * scale, dtexinfoT.vec3t) + dtexinfoT.offt * scale;
                uv.Add(new Vector2(DecalS / (dmiptexT.width * scale), DecalT / (dmiptexT.height * scale)));
            }

            //lightmap
            List<float> vec3sDot = new List<float>();
            List<float> vec3tDot = new List<float>();
            for (int l = 0; l < vertex.Count; l++)
            {
                vec3sDot.Add(Vector3.Dot(dtexinfoT.vec3s, vertex[l]) + dtexinfoT.offs);
                vec3tDot.Add(Vector3.Dot(dtexinfoT.vec3t, vertex[l]) + dtexinfoT.offt);
            }

            for (int i = 0; i < vertex.Count; i++)
            {
                float f21 = (Mathf.Ceil(vec3sDot.Max() / 16f) - Mathf.Floor(vec3sDot.Min() / 16f) + 1) / 2f +
                            (Vector3.Dot(dtexinfoT.vec3s, vertex[i]) + dtexinfoT.offs - (vec3sDot.Min() + vec3sDot.Max()) / 2f) / 16f;

                float f22 = (Mathf.Ceil(vec3tDot.Max() / 16f) - Mathf.Floor(vec3tDot.Min() / 16f) + 1) / 2f +
                            (Vector3.Dot(dtexinfoT.vec3t, vertex[i]) + dtexinfoT.offt - (vec3tDot.Min() + vec3tDot.Max()) / 2f) / 16f;

                float DecalS = f21 / (Mathf.CeilToInt(vec3sDot.Max() / 16f) - Mathf.FloorToInt(vec3sDot.Min() / 16f) + 1);
                float DecalT = f22 / (Mathf.CeilToInt(vec3tDot.Max() / 16f) - Mathf.FloorToInt(vec3tDot.Min() / 16f) + 1);
                uv2.Add(new Vector2(DecalS, DecalT));
            }

            dfaceT.vertex = vertex.ToArray();
            dfaceT.triangles = indices.ToArray();
            dfaceT.uv = uv.ToArray();
            dfaceT.uv2 = uv2.ToArray();
            dfaceT.lightMapW = Mathf.CeilToInt(vec3sDot.Max() / 16f) - Mathf.FloorToInt(vec3sDot.Min() / 16f) + 1;
            dfaceT.lightMapH = Mathf.CeilToInt(vec3tDot.Max() / 16f) - Mathf.FloorToInt(vec3tDot.Min() / 16f) + 1;
        }

        private void ReadTexinfo()
        {
            br.BaseStream.Position = header.directory[6].offset;
            int numTexinfos = header.directory[6].length / 40;
            texinfoLump = new BSPTexInfo[numTexinfos];

            for (int i = 0; i < numTexinfos; i++)
                texinfoLump[i] = new BSPTexInfo(br.ReadVector3(), br.ReadSingle(), br.ReadVector3(), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32());
        }

        private IEnumerator LoadTextureFromWad(string WadFileName, TexInfoClass[] TexturesToLoad)
        {
            MemoryStream ms = null;
            yield return Base.CustomCorontinue(loadWad(WadFileName, a => ms = a), null, Throw: false);
            if (ms == null) yield break;
            using (Profile("ReadWad"))
            using (BinaryReader wadStream = new BinaryReader(ms))
            {
                string wadType = new string(wadStream.ReadChars(4));
                if (wadType != "WAD3" && wadType != "WAD2")
                {
                    Debug.LogError("Wad file wrong type");
                    yield break;
                }
                int numberOfTexs = (int)wadStream.ReadUInt32();
                wadStream.BaseStream.Position = wadStream.ReadUInt32();
                TexInfoClass[] TexuresInWadFile = new TexInfoClass[numberOfTexs];
                for (int i = 0; i < TexuresInWadFile.Length; i++)
                {
                    TexuresInWadFile[i] = new TexInfoClass();
                    TexuresInWadFile[i].IndexOfMipTex = (int)wadStream.ReadUInt32();
                    wadStream.BaseStream.Position += 12;
                    TexuresInWadFile[i].TextureName = wadStream.LoadCleanString(16);
                }
                for (int j = 0; j < TexturesToLoad.Length; j++)
                {
                    for (int k = 0; k < TexuresInWadFile.Length; k++)
                    {
                        if (string.Equals(TexturesToLoad[j].TextureName, TexuresInWadFile[k].TextureName, StringComparison.OrdinalIgnoreCase))
                        {
                            miptexLump[TexturesToLoad[j].IndexOfMipTex] = ReadInTexture(TexuresInWadFile[k].IndexOfMipTex, wadStream, WadFileName);
                            if (miptexLump[TexturesToLoad[j].IndexOfMipTex].texture != null)
                                TexturesToLoad[j].TextureName = null;
                            break;
                        }
                    }
                }
            }
        }
        public BSPMipTexture ReadInTexture(long Texoffset, BinaryReader stream, string wadname)
        {
            long textureOffset = Texoffset;
            stream.BaseStream.Position = textureOffset;
            BSPMipTexture miptex = new BSPMipTexture(stream.LoadCleanString(16), stream.ReadUInt32(), stream.ReadUInt32(), stream.ReadUInt32Array(4));
            if (miptex.offset[0] == 0)
            {
                Debug.Log2("Error Error");
                miptex.texture = null;
                return miptex;
            }
            ReadTexture(miptex, textureOffset, stream);

            return miptex;
        }

        private void ReadTextures()
        {
            br.BaseStream.Position = header.directory[2].offset;
            int numberOfTextures = (int)br.ReadUInt32();
            miptexLump = new BSPMipTexture[numberOfTextures];
            Int32[] BSPMIPTEXOFFSET = new Int32[numberOfTextures];
            for (int i = 0; i < numberOfTextures; i++)
            {
                BSPMIPTEXOFFSET[i] = (header.directory[2].offset + br.ReadInt32());
            }
            for (int indexOfTex = 0; indexOfTex < numberOfTextures; indexOfTex++)
            {
                int textureOffset = BSPMIPTEXOFFSET[indexOfTex];
                br.BaseStream.Position = textureOffset;
                miptexLump[indexOfTex] = new BSPMipTexture(br.LoadCleanString(16), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32Array(4));
                var mip = miptexLump[indexOfTex];
                if (mip.offset[0] == 0)
                {
                    NumTexLoadFromWad++;
                    mip.texture = null;
                    continue;
                }
                //Debug.Log2("starting to read in texture " + mip.name);
                ReadTexture(mip, textureOffset, br);
            }
            Debug.Log2("finished reading textures");
        }
        private void ReadTexture(BSPMipTexture mip, long textureOffset, BinaryReader BinaryReader)
        {
            if (disableTexturesAndColliders)
                mip.texture = Texture2D.whiteTexture;
            else
            {
                BinaryReader.BaseStream.Position = ((mip.width * mip.height / 64) + mip.offset[3] + textureOffset + 2);
                Color[] colourPalette = new Color[256];
                bool transparent = false;
                for (int j = 0; j < 256; j++)
                {
                    var c = new Color32(BinaryReader.ReadByte(), BinaryReader.ReadByte(), BinaryReader.ReadByte(), 255);
                    if (c.b == 255 && c.r == 0 && c.g == 0)
                    {
                        c.a = 0;
                        transparent = true;
                    }
                    colourPalette[j] = c;
                }
                BinaryReader.BaseStream.Position = (textureOffset + mip.offset[0]);
                int NumberOfPixels = mip.height * mip.width;
                Color[] colour = new Color[NumberOfPixels];
                for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
                {
                    var i = BinaryReader.ReadByte();
                    colour[currentPixel] = colourPalette[i];
                }
                mip.texture = new Texture2D(mip.width, mip.height, transparent ? TextureFormat.ARGB32 : TextureFormat.RGB24, false);
                mip.texture.SetPixels(colour);
                mip.texture.filterMode = FilterMode.Point;
                mip.texture.Apply();
            }
        }


#if !new 
        public void CreateLightmap2(IList<BSPFace> inpFaces, out Texture2D Lightmap_tex, out Vector2[] uv2)
        {
            Texture2D[] LMs = new Texture2D[inpFaces.Count];

            for (int i = 0; i < inpFaces.Count; i++)
            {
                var inpFace = inpFaces[i];

                if (inpFace.faceId == -1)
                    continue;

                LMs[i] = new Texture2D(inpFace.lightMapW, inpFace.lightMapH, TextureFormat.RGB24, false);
                Color32[] TexPixels = new Color32[LMs[i].width * LMs[i].height];
                var lightofs = facesLump[inpFace.faceId].lightmapOffset;
                for (int j = 0; j < TexPixels.Length; j++)
                {

                    byte r = lightlump[lightofs + j * 3];
                    byte g = lightlump[lightofs + j * 3 + 1];
                    byte b = lightlump[lightofs + j * 3 + 2];
                    TexPixels[j] = new Color32(r, g, b, 255);

                    //ColorRGBExp32 ColorRGBExp32 = TexLightToLinear(InpFaces[i].index+ (j * 4));
                    //TexPixels[j] = new Color32(ColorRGBExp32.r, ColorRGBExp32.g, ColorRGBExp32.b, 255);
                }

                LMs[i].SetPixels32(TexPixels);
            }
            Lightmap_tex = new Texture2D(1, 1);
            Rect[] UVs2 = Lightmap_tex.PackTextures(LMs, 1);
            var UV2 = new List<Vector2>();
            for (var i = 0; i < inpFaces.Count; i++)
            {
                DestroyImmediate(LMs[i]);
                for (int j = 0; j < inpFaces[i].uv2.Length; j++)
                    UV2.Add(new Vector2((inpFaces[i].uv2[j].x * UVs2[i].width) + UVs2[i].x, (inpFaces[i].uv2[j].y * UVs2[i].height) + UVs2[i].y));
            }
            uv2 = UV2.ToArray();
        }
#endif

        private void ReadMarkSurfaces()
        {
            int numMarkSurfaces = header.directory[11].length / 2;
            markSurfacesLump = new int[numMarkSurfaces];
            br.BaseStream.Position = header.directory[11].offset;
            for (int i = 0; i < numMarkSurfaces; i++)
                markSurfacesLump[i] = br.ReadUInt16();
        }
        //public const bool pvsDisable = true;
        private void ReadPvsVisData()
        {
            //br.BaseStream.Position = header.directory[4].offset;
            //var compressedVIS = br.ReadBytes(header.directory[4].length);

            //for (var i = 1; i < leafs.Length; i++)
            //{
            //    var leaf = leafs[i];
            //    List<byte> bytes = new List<byte>();
            //    int offset = leaf.VisOffset;
            //    if (offset == -1)
            //        continue;
            //    for (int j = 0; j < Mathf.FloorToInt((modelsLump[0].numLeafs + 7f) / 8f);)
            //    {
            //        if (compressedVIS[offset] != 0)
            //        {
            //            bytes.Add(compressedVIS[offset++]);
            //            j++;
            //        }
            //        else
            //        {
            //            int c = compressedVIS[offset + 1];
            //            offset += 2;
            //            while (c != 0)
            //            {
            //                bytes.Add(0);
            //                j++;
            //                c--;
            //            }
            //        }
            //    }

            //    var bits = new BitArray(bytes.ToArray());
            //    leaf.pvsList = new List<Leaf>(bits.Count / 10);
            //    for (int j = 0; j < bits.Length; j++)
            //    {
            //        if (bits[j])
            //        {
            //            Leaf a = leafs[j + 1];
            //            a.used = true;
            //            leaf.pvsList.Add(a);

            //        }
            //    }
            //}



        }
        private void ReadModels()
        {

            br.BaseStream.Position = header.directory[14].offset;
            int modelCount = header.directory[14].length / 64;
            modelsLump = new BSPModel[modelCount];
            for (int i = 0; i < modelCount; i++)
            {
                var m = modelsLump[i] = new BSPModel(br.ReadVector32(), br.ReadVector32(), br.ReadVector32(), br.ReadInt32Array(4), br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
                for (int j = m.indexOfFirstFace; j < m.indexOfFirstFace + m.numberOfFaces; j++)
                    facesLump[j].model = m;
            }

        }
        public Leaf[] leafs = new Leaf[0];
        private void ReadLeafs()
        {
            int leafCount = header.directory[10].length / 28;

            leafs = new Leaf[leafCount];
            br.BaseStream.Position = header.directory[10].offset;
            for (int i = 0; i < leafCount; i++)
            {
                var leaf = leafs[i] = new Leaf(br.ReadInt32(), /*vislist */ br.ReadInt32(), br.ReadBBoxshort(), br.ReadBBoxshort(), br.ReadUInt16(), br.ReadUInt16(), br.ReadBytes(4));

                leaf.faces = new BSPFace[leaf.NumMarkSurfaces];
                for (int j = 0; j < leaf.faces.Length; j++)
                {
                    BSPFace f = leaf.faces[j] = facesLump[markSurfacesLump[leaf.FirstMarkSurface + j]];
                    f.leaf = leaf;
                }
            }



        }
        void ReadLightLump()
        {
            br.BaseStream.Position = header.directory[8].offset;
            if (header.directory[8].length == 0)
                return;
            lightlump = br.ReadBytes(header.directory[8].length);
        }

    }
}