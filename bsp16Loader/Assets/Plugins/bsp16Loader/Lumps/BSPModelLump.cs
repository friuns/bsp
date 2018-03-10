namespace bsp
{
    public class BSPModelLump
    {
        public BSPModel[] models;

        public BSPModelLump() { }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("Models:\r\n");
            foreach (BSPModel model in models)
            {
                UnityEngine.Debug.Log("Model - Leafs: " + model.numLeafs.ToString() + " Nodes: " + model.nodes[0] + ", " + model.nodes[1] + ", " + model.nodes[2] + ", " + model.nodes[3]);
            }
        }
    }
}

