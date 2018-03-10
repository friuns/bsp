namespace bsp
{
    public class BSPFaceLump
    {
        public BSPFace[] faces;

        public BSPFaceLump()
        {
        }

        public void PrintInfo()
        {
            UnityEngine.Debug.Log("Faces:\r\n");
            foreach (BSPFace face in faces)
            {
                UnityEngine.Debug.Log(face.ToString());
            }
        }
    }
}

