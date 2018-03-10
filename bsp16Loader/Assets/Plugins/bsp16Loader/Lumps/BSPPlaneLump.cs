namespace bsp
{
    public class BSPPlaneLump
    {
        public BSPPlane[] planes;

        public BSPPlaneLump() { }

        public void PrintInfo()
        {
            foreach (BSPPlane plane in planes)
            {
                UnityEngine.Debug.Log(plane.ToString());
            }
        }
    }
}

