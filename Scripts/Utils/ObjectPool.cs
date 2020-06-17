using System.Collections.Generic;

namespace ProjectorForLWRP
{
    public class ObjectPool<T> where T : new()
    {
        Stack<T> m_pool = new Stack<T>();
        public T Get()
        {
            if (0 < m_pool.Count)
            {
                return m_pool.Pop();
            }
            return new T();
        }
        public void Release(T obj)
        {
            if (obj != null)
            {
                m_pool.Push(obj);
            }
        }
        public void Clear()
        {
            m_pool.Clear();
        }
    }
}
