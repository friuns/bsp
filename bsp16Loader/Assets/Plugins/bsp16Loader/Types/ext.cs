using System;
using System.Text;
using UnityEngine;

namespace bsp
{
    public static class ext
    {


        public static Point3s ReadPoint3s(this BinaryReader BinRead)
        {
            return new Point3s(BinRead.ReadInt16(), BinRead.ReadInt16(), BinRead.ReadInt16());
        }


        public static Vector3 ReadVector32(this BinaryReader BinRead)
        {
            var vert = ReadVector3(BinRead);

            return ConvertScaleVertex(vert) * BspGenerateMapVis.scale;
        }
        public static Vector3 ConvertScaleVertex(Vector3 vert)
        {
            return new Vector3(-vert.x, vert.z, -vert.y);
        }

        public static Vector3 ReadVector3(this BinaryReader BinRead)
        {
            return new Vector3(BinRead.ReadSingle(), BinRead.ReadSingle(), BinRead.ReadSingle());
        }

        public static Int32[] ReadInt32Array(this BinaryReader BinRead, int Count)
        {
            Int32[] IntArray = new Int32[Count];

            for (int i = 0; i < Count; i++)
            {
                IntArray[i] = BinRead.ReadInt32();

            }
            return IntArray;
        }

        //Use this as readchars() position can be wrong depending on which chars it reads in
        public static string LoadCleanString(this BinaryReader BinRead, int Count)
        {
            byte[] ByteArray = BinRead.ReadBytes(Count);

            if (ByteArray == null) return null;
            bool isTeminated = false;
            char[] dirtyChars = Encoding.UTF8.GetChars(ByteArray);
            for (int i = 0; i < dirtyChars.Length; i++)
            {



                if (char.IsControl(dirtyChars[i]) || isTeminated)
                {
                    dirtyChars[i] = '\x0000';
                    isTeminated = true;
                }
            }
            return new string(dirtyChars).TrimEnd('\0');
        }




        public static UInt32[] ReadUInt32Array(this BinaryReader BinRead, int Count)
        {
            UInt32[] IntArray = new UInt32[Count];

            for (int i = 0; i < Count; i++)
            {
                IntArray[i] = BinRead.ReadUInt32();

            }
            return IntArray;
        }

        public static Vector3 ReadBBoxshort(this BinaryReader BinRead)
        {
            return new Vector3(BinRead.ReadInt16(), BinRead.ReadInt16(), BinRead.ReadInt16());
        }

    }
}