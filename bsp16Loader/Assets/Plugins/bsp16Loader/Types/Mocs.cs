#if console

    public class Texture2D 
    {
        public FilterMode filterMode;

        public Texture2D(int Width, int Height)
        {
        }

        public Texture2D(int Width, int Height, TextureFormat rGB24, bool v) : this(Width, Height)
        {
        }

        public void SetPixels(Color[] Pixels)
        {

        }
        public void Apply()
        {
        }
        public void SetPixels32(Color32[] Colors)
        {
        }
    }
#endif
