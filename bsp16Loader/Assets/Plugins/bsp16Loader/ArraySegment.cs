using System;

namespace bsp
{
    
    public class ArrayOffset<T>
    {
        public ArrayOffset(int cnt)
        {
            array = new T[cnt];            
        }
        public void Add(T[] d)
        {
            for (int i = 0; i < d.Length; i++)
                array[offset++] = d[i];
            
        }
        
        public T[] array;
        public int offset;
        public T[] ToArray()
        {
            Array.Resize(ref array, offset);
            return array;
        }
    }
}