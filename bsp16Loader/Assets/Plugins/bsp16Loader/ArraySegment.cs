using System;

namespace bsp
{
    
    public class ArrayOffset<T>
    {
        public ArrayOffset(int cnt)
        {
            array = new T[cnt];            
        }
        public void Add(T d)
        {
            array[length++] = d;
        }
        
        public void Add(T[] d)
        {
            for (int i = 0; i < d.Length; i++)
                array[length++] = d[i];
        }
        
        public T[] array;
        public int length;
        public T[] ToArray()
        {
            Array.Resize(ref array, length);
            return array;
        }
    }
}