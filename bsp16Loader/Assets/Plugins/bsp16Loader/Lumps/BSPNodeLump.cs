namespace bsp
{
    public class BSPNodeLump
    {
        public BSPNode[] nodes;
	
        public BSPNodeLump ()
        {
        }

        public void PrintInfo(){
            foreach (BSPNode node in nodes)
            {
                UnityEngine.Debug.Log(node.ToString());
            }
        }
    }
}

