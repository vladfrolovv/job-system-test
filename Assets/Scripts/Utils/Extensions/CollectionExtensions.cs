using System;
using System.Collections.Generic;

namespace Utils.Extensions
{
    public static class CollectionExtensions
    {
        public static void Shuffle<T>(this IList<T> list)  
        {  
            int n = list.Count;
            Random random = new();
            while (n > 1) {  
                n--;  
                int k = random.Next(0, n + 1);  
                (list[k], list[n]) = (list[n], list[k]);
            }  
        }
    }
}