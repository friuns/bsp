namespace bsp
{
    public class BSPEntityLump
    {
        public string rawEntities;

        public BSPEntityLump(char[] ents)
        {
            this.rawEntities = new string(ents);
        }
    }
}

