using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
namespace bsp
{
    public partial class BSP30Map : Base
    {
        internal bool disableTexturesAndColliders = false;
        private int NumTexLoadFromWad;
        private new BinaryReader br;
        public BSPHeader header;
        public EntityParser entityLump;
        public BSPFace[] facesLump;
        //public RendererCache[] renderers;
        public BSPEdge[] edgesLump;
        public int[] surfedgesLump;

        public byte[] lightlump;
        public Vector3[] vertexesLump;
        public dtexinfo_t[] texinfoLump;
        public BSPMipTexture[] texturesLump;
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
            Debug.Log2("number of verts " + vertexesLump.Length);
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
            for (int j = 0; j < texturesLump.Length; j++)
            {
                if (texturesLump[j].texture == null)
                {
                    texinfo[IndexOfTexinfo] = new TexInfoClass(texturesLump[j].name, j);
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
                planesLump[i] = new BSPPlane(bspExt.ReadVector3(br), br.ReadSingle(), br.ReadInt32());
        }
        private void ReadVerts()
        {
            br.BaseStream.Position = header.directory[3].offset;
            int numVerts = header.directory[3].length / 12;
            vertexesLump = new Vector3[numVerts];
            for (int i = 0; i < numVerts; i++)
                vertexesLump[i] = bspExt.ReadVector3(br);
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
            surfedgesLump = new int[numSURFEDGES];

            for (int i = 0; i < numSURFEDGES; i++)
                surfedgesLump[i] = br.ReadInt32();
        }
        private void ReadFaces()
        {
            br.BaseStream.Position = header.directory[7].offset;
            int numFaces = header.directory[7].length / 20;
            facesLump = new BSPFace[numFaces];

            for (int i = 0; i < numFaces; i++)
            {
                facesLump[i] = new BSPFace(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(), br.ReadBytes(4), br.ReadUInt32(), header.directory[8].length) { faceId = i };

            }

            Debug.Log2("faces read");
        }


        public const int LM_SAMPLE_SIZE = 16;


        private void ReadTexinfo()
        {
            br.BaseStream.Position = header.directory[6].offset;
            int numTexinfos = header.directory[6].length / 40;
            texinfoLump = new dtexinfo_t[numTexinfos];

            for (int i = 0; i < numTexinfos; i++)
                texinfoLump[i] = new dtexinfo_t(bspExt.ReadVector3(br), br.ReadSingle(), bspExt.ReadVector3(br), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32());
        }

        private IEnumerator LoadTextureFromWad(string WadFileName, TexInfoClass[] TexturesToLoad)
        {
            MemoryStream ms = null;
            yield return CustomCorontinue(loadWad(WadFileName, a => ms = a), null, Throw: false);
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
                            texturesLump[TexturesToLoad[j].IndexOfMipTex] = ReadInTexture(TexuresInWadFile[k].IndexOfMipTex, wadStream, WadFileName);
                            if (texturesLump[TexturesToLoad[j].IndexOfMipTex].texture != null)
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
            texturesLump = new BSPMipTexture[numberOfTextures];
            Int32[] BSPMIPTEXOFFSET = new Int32[numberOfTextures];
            for (int i = 0; i < numberOfTextures; i++)
            {
                BSPMIPTEXOFFSET[i] = (header.directory[2].offset + br.ReadInt32());
            }
            for (int indexOfTex = 0; indexOfTex < numberOfTextures; indexOfTex++)
            {
                int textureOffset = BSPMIPTEXOFFSET[indexOfTex];
                br.BaseStream.Position = textureOffset;
                texturesLump[indexOfTex] = new BSPMipTexture(br.LoadCleanString(16), br.ReadUInt32(), br.ReadUInt32(), br.ReadUInt32Array(4));
                var mip = texturesLump[indexOfTex];
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

                    Color32 c = new Color32(BinaryReader.ReadByte(), BinaryReader.ReadByte(), BinaryReader.ReadByte(), 255);
                    if (j == 255)
                    {
                        //Debug.LogError(mip.name + " color at 255:" + c);
                        
                        c.r = c.b = c.g = c.a = 0; 
                    }
                    //if (c.b == 255 && c.r == 0 && c.g == 0)
                    //{
                    //    c.a = 0;
                    //    transparent = true;
                    //    Debug.LogError(mip.name + " color index:" + j);
                    //}

                    colourPalette[j] = c;
                }

                if (mip.name.StartsWith("{"))
                    transparent = true;

                BinaryReader.BaseStream.Position = (textureOffset + mip.offset[0]);
                int NumberOfPixels = mip.height * mip.width;
                Color[] colour = new Color[NumberOfPixels];
                for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
                {
                    var i = BinaryReader.ReadByte();
                    colour[currentPixel] = colourPalette[i];
                }
                mip.texture = TextureManager.Texture2D(mip.width, mip.height, transparent ? TextureFormat.ARGB32 : TextureFormat.RGB24/*,!transparent*/);
                mip.texture.SetPixels(colour);
//                if (transparent)
//                    mip.texture.filterMode = FilterMode.Point;
                mip.texture.Apply();
            }
        }




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