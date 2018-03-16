using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace bsp
{
    public class BSP30Map:MonoBehaviour
    {
        private int NumTexLoadFromWad;
        private BinaryReader br;
        public BSPHeader header;
        public BSPColors palette;
        public BSPEntityLump entityLump;
        public BSPFaceLump facesLump;
        public BSPEdgeLump edgeLump;
        public byte[] lightlump;
        public BSPVertexLump vertLump;
        public BSPTexInfoLump texinfoLump;
        public BSPMipTexture[] miptexLump;
        public BSPMarkSurfaces markSurfacesLump;
        public BSPvisLump visLump;
        //public BSPLeafLump leafLump;
        public BSPPlaneLump planeLump;
        public BSPNodeLump nodeLump;
        public BSPModelLump modelLump;
        private EntityParser myParser;
        public string wadUrl { get { return bsWeb.mainSite + "wad/"; } }

        public virtual IEnumerator Load(MemoryStream ms)
        {
            br = new BinaryReader(ms);
            header = new BSPHeader(br);
            Debug.Log(header.PrintInfo());
            new BspInfo();
            ReadEntities();
            ReadFaces();
            ReadEdges();
            ReadVerts();
            ReadTexinfo();
            ReadLightLump();
            ReadTextures();
            ReadMarkSurfaces();
            ReadLeafs();
            ReadPlanes();
            ReadNodes();
            ReadModels();
            ReadVisData();
            Debug.Log2("data start ");
            Debug.Log2("Entity char length " + entityLump.rawEntities.Length);
            Debug.Log2("lightmap length " + lightlump.Length);
            Debug.Log2("number of verts " + vertLump.verts.Length);
            Debug.Log2("number of Faces " + facesLump.faces.Length);
            Debug.Log2("textures " + texinfoLump.texinfo.Length);
            Debug.Log2("marksurf " + markSurfacesLump.markSurfaces.Length);
            Debug.Log2("VisData Length: " + visLump.compressedVIS.Length);
            Debug.Log2("leaf limp  Length: " + visLump.compressedVIS.Length);
            Debug.Log2("plane Length: " + planeLump.planes.Length);
            Debug.Log2("node lump Length: " + nodeLump.nodes.Length);
            Debug.Log2("models " + modelLump.models.Length);
            Debug.Log2("data end ");
            Debug.Log2("clipNodes "+ header.directory[9].length / 8);
            ReadPVS();
            br.BaseStream.Dispose();



            if (NumTexLoadFromWad > 0)
            {
                Debug.Log2("Reading in textures from wad");
                yield return findNullTextures();
            }
        }

        private void ReadPVS()
        {
            for (int i = 1; i < leafs.Length; i++)
            {
                int c;
                List<byte> pvs = new List<byte>();
                int offset = leafs[i].VisOffset;
                if (offset == -1)
                    continue;
                for (int j = 0; j < Mathf.FloorToInt((modelLump.models[0].numLeafs + 7f) / 8f);)
                {
                    if (visLump.compressedVIS[offset] != 0)
                    {
                        pvs.Add(visLump.compressedVIS[offset++]);
                        j++;
                    }
                    else
                    {
                        c = visLump.compressedVIS[offset + 1];
                        offset += 2;
                        while (c != 0)
                        {
                            pvs.Add(0);
                            j++;
                            c--;
                        }
                    }
                }
                leafs[i].pvs = new BitArray(pvs.ToArray());
            }
        }
        private void ReadNodes()
        {
            nodeLump = new BSPNodeLump();
            br.BaseStream.Position = header.directory[5].offset;
            int nodeCount = header.directory[5].length / BSPNode.Size;
            nodeLump.nodes = new BSPNode[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                nodeLump.nodes[i] = new BSPNode(br.ReadUInt32(), br.ReadInt16(), br.ReadInt16(), br.ReadPoint3s(),
                                    br.ReadPoint3s(), br.ReadUInt16(), br.ReadUInt16());
            }
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
            myParser = new EntityParser(entityLump.rawEntities);
            Dictionary<string, string> mylist = myParser.ReadEntity();
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
            planeLump = new BSPPlaneLump();
            br.BaseStream.Position = header.directory[1].offset;
            int planeCount = header.directory[1].length / 20;
            planeLump.planes = new BSPPlane[planeCount];
            for (int i = 0; i < planeCount; i++)
            {
                planeLump.planes[i] = new BSPPlane(br.ReadVector3(), br.ReadSingle(), br.ReadInt32());
            }
        }
        private void ReadVerts()
        {
            vertLump = new BSPVertexLump();
            br.BaseStream.Position = header.directory[3].offset;
            int numVerts = header.directory[3].length / 12;
            vertLump.verts = new Vector3[numVerts];
            for (int i = 0; i < numVerts; i++)
            {
                vertLump.verts[i] = br.ReadVector3();
            }
        }
        private void ReadEntities()
        {
            br.BaseStream.Position = header.directory[0].offset;
            entityLump = new BSPEntityLump(br.ReadChars(header.directory[0].length));
        }
        private void ReadEdges()
        {
            edgeLump = new BSPEdgeLump();
            br.BaseStream.Position = header.directory[12].offset;
            int numEdges = header.directory[12].length / 4;
            edgeLump.edges = new BSPEdge[numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                edgeLump.edges[i] = new BSPEdge(br.ReadUInt16(), br.ReadUInt16());
            }
            int numSURFEDGES = header.directory[13].length / 4;
            br.BaseStream.Position = header.directory[13].offset;
            edgeLump.SURFEDGES = new int[numSURFEDGES];
            for (int i = 0; i < numSURFEDGES; i++)
            {
                edgeLump.SURFEDGES[i] = br.ReadInt32();
            }
        }
        private void ReadFaces()
        {
            facesLump = new BSPFaceLump();
            br.BaseStream.Position = header.directory[7].offset;
            int numFaces = header.directory[7].length / 20;
            facesLump.faces = new BSPFace[numFaces];
            for (int i = 0; i < numFaces; i++)
            {
                facesLump.faces[i] = new BSPFace(br.ReadUInt16(), br.ReadUInt16(), br.ReadUInt32(), br.ReadUInt16(), br.ReadUInt16(),
                                  br.ReadBytes(4), br.ReadUInt32(), header.directory[8].length) {faceId = i};
            }
            Debug.Log2("faces read");
        }
        private void ReadTexinfo()
        {
            texinfoLump = new BSPTexInfoLump();
            br.BaseStream.Position = header.directory[6].offset;
            int numTexinfos = header.directory[6].length / 40;
            texinfoLump.texinfo = new BSPTexInfo[numTexinfos];
            for (int i = 0; i < numTexinfos; i++)
            {
                texinfoLump.texinfo[i] = new BSPTexInfo(br.ReadVector3(), br.ReadSingle(), br.ReadVector3(), br.ReadSingle(), br.ReadUInt32(), br.ReadUInt32());
            }
        }
        private IEnumerator LoadTextureFromWad(string WadFileName, TexInfoClass[] TexturesToLoad)
        {
            var w = new Web(wadUrl + WadFileName, cache: true);
            yield return Base.CustomCorontinue(w, error: null, Throw: false);
            if (!string.IsNullOrEmpty(w.w.error)) { yield break; }
            using (BinaryReader wadStream = new BinaryReader(new MemoryStream(w.w.bytes)))
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
            //stream.BaseStream.Position = ((miptex.width * miptex.height / 64) + miptex.offset[3] + textureOffset + 2);
            //byte[] colourArray = stream.ReadBytes(256 * 3);
            //stream.BaseStream.Position = (textureOffset + miptex.offset[0]);
            //int NumberOfPixels = miptex.height * miptex.width;
            //byte[] pixelArray = stream.ReadBytes(NumberOfPixels);
            //miptex.texture = MakeTexture2D(miptex.height, miptex.width, colourArray, pixelArray);
            return miptex;
        }
        //private Texture2D MakeTexture2D(int height, int width, byte[] colourArray, byte[] pixelArray)
        //{
        //    if ((width * height) != pixelArray.Length || colourArray.Length != (256 * 3))
        //    {
        //        Debug.LogError("(Method MakeTexture2D) something wrong with array sizes");
        //        return null;
        //    }
        //    Color[] colourPalette = new Color[256];
        //    int indexOfcolourArray = 0;
        //    for (int j = 0; j < colourPalette.Length; j++)
        //    {
        //        colourPalette[j] = new Color32(colourArray[indexOfcolourArray], colourArray[indexOfcolourArray + 1], colourArray[indexOfcolourArray + 2], 255);
        //        indexOfcolourArray += 3;
        //    }
        //    int NumberOfPixels = height * width;
        //    Color[] colour = new Color[NumberOfPixels];
        //    for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
        //    {
        //        var i = pixelArray[currentPixel];
        //        colour[currentPixel] = colourPalette[i];
        //    }
        //    Texture2D newTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
        //    newTexture2D.SetPixels(colour);
        //    newTexture2D.filterMode = FilterMode.Point;
        //    newTexture2D.Apply();
        //    return newTexture2D;
        //}
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
                Debug.Log2("starting to read in texture " + mip.name);
                //Debug.Log2((miptexLump[indexOfTex].width * miptexLump[indexOfTex].height / 64) + (miptexLump[indexOfTex].offset[3] + textureOffset + 2).ToString());
                ReadTexture(mip, textureOffset, br);
            }
            Debug.Log2("finished reading textures");
        }
        private void ReadTexture(BSPMipTexture mip, long textureOffset, BinaryReader BinaryReader)
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
        private void ReadMarkSurfaces()
        {
            markSurfacesLump = new BSPMarkSurfaces();
            int numMarkSurfaces = header.directory[11].length / 2;
            markSurfacesLump.markSurfaces = new int[numMarkSurfaces];
            br.BaseStream.Position = header.directory[11].offset;
            for (int i = 0; i < numMarkSurfaces; i++)
            {
                markSurfacesLump.markSurfaces[i] = br.ReadUInt16();
            }
        }
        private void ReadVisData()
        {
            visLump = new BSPvisLump();
            br.BaseStream.Position = header.directory[4].offset;
            visLump.compressedVIS = br.ReadBytes(header.directory[4].length);
            //visLump.SwizBits();
        }
        private void ReadModels()
        {
            modelLump = new BSPModelLump();
            br.BaseStream.Position = header.directory[14].offset;
            int modelCount = header.directory[14].length / 64;
            modelLump.models = new BSPModel[modelCount];
            for (int i = 0; i < modelCount; i++)
                modelLump.models[i] = new BSPModel(br.ReadVector32(), br.ReadVector32(), br.ReadVector32()
                                         , br.ReadInt32Array(4), br.ReadInt32(), br.ReadInt32(), br.ReadInt32());
        }
        public BSPLeaf[] leafs;
        private void ReadLeafs()
        {
            int leafCount = header.directory[10].length / 28;

            leafs = new BSPLeaf[leafCount];
            br.BaseStream.Position = header.directory[10].offset;
            for (int i = 0; i < leafCount; i++)
                leafs[i] = new BSPLeaf(br.ReadInt32(), br.ReadInt32(), br.ReadBBoxshort(), br.ReadBBoxshort(),
                                     br.ReadUInt16(), br.ReadUInt16(), br.ReadBytes(4));
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