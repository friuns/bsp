using System;

namespace bsp
{
    public struct ArraySegment<T>
    {
        public T[] array;
        public int offset;
        public int len;
        public T this[int index]
        {
            get
            {
                if (index > len) throw new ArgumentOutOfRangeException(index + ">" + len);
                return array[offset + index];
            }
            set
            {
                if (index > len) throw new ArgumentOutOfRangeException(index + ">" + len);
                array[offset + index] = value;
            }
        }
    }
    public class ArrayOffset<T>
    {
        public ArraySegment<T> GetNextSegment(int len)
        {
            var segment = new ArraySegment<T>();
            segment.array = array;
            segment.offset = offset;
            segment.len = len;
            offset += len;
            return segment;
        }
        public T[] array = new T[1000];
        public int offset;
        public T[] ToArray()
        {
            Array.Resize(ref array, offset);
            return array;
        }
    }
}