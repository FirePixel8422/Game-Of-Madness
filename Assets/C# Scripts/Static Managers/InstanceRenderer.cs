using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


[System.Serializable]
[BurstCompile]
public class InstanceRenderer
{
    public InstanceRenderer(Mesh[] _meshes, Material mat, int _perMeshArraySize, Camera targetCamera = null)
    {
        meshes = _meshes;

        meshCount = _meshes.Length;
        perMeshArraySize = _perMeshArraySize;

        material = mat;
        mpb = new MaterialPropertyBlock();

        SetupMatrixData(targetCamera);

        UpdateScheduler.RegisterUpdate(OnUpdate);

#if UNITY_EDITOR
        DEBUG_enabled = true;
#endif
    }

    private void SetupMatrixData(Camera targetCamera)
    {
        int totalArraySize = perMeshArraySize * meshCount;

        matrixKeys = new NativeArray<int>(totalArraySize, Allocator.Persistent);
        cellIdKeys = new NativeArray<int>(totalArraySize, Allocator.Persistent);

        IntArrayFillJobParallel fillMatrixKeys = new IntArrayFillJobParallel()
        {
            array = matrixKeys,
            value = -1,
        };

        IntArrayFillJobParallel fillCellIdKeys = new IntArrayFillJobParallel()
        {
            array = cellIdKeys,
            value = -1,
        };

        JobHandle fillArraysJobHandle = JobHandle.CombineDependencies(
            fillCellIdKeys.Schedule(totalArraySize, 1024),
            fillMatrixKeys.Schedule(totalArraySize, 1024)
            );

        matrices = new NativeArray<Matrix4x4>(totalArraySize, Allocator.Persistent);
        matrixCounts = new NativeArray<int>(meshCount, Allocator.Persistent);

        culledInstanceMatrices = new NativeList<Matrix4x4>(perMeshArraySize, Allocator.Persistent);
        toDrawMatrices = new Matrix4x4[perMeshArraySize];

        frustumPlanes = new NativeArray<FastFrustumPlane>(6, Allocator.Persistent);
        frustumPlanesArray = new Plane[6];

        // Set cam to targetCamera or main camera if its null
        cam = targetCamera == null ? Camera.main : targetCamera;
        lastCamPos = cam.transform.position;
        lastCamRot = cam.transform.rotation;

        fillArraysJobHandle.Complete();
    }




    private readonly Mesh[] meshes;

    private readonly int meshCount;
    private readonly int perMeshArraySize;

    private readonly Material material;
    private MaterialPropertyBlock mpb;

    [Tooltip("Flattened array that acts as multiple arrays, 1 for every mesh accesed by meshId multiplied by perMeshArraySize")]
    private NativeArray<Matrix4x4> matrices;

    [Tooltip("CellId to MatrixId")]
    private NativeArray<int> matrixKeys;

    [Tooltip("MatrixId to CellId")]
    private NativeArray<int> cellIdKeys;

    [Tooltip("Number of instances for each mesh")]
    private NativeArray<int> matrixCounts;


    [Tooltip("List that holds calculated matrices that are in camera frustum for target mesh instance")]
    private NativeList<Matrix4x4> culledInstanceMatrices;
    [Tooltip("Managed conversion of culledInstanceMatrices, so the URP method understands the data")]
    private Matrix4x4[] toDrawMatrices;

    private Camera cam;
    private Vector3 lastCamPos;
    private Quaternion lastCamRot;

    private NativeArray<FastFrustumPlane> frustumPlanes;
    private Plane[] frustumPlanesArray;


    private void OnUpdate()
    {
        bool camMoved = cam.transform.position != lastCamPos || cam.transform.rotation != lastCamRot;

        //only if camera has moved or rotated, recalculate frustum planes
        if (camMoved)
        {
            lastCamPos = cam.transform.position;
            lastCamRot = cam.transform.rotation;

            GeometryUtility.CalculateFrustumPlanes(cam, frustumPlanesArray);
            for (int i = 0; i < 6; i++)
            {
                frustumPlanes[i] = new FastFrustumPlane(frustumPlanesArray[i].normal, frustumPlanesArray[i].distance);
            }
        }


        for (int meshId = 0; meshId < meshCount; meshId++)
        {
            int meshInstanceCount = matrixCounts[meshId];

            //skip currentmesh if there are 0 instances of it (nothing to render)
            if (meshInstanceCount == 0) continue;

            //Frustom Culling job
            var frustomCullingJob = new FrustumCullingJobParallel
            {
                meshBounds = meshes[meshId].bounds,
                frustumPlanes = frustumPlanes,

                matrices = matrices,
                startIndex = meshId * perMeshArraySize,

                culledMatrices = culledInstanceMatrices.AsParallelWriter(),
            };

            frustomCullingJob.Schedule(meshInstanceCount, 1024).Complete();

            int culledMatrixCount = culledInstanceMatrices.Length;

            // If no mesh instances are visible, skip rendering that mesh
            if (culledMatrixCount != 0)
            {
                RenderMeshInstance(meshId, culledMatrixCount);

                // Reset visibleMeshMatrices List to allow filling it with new data
                culledInstanceMatrices.Clear();
            }
        }

#if UNITY_EDITOR
        if (DEBUG_enabled == false) return;
        if (perMeshArraySize > 100)
        {
            DebugLogger.LogWarning(">>Instance Renderer<< \nAttempted to display DEBUG data for too large arrays, please lower the gridSize. Debug display is now disabled!");
            DEBUG_enabled = false;
            return;
        }

        DEBUG_matrices = matrices.ToArray();
        DEBUG_matrixKeys = matrixKeys.ToArray();
        DEBUG_cellIdKeys = cellIdKeys.ToArray();
        DEBUG_matrixCounts = matrixCounts.ToArray();
#endif
    }


