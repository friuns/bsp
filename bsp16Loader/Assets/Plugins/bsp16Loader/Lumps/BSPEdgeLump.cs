namespace bsp
{
    public class BSPEdgeLump
    {
        public BSPEdge[] edges;
        public int[] SURFEDGES;

        public BSPEdgeLump()
        {
        }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("Edges:\r\n");
            foreach (BSPEdge edge in edges)
            {
                UnityEngine.Debug.Log(edge.ToString());
            }

            UnityEngine.Debug.Log("Ledges:\r\n");
            foreach (short ledge in SURFEDGES)
            {
                UnityEngine.Debug.Log(ledge.ToString());
            }
        }
    }
}

