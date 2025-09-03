using System;
using System.Collections.Generic;
using _Pyrenees.Scripts;
using UnityEngine;

public static class Extensions
{
    
    public static T RandomElement<T>(this T[] array)
    {
        if (array == null || array.Length == 0)
            throw new System.InvalidOperationException("Cannot select a random element from a null or empty array.");
        
        return array[UnityEngine.Random.Range(0, array.Length)];
    }
    
    public static string HierarchyPath(this GameObject obj)
    {
        if (obj == null) return string.Empty;

        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }


    /// <summary>
    /// Gets random value between Vector components
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static float Random(this Vector2 vector)
    {
        return UnityEngine.Random.Range(vector.x, vector.y);
    }
    
    public static float Range(this Vector2 vector2)
    {
        return vector2.y - vector2.x;
    }

    /// <summary>
    /// For nested transforms, calculates & sets the necessary local scale to match the given world scale. Local rotation must be zero for this to work.
    /// </summary>
    public static void SetWorldScale(this Transform transform, Vector3 worldScale)
    {
        if (transform.parent == null)
        {
            transform.localScale = worldScale;
            return;
        }

        var parentScale = transform.parent.lossyScale;
        transform.localScale = new Vector3(worldScale.x / parentScale.x, worldScale.y / parentScale.y, worldScale.z / parentScale.z);
    }

    public static void Log(this string s)
    {
        Debug.Log(s);
    }
    
    public static Vector3 InverseTransformScale(this Transform parent, Vector3 vector)
    {
        var parentScale = parent.lossyScale;
        return new Vector3(vector.x / parentScale.x, vector.y / parentScale.y, vector.z / parentScale.z);
    }

    public static Vector3 Abs(this Vector3 vector3)
    {
        return new Vector3(Mathf.Abs(vector3.x), Mathf.Abs(vector3.y), Mathf.Abs(vector3.z));
    }

    public static void SlerpTowards(this Transform transform, Vector3 targetPosition, float speed = 1f)
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Slerp from current rotation to target rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }
    
    // public static bool IsNullOrEmpty(this ICollection collection)
    // {
    //     return collection is null || collection.Count < 1;
    // }
    
    public static float NormalizeValue01(this Vector2 range, float value)
    {
        return Mathf.Clamp01((value - range.x) / range.Range());
    }
    
    public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
    {
        return collection is null || collection.Count < 1;
    }
    
    public static float EvaluateWeighted(this AnimationCurve curve, float time, float weight)
    {
        return curve.Evaluate(time) * weight;
    }
    
    public static float Round(this float value, int digits = 1)
    {
        return (float)Math.Round(value, digits);
    }

    public static float Volume(this Bounds bounds)
    {
        return bounds.size.x * bounds.size.y * bounds.size.z;
    }

    public static float MeanMagnitude(this Vector3 vector3)
    {
        return (vector3.x + vector3.y + vector3.z) * 0.333333333333f;
    }
    
    /// <summary>
    /// Performs "naive", element-wise multiplication.
    /// </summary>
    public static Vector3 HadamardProduct(this Vector3 vectorA, Vector3 vectorB)
    {
        return new Vector3(vectorA.x * vectorB.x, vectorA.y * vectorB.y, vectorA.z * vectorB.z);
    }
    

    /// <summary>
    /// Removes the item at the given index by swapping it with the last element and removing the last.
    /// This is O(1) but does not preserve list order.
    /// </summary>
    public static void FastRemoveAt<T>(this IList<T> list, int index)
    {
        int lastIndex = list.Count - 1;
        if (index < 0 || index > lastIndex)
            throw new System.ArgumentOutOfRangeException(nameof(index));

        if (index < lastIndex)
            list[index] = list[lastIndex]; // Overwrite with last item

        list.RemoveAt(lastIndex); // Remove last
    }
    

    
}
