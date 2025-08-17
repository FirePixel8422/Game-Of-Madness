using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


[BurstCompile]
public struct SetMeshMatrixDataJob : IJob
{
    [ReadOnly][NoAlias] public NativeArray<Matrix4X4IdMapper> matrixIdMappers;
    [ReadOnly][NoAlias] public NativeArray<int> cellIdsForRemoval;

    [NativeDisableParallelForRestriction]
    [NoAlias] public NativeArray<Matrix4x4> matricesArray;

    [NativeDisableParallelForRestriction]
    [NoAlias] public NativeArray<int> cellIdKeys;

    [NoAlias] public NativeArray<int> matrixKeys;
    [NoAlias] public NativeArray<int> matrixCounts;

    public int meshId;
    public int perMeshArraySize;


    [BurstCompile]
    public void Execute()
    {
        for (int i = 0; i < matrixIdMappers.Length; i++)
        {
            matrixIdMappers[i].GetData(out Matrix4x4 targetMatrix, out int targetIndex);

            // If matrixKeys[cellId] == -1, there is no mesh for that cell, so assign a new matrix 
            if (matrixKeys[targetIndex] == -1)
            {
                int matrixArrayIndex = meshId * perMeshArraySize + matrixCounts[meshId];

                // Save matrix to nest spot in matrixArray
                matricesArray[matrixArrayIndex] = targetMatrix;

                // Save cellId to matrixArray in the same index
                cellIdKeys[matrixArrayIndex] = targetIndex;

                // Save matrixArray index to cellId in matrixKeys
                matrixKeys[targetIndex] = matrixArrayIndex;

                // Increment matrixCount for this mesh by 1
                matrixCounts[meshId] += 1;
            }
            // If matrixKeys[cellId] has a value, modify the equivelenat matrix instead of asigning a new one
            else
            {
                matricesArray[matrixKeys[targetIndex]] = targetMatrix;
            }
        }

        for (int i = 0; i < cellIdsForRemoval.Length; i++)
        {
            int toRemoveCellId = cellIdsForRemoval[i];

            int toRemoveMatrixId = matrixKeys[toRemoveCellId];
            int meshId = toRemoveMatrixId / perMeshArraySize;

            int lastMatrixId = meshId * perMeshArraySize + matrixCounts[meshId] - 1;
            int lastCellId = cellIdKeys[lastMatrixId];

            //swap last matrix with the one to be removed
            matricesArray[toRemoveMatrixId] = matricesArray[lastMatrixId];

            //swap last cellId with the one to be removed (get last cellId from lastMatrixId in cellIdKeys array)
            cellIdKeys[toRemoveMatrixId] = lastCellId;

            //swap last matrixKey with the one to be removed (get last cellId from lastMatrixId in cellIdKeys array)
            matrixKeys[lastCellId] = toRemoveMatrixId;

            //remove matrixKey for swapped from back matrix
            matrixKeys[toRemoveCellId] = -1;

            //update matrixCount for this mesh to reflect the removal
            matrixCounts[meshId] -= 1;
        }
    }
}