    /// <summary>
    /// Render all instances of meshId with matrixData from <see cref="culledInstanceMatrices"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RenderMeshInstance(int meshId, int instanceCount)
    {
        // Copy all culled native matrix data into managed array for DrawMeshInstanced
        for (int i = 0; i < instanceCount; i++)
        {
            toDrawMatrices[i] = culledInstanceMatrices[i];
        }

        // Max Supported Batches is 1023
        for (int startInstance = 0; startInstance < instanceCount; startInstance += 1023)
        {
            int batchCount = math.min(1023, instanceCount - startInstance);

            // If a second batch is made (when exceeding 1023 instances), copy the matrices to the start of the array because DrawMeshInstanced only reads the first 1023 matrices
            if (startInstance != 0)
            {
                for (int i = 0; i < batchCount; i++)
                {
                    toDrawMatrices[i] = toDrawMatrices[startInstance + i];
                }
            }

            Graphics.DrawMeshInstanced(meshes[meshId], 0, material, toDrawMatrices, batchCount, mpb, UnityEngine.Rendering.ShadowCastingMode.On, true);
        }
    }





    public void SetMeshInstanceMatrix(int meshId, int cellId, Matrix4x4 matrix)
    {
        // If matrixKeys[cellId] == -1, there is no mesh for that cell, so assign a new matrix 
        if (matrixKeys[cellId] == -1)
        {
            int matrixArrayIndex = meshId * perMeshArraySize + matrixCounts[meshId];

            // Save matrix to nest spot in matrixArray
            matrices[matrixArrayIndex] = matrix;

            // Save cellId to matrixArray in the same index
            cellIdKeys[matrixArrayIndex] = cellId;

            // Save matrixArray index to cellId in matrixKeys
            matrixKeys[cellId] = matrixArrayIndex;

            // Increment matrixCount for this mesh by 1
            matrixCounts[meshId] += 1;
        }
        // If matrixKeys[cellId] has a value, modify the equivelenat matrix instead of asigning a new one
        else
        {
            matrices[matrixKeys[cellId]] = matrix;
        }
    }

    public void UpdateMeshMatricesBulk(int meshId, NativeArray<Matrix4X4IdMapper> matrixIdMappers, NativeArray<int> cellIdsForRemoval, ref JobHandle waitCondition)
    {
        new SetMeshMatrixDataJob
        {
            matrixIdMappers = matrixIdMappers,
            cellIdsForRemoval = cellIdsForRemoval,

            matricesArray = matrices,

            cellIdKeys = cellIdKeys,
            matrixKeys = matrixKeys,
            matrixCounts = matrixCounts,

            meshId = meshId,
            perMeshArraySize = perMeshArraySize,
        }
        .Schedule(waitCondition)
        .Complete();
    }


    public void RemoveMeshInstanceMatrix(int toRemoveCellId)
    {
        int toRemoveMatrixId = matrixKeys[toRemoveCellId];
        int meshId = toRemoveMatrixId / perMeshArraySize;

        int lastMatrixId = meshId * perMeshArraySize + matrixCounts[meshId] - 1;
        int lastCellId = cellIdKeys[lastMatrixId];

        //swap last matrix with the one to be removed
        matrices[toRemoveMatrixId] = matrices[lastMatrixId];

        //swap last cellId with the one to be removed (get last cellId from lastMatrixId in cellIdKeys array)
        cellIdKeys[toRemoveMatrixId] = lastCellId;


        //swap last matrixKey with the one to be removed (get last cellId from lastMatrixId in cellIdKeys array)
        matrixKeys[lastCellId] = toRemoveMatrixId;

        //remove matrixKey for swapped from back matrix
        matrixKeys[toRemoveCellId] = -1;

        //update matrixCount for this mesh to reflect the removal
        matrixCounts[meshId] -= 1;
    }




    /// <summary>
    /// Dispose all native memory allocated and unregister from the update scheduler.
    /// </summary>
    public void Dispose()
    {
        matrices.DisposeIfCreated();
        matrixKeys.DisposeIfCreated();
        cellIdKeys.DisposeIfCreated();
        matrixCounts.DisposeIfCreated();
        culledInstanceMatrices.DisposeIfCreated();
        frustumPlanes.DisposeIfCreated();

        UpdateScheduler.UnregisterUpdate(OnUpdate);
    }




#if UNITY_EDITOR
    [SerializeField] private bool DEBUG_enabled = false;

    [Header ("Array consisting of multiple arrays, 1 for every mesh accesed by meshId multiplied by perMeshArraySize")]
    [SerializeField] private Matrix4x4[] DEBUG_matrices;

    [Header("CellId to MatrixId")]
    [SerializeField] private int[] DEBUG_matrixKeys;

    [Header("MatrixId to CellId")]
    [SerializeField] private int[] DEBUG_cellIdKeys;

    [Header("Number of instances for each mesh")]
    [SerializeField] private int[] DEBUG_matrixCounts;
#endif
}
