using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;



[BurstCompile(DisableSafetyChecks = true)]
public static class CullingUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExtractFrustumPlanes(ref NativeArray<Plane> planes, Matrix4x4 worldToCameraMatrix, Matrix4x4 projectionMatrix)
    {
        Matrix4x4 vp = projectionMatrix * worldToCameraMatrix;

        // Local helper
        static Plane CreatePlane(float a, float b, float c, float d)
        {
            float3 n = new float3(a, b, c);
            float invLen = math.rsqrt(math.dot(n, n));
            return new Plane(n * invLen, d * invLen);
        }

        // Left
        planes[0] = CreatePlane(vp.m30 + vp.m00, vp.m31 + vp.m01, vp.m32 + vp.m02, vp.m33 + vp.m03);
        // Right
        planes[1] = CreatePlane(vp.m30 - vp.m00, vp.m31 - vp.m01, vp.m32 - vp.m02, vp.m33 - vp.m03);
        // Bottom
        planes[2] = CreatePlane(vp.m30 + vp.m10, vp.m31 + vp.m11, vp.m32 + vp.m12, vp.m33 + vp.m13);
        // Top
        planes[3] = CreatePlane(vp.m30 - vp.m10, vp.m31 - vp.m11, vp.m32 - vp.m12, vp.m33 - vp.m13);
        // Near
        planes[4] = CreatePlane(vp.m30 + vp.m20, vp.m31 + vp.m21, vp.m32 + vp.m22, vp.m33 + vp.m23);
        // Far
        planes[5] = CreatePlane(vp.m30 - vp.m20, vp.m31 - vp.m21, vp.m32 - vp.m22, vp.m33 - vp.m23);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TestPlanesAABB(NativeArray<Plane> planes, float3 center, float3 extents)
    {
        for (int i = 0; i < 6; i++)
        {
            Plane p = planes[i];
            float3 normal = p.normal;
            float distance = p.distance;

            // Compute the projected radius onto the plane normal
            float r = math.abs(extents.x * normal.x) +
                      math.abs(extents.y * normal.y) +
                      math.abs(extents.z * normal.z);

            // Signed distance from box center to plane
            float s = math.dot(normal, center) + distance;

            // If completely outside plane, cull it
            if (s + r < 0f)
                return false;
        }

        return true;
    }
}