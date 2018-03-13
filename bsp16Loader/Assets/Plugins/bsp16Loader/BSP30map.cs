using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace bsp
{
    public class BSP30Map
    {
        private BspInfo bspInfo;
        private int NumTexLoadFromWad;
        private BinaryReader BSPfile;
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
        public BSPLeafLump leafLump;
        public BSPPlaneLump planeLump;
        public BSPNodeLump nodeLump;
        public BSPModelLump modelLump;
        private EntityParser myParser;
        public static string wadUrl = bsWeb.mainSite + "wad/";

        public IEnumerator Load(MemoryStream ms)
        {
            BSPfile = new BinaryReader(ms);
            header = new BSPHeader(BSPfile);
            Debug.Log(header.PrintInfo());
            bspInfo = new BspInfo();
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
            Debug.Log2("number of Faces " + bspInfo.mapNum_faces);
            Debug.Log2("lightmap length " + bspInfo.mapNum_lighting);
            Debug.Log2("number of verts " + bspInfo.mapNum_verts);
            Debug.Log2("number of Faces " + bspInfo.mapNum_faces);
            Debug.Log2("textures " + bspInfo.mapNum_textures);
            Debug.Log2("marksurf " + markSurfacesLump.markSurfaces.Length);
            Debug.Log2("VisData Length: " + bspInfo.mapNum_visability);
            Debug.Log2("leaf limp  Length: " + leafLump.numLeafs);
            Debug.Log2("plane Length: " + planeLump.planes.Length);
            Debug.Log2("node lump Length: " + nodeLump.nodes.Length);
            Debug.Log2("models " + modelLump.models.Length);
            Debug.Log2("data end ");
            bspInfo.mapNum_clipnodes = header.directory[9].length / 8;
            ReadPVS();
            BSPfile.ReadBytes(3);
            BSPfile.BaseStream.Dispose();

            foreach (var item in VdfReader.Parse(entityLump.rawEntities))
            {
                var key = item["classname"] ?? "null";
                item.key = key;
                dict[key].Add(item);
            }
            if (NumTexLoadFromWad > 0)
            {
                Debug.Log2("Reading in textures from wad");
                yield return findNullTextures();
            }
        }
        public MyDictionary<string, List<VdfReader>> dict = new MyDictionary<string, List<VdfReader>>();
        private void ReadPVS()
        {
            for (int i = 1; i < leafLump.numLeafs; i++)
            {
                int c;
                List<byte> pvs = new List<byte>();
                int offset = leafLump.leafs[i].VisOffset;
                if (offset == -1)
                    continue;
                for (int j = 0; j < Mathf.FloorToInt((modelLump.models[0].numLeafs + 7f) / 8);)
                {
                    if (offset > visLump.compressedVIS.Length)
                    {
                        Debug.Log2("somthing wrong here");
                    }
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
                leafLump.leafs[i].pvs = new BitArray(pvs.ToArray());
            }
        }
        private void ReadNodes()
        {
            nodeLump = new BSPNodeLump();
            BSPfile.BaseStream.Position = header.directory[5].offset;
            int nodeCount = header.directory[5].length / BSPNode.Size;
            bspInfo.mapNum_nodes = nodeCount;
            nodeLump.nodes = new BSPNode[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                nodeLump.nodes[i] = new BSPNode(BSPfile.ReadUInt32(), BSPfile.ReadInt16(), BSPfile.ReadInt16(), BSPfile.ReadPoint3s(),
                                    BSPfile.ReadPoint3s(), BSPfile.ReadUInt16(), BSPfile.ReadUInt16());
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
            BSPfile.BaseStream.Position = header.directory[1].offset;
            int planeCount = header.directory[1].length / 20;
            bspInfo.mapNum_planes = planeCount;
            planeLump.planes = new BSPPlane[planeCount];
            for (int i = 0; i < planeCount; i++)
            {
                planeLump.planes[i] = new BSPPlane(BSPfile.ReadVector3(), BSPfile.ReadSingle(), BSPfile.ReadInt32());
            }
        }
        private void ReadVerts()
        {
            vertLump = new BSPVertexLump();
            BSPfile.BaseStream.Position = header.directory[3].offset;
            int numVerts = header.directory[3].length / 12;
            bspInfo.mapNum_verts = numVerts;
            vertLump.verts = new Vector3[numVerts];
            for (int i = 0; i < numVerts; i++)
            {
                vertLump.verts[i] = BSPfile.ReadVector3();
            }
        }
        private void ReadEntities()
        {
            BSPfile.BaseStream.Position = header.directory[0].offset;
            entityLump = new BSPEntityLump(BSPfile.ReadChars(header.directory[0].length));
        }
        private void ReadEdges()
        {
            edgeLump = new BSPEdgeLump();
            BSPfile.BaseStream.Position = header.directory[12].offset;
            int numEdges = header.directory[12].length / 4;
            bspInfo.mapNum_edges = numEdges;
            edgeLump.edges = new BSPEdge[numEdges];
            for (int i = 0; i < numEdges; i++)
            {
                edgeLump.edges[i] = new BSPEdge(BSPfile.ReadUInt16(), BSPfile.ReadUInt16());
            }
            int numSURFEDGES = header.directory[13].length / 4;
            BSPfile.BaseStream.Position = header.directory[13].offset;
            bspInfo.mapNum_surfedges = numSURFEDGES;
            edgeLump.SURFEDGES = new int[numSURFEDGES];
            for (int i = 0; i < numSURFEDGES; i++)
            {
                edgeLump.SURFEDGES[i] = BSPfile.ReadInt32();
            }
        }
        private void ReadFaces()
        {
            facesLump = new BSPFaceLump();
            BSPfile.BaseStream.Position = header.directory[7].offset;
            int numFaces = header.directory[7].length / 20;
            bspInfo.mapNum_faces = numFaces;
            facesLump.faces = new BSPFace[numFaces];
            for (int i = 0; i < numFaces; i++)
            {
                facesLump.faces[i] = new BSPFace(BSPfile.ReadUInt16(), BSPfile.ReadUInt16(), BSPfile.ReadUInt32(), BSPfile.ReadUInt16(), BSPfile.ReadUInt16(),
                                  BSPfile.ReadBytes(4), BSPfile.ReadUInt32(), header.directory[8].length);
            }
            Debug.Log2("faces read");
        }
        private void ReadTexinfo()
        {
            texinfoLump = new BSPTexInfoLump();
            BSPfile.BaseStream.Position = header.directory[6].offset;
            int numTexinfos = header.directory[6].length / 40;
            texinfoLump.texinfo = new BSPTexInfo[numTexinfos];
            for (int i = 0; i < numTexinfos; i++)
            {
                texinfoLump.texinfo[i] = new BSPTexInfo(BSPfile.ReadVector3(), BSPfile.ReadSingle(), BSPfile.ReadVector3(), BSPfile.ReadSingle(), BSPfile.ReadUInt32(), BSPfile.ReadUInt32());
            }
        }
        private IEnumerator LoadTextureFromWad(string WadFileName, TexInfoClass[] TexturesToLoad)
        {
            var w = new Web(wadUrl + WadFileName, cache: true);
            yield return w;
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
            stream.BaseStream.Position = ((miptex.width * miptex.height / 64) + miptex.offset[3] + textureOffset + 2);
            byte[] colourArray = stream.ReadBytes(256 * 3);
            stream.BaseStream.Position = (textureOffset + miptex.offset[0]);
            int NumberOfPixels = miptex.height * miptex.width;
            byte[] pixelArray = stream.ReadBytes(NumberOfPixels);
            miptex.texture = MakeTexture2D(miptex.height, miptex.width, colourArray, pixelArray);
            return miptex;
        }
        private Texture2D MakeTexture2D(int height, int width, byte[] colourArray, byte[] pixelArray)
        {
            if ((width * height) != pixelArray.Length || colourArray.Length != (256 * 3))
            {
                Debug.LogError("(Method MakeTexture2D) something wrong with array sizes");
                return null;
            }
            Color[] colourPalette = new Color[256];
            int indexOfcolourArray = 0;
            for (int j = 0; j < colourPalette.Length; j++)
            {
                colourPalette[j] = new Color32(colourArray[indexOfcolourArray], colourArray[indexOfcolourArray + 1], colourArray[indexOfcolourArray + 2], 255);
                indexOfcolourArray += 3;
            }
            int NumberOfPixels = height * width;
            Color[] colour = new Color[NumberOfPixels];
            int indexInToColourPalette;
            for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
            {
                colour[currentPixel] = new Color();
                indexInToColourPalette = pixelArray[currentPixel];
                if (indexInToColourPalette < 0 || indexInToColourPalette > 255)
                {
                    Debug.LogError("something wrong here chap!!!");
                }
                colour[currentPixel] = colourPalette[indexInToColourPalette];
            }
            Texture2D newTexture2D = new Texture2D(width, height, TextureFormat.RGB24, false);
            newTexture2D.SetPixels(colour);
            newTexture2D.filterMode = FilterMode.Point;
            newTexture2D.Apply();
            return newTexture2D;
        }
        private void ReadTextures()
        {
            BSPfile.BaseStream.Position = header.directory[2].offset;
            int numberOfTextures = (int)BSPfile.ReadUInt32();
            miptexLump = new BSPMipTexture[numberOfTextures];
            Int32[] BSPMIPTEXOFFSET = new Int32[numberOfTextures];
            for (int i = 0; i < numberOfTextures; i++)
            {
                BSPMIPTEXOFFSET[i] = (header.directory[2].offset + BSPfile.ReadInt32());
            }
            for (int indexOfTex = 0; indexOfTex < numberOfTextures; indexOfTex++)
            {
                int textureOffset = BSPMIPTEXOFFSET[indexOfTex];
                BSPfile.BaseStream.Position = textureOffset;
                miptexLump[indexOfTex] = new BSPMipTexture(BSPfile.LoadCleanString(16), BSPfile.ReadUInt32(), BSPfile.ReadUInt32(), BSPfile.ReadUInt32Array(4));
                if (miptexLump[indexOfTex].offset[0] == 0)
                {
                    NumTexLoadFromWad++;
                    miptexLump[indexOfTex].texture = null;
                    continue;
                }
                Debug.Log2("starting to read in texture " + miptexLump[indexOfTex].name);
                miptexLump[indexOfTex].texture = new Texture2D(miptexLump[indexOfTex].width, miptexLump[indexOfTex].height);
                Debug.Log2((miptexLump[indexOfTex].width * miptexLump[indexOfTex].height / 64) + (miptexLump[indexOfTex].offset[3] + textureOffset + 2).ToString());
                BSPfile.BaseStream.Position = ((miptexLump[indexOfTex].width * miptexLump[indexOfTex].height / 64) + miptexLump[indexOfTex].offset[3] + textureOffset + 2);
                Color[] colourPalette = new Color[256];
                for (int j = 0; j < 256; j++)
                {
                    colourPalette[j] = new Color32(BSPfile.ReadByte(), BSPfile.ReadByte(), BSPfile.ReadByte(), 0);
                }
                BSPfile.BaseStream.Position = (textureOffset + miptexLump[indexOfTex].offset[0]);
                int NumberOfPixels = miptexLump[indexOfTex].height * miptexLump[indexOfTex].width;
                Color[] colour = new Color[NumberOfPixels];
                int indexInToColourPalette;
                for (int currentPixel = 0; currentPixel < NumberOfPixels; currentPixel++)
                {
                    colour[currentPixel] = new Color();
                    indexInToColourPalette = BSPfile.ReadByte();
                    if (indexInToColourPalette < 0 || indexInToColourPalette > 255)
                    {
                        Debug.LogError("something wrong here chap!!!");
                    }
                    colour[currentPixel] = colourPalette[indexInToColourPalette];
                }
                miptexLump[indexOfTex].texture.SetPixels(colour);
                miptexLump[indexOfTex].texture.filterMode = FilterMode.Bilinear;
                miptexLump[indexOfTex].texture.Apply();
            }
            Debug.Log2("finished reading textures");
        }
        private void ReadMarkSurfaces()
        {
            markSurfacesLump = new BSPMarkSurfaces();
            int numMarkSurfaces = header.directory[11].length / 2;
            bspInfo.mapNum_marksurfaces = numMarkSurfaces;
            markSurfacesLump.markSurfaces = new int[numMarkSurfaces];
            BSPfile.BaseStream.Position = header.directory[11].offset;
            for (int i = 0; i < numMarkSurfaces; i++)
            {
                markSurfacesLump.markSurfaces[i] = BSPfile.ReadUInt16();
            }
        }
        private void ReadVisData()
        {
            bspInfo.mapNum_visability = header.directory[4].length;
            visLump = new BSPvisLump();
            BSPfile.BaseStream.Position = header.directory[4].offset;
            visLump.compressedVIS = BSPfile.ReadBytes(header.directory[4].length);
        }
        private void ReadModels()
        {
            modelLump = new BSPModelLump();
            BSPfile.BaseStream.Position = header.directory[14].offset;
            int modelCount = header.directory[14].length / 64;
            bspInfo.mapNum_models = modelCount;
            modelLump.models = new BSPModel[modelCount];
            for (int i = 0; i < modelCount; i++)
            {
                modelLump.models[i] = new BSPModel(BSPfile.ReadVector32(), BSPfile.ReadVector32(), BSPfile.ReadVector32()
                                         , BSPfile.ReadInt32Array(4), BSPfile.ReadInt32(), BSPfile.ReadInt32(), BSPfile.ReadInt32());
            }
        }
        private void ReadLeafs()
        {
            leafLump = new BSPLeafLump();
            int leafCount = header.directory[10].length / 28;
            bspInfo.mapNum_leafs = leafCount;
            leafLump.leafs = new BSPLeaf[leafCount];
            leafLump.numLeafs = leafCount;
            BSPfile.BaseStream.Position = header.directory[10].offset;
            for (int i = 0; i < leafCount; i++)
            {
                leafLump.leafs[i] = new BSPLeaf(BSPfile.ReadInt32(), BSPfile.ReadInt32(), BSPfile.ReadBBoxshort(), BSPfile.ReadBBoxshort(),
                                     BSPfile.ReadUInt16(), BSPfile.ReadUInt16(), BSPfile.ReadBytes(4));
            }
        }
        void ReadLightLump()
        {
            bspInfo.mapNum_lighting = header.directory[8].length;
            BSPfile.BaseStream.Position = header.directory[8].offset;
            if (header.directory[8].length == 0)
                return;
            lightlump = BSPfile.ReadBytes(header.directory[8].length);
        }
    }
}