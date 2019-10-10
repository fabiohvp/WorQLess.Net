namespace System.Collections.Generic
{
    public static class IDictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic1, IDictionary<TKey, TValue> dic2)
        {
            foreach (var item in dic2)
            {
                dic1.Add(item.Key, item.Value);
            }
        }
    }
}
