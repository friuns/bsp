namespace bsp
{
    public class BSPMarkSurfaces
    {
        public int[] markSurfaces;

        public BSPMarkSurfaces()
        {
        }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("MarkSurfaces:\r\n");
            foreach (int msurface in markSurfaces)
            {
                UnityEngine.Debug.Log(msurface.ToString());
            }
        }
    }
}
