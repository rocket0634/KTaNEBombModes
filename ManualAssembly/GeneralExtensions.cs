using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class GeneralExtensions
{
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
    {
        return source.OrderBy(x => Random.value);
    }
}
