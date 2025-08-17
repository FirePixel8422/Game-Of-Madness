using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;


[BurstCompile]
public static class VectorLogic
{
    /// <summary>
    /// Instantly move a vector3 towards the new Vector3, up to maxDistance
    /// </summary>
    /// <returns>The new Position</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 InstantMoveTowards(Vector3 from, Vector3 to, float maxDist)
    {
        // Calculate the direction vector and its magnitude
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        // If the distance is less than or equal to maxDist, move directly to the target
        if (distance <= maxDist)
        {
            return to;
        }

        // Normalize the direction and scale by maxDist
        Vector3 move = direction.normalized * maxDist;

        // Return the new position
        return from + move;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Clamp(this Vector3 value, Vector3 min, Vector3 max)
    {
        value.x = math.clamp(value.x, min.x, max.x);
        value.y = math.clamp(value.y, min.y, max.y);
        value.z = math.clamp(value.z, min.z, max.z);

        return value;
    }



    public static Vector3 ClampDirection(this Vector3 value, Vector3 clamp)
    {
        // Calculate the scale factors for each axis
        float scaleX = math.abs(value.x) > clamp.x ? math.abs(clamp.x / value.x) : 1f;
        float scaleY = math.abs(value.y) > clamp.y ? math.abs(clamp.y / value.y) : 1f;
        float scaleZ = math.abs(value.z) > clamp.z ? math.abs(clamp.z / value.z) : 1f;

        // Use the smallest scale factor to preserve direction
        float scale = math.min(scaleX, math.min(scaleY, scaleZ));

        // Scale the vector uniformly
        return value * scale;
    }

    public static float3 ClampDirection(this float3 value, float3 clamp)
    {
        // Calculate the scale factors for each axis
        float scaleX = math.abs(value.x) > clamp.x ? math.abs(clamp.x / value.x) : 1f;
        float scaleY = math.abs(value.y) > clamp.y ? math.abs(clamp.y / value.y) : 1f;
        float scaleZ = math.abs(value.z) > clamp.z ? math.abs(clamp.z / value.z) : 1f;

        // Use the smallest scale factor to preserve direction
        float scale = math.min(scaleX, math.min(scaleY, scaleZ));

        // Scale the vector uniformly
        return value * scale;
    }


    /// <summary>
    /// Turn 3d vector into 2d vector
    /// </summary>
    /// <returns>The Vector as 2d Vector2</returns>
    public static Vector2 ToVector2(this Vector3 value)
    {
        return new Vector2(value.x, value.z);
    }
    public static Vector2 ToRoundedVector2(this Vector3 value)
    {
        return new Vector2(math.round(value.x), math.round(value.z));
    }
    public static Vector2 ToRoundedVector2(this Vector2 value)
    {
        return new Vector2(math.round(value.x), math.round(value.y));
    }


    /// <summary>
    /// Turn 2d vector into 3d vector
    /// </summary>
    /// <returns>The Vector as 3d Vector3 (sets y to 0)</returns>
    public static Vector3 ToVector3(this Vector2 value)
    {
        return new Vector3(value.x, 0, value.y);
    }

    /// <summary>
    /// Turn 2d vector into 3d vector and set vector3.y equal to yValue
    /// </summary>
    /// <returns>The Vector as 3d Vector3</returns>
    public static Vector3 ToVector3(this Vector2 value, float yValue)
    {
        return new Vector3(value.x, yValue, value.y);
    }

    /// <summary>
    /// Turn float3 into 3d vector
    /// </summary>
    /// <returns>The Vector3</returns>
    public static Vector3 ToVector3(this float3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }
}
