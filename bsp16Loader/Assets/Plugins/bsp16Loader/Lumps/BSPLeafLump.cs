namespace bsp
{
    public class BSPLeafLump
    {
        public BSPLeaf[] leafs;

        public BSPLeafLump()
        {
        }

        public void GetLeafInfo(int leaf)
        {
            UnityEngine.Debug.Log("Leaf " + leaf.ToString() + " " + leafs[leaf].ToString());
        }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("Leafs:\r\n");
            foreach (BSPLeaf leaf in leafs)
            {
                UnityEngine.Debug.Log(leaf.ToString());
            }
        }
    }
}

