namespace bsp
{
    public class BSPTexInfoLump
    {
        public BSPTexInfo[] texinfo;

        public BSPTexInfoLump()
        {
        }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("TexInfos:\r\n");
            foreach (BSPTexInfo tex in texinfo)
            {
                UnityEngine.Debug.Log(tex.ToString());
            }
        }
    }
}

