namespace System.Collections.Generic;

public class FancyList<T> : List<T>
{
    public FancyList()
        : base() {}
    
    public FancyList(IEnumerable<T> collection)
        : base(collection) {}
    
    public FancyList(int capacity)
        : base(capacity) {}
    
    public static FancyList<T> operator +(FancyList<T> list, T item)
    {
        list.Add(item);
        return list;
    }
    
    public static FancyList<T> operator +(FancyList<T> list, IEnumerable<T> items)
    {
        list.AddRange(items);
        return list;
    }
    
    public static FancyList<T> operator -(FancyList<T> list, T item)
    {
        list.Remove(item);
        return list;
    }
    
    public static FancyList<T> operator -(FancyList<T> list, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            list.Remove(item);
        }
        return list;
    }
}