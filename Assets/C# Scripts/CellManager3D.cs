using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.ParticleSystemJobs;


[BurstCompile]
public class CellManager3D : MonoBehaviour
{
    [Header("Game Rules")]
    [SerializeField] private GameRulesSO rules;

    [Header("Setting this to 0 will cause an update every frame")]
    [SerializeField] private float updateInterval = 0.02f;
    [Header("If true, wait for cycle completion before starting a new one")]
    [SerializeField] private bool updateAsynchronously = true;

    [Header("In how large groups does the job execute")]
    [SerializeField] private int batchSize = 256;
#if UNITY_EDITOR
    [SerializeField] private int DEBUG_totalBatchSize;
#endif

    [Header("If true, schedule new job after completion immediately. \nIf false, the update will be scheduled on next frame.")]
    [SerializeField] private bool preScheduleUpdates = true;

    [Header("Rendering")]
    [SerializeField] private Mesh cellMesh;
    [SerializeField] private Material cellMaterial;

    [SerializeField] private InstanceRenderer instanceRenderer;

    private int GridLength => rules.gridSize.x * rules.gridSize.y * rules.gridSize.z;

    private NativeArray<byte> cellStates;
    private NativeArray<CellData> newCellStateData;

    NativeArray<MinMaxInt> birthRange;
    NativeArray<MinMaxInt> survivalRange;

    private NativeList<Matrix4X4IdMapper> matrixIdMappers;
    private NativeList<int> cellIdsForRemoval;

    private JobHandle mainJobHandle;
    private Unity.Mathematics.Random random;

    [SerializeField] private float elapsedTime;
    private bool queueNewLifeUpdate;



    private void Awake()
    {
        uint seed = rules.seed;
        if (rules.reSeedOnStart)
        {
            seed = EzRandom.Seed();
        }
        random = new Unity.Mathematics.Random(seed);

        instanceRenderer = new InstanceRenderer(new Mesh[1] { cellMesh }, cellMaterial, GridLength, Camera.main);

        cellStates = new NativeArray<byte>(GridLength, Allocator.Persistent);
        newCellStateData = new NativeArray<CellData>(GridLength, Allocator.Persistent);

        birthRange = new NativeArray<MinMaxInt>(rules.birthRange, Allocator.Persistent);
        survivalRange = new NativeArray<MinMaxInt>(rules.survivalRange, Allocator.Persistent);

        matrixIdMappers = new NativeList<Matrix4X4IdMapper>(GridLength, Allocator.Persistent);
        cellIdsForRemoval = new NativeList<int>(GridLength, Allocator.Persistent);

        queueNewLifeUpdate = true;
    }
    private void SelectRandomAliveCells()
    {
        int cellCount = GridLength;

        for (int i = 0; i < cellCount; i++)
        {
            if (rules.initialCellAliveChance >= random.NextFloat(0, 100))
            {
                newCellStateData[i] = new CellData(true, true);
            }
        }
    }

    private void OnEnable() => UpdateScheduler.RegisterUpdate(OnUpdate);
    private void OnDisable() => UpdateScheduler.UnregisterUpdate(OnUpdate);

    private void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            queueNewLifeUpdate = true;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime < updateInterval || (updateAsynchronously && mainJobHandle.IsCompleted == false)) return;
#if UNITY_EDITOR
        lastCycleCPUMs = (int)(elapsedTime * 1000);
#endif
        elapsedTime = 0;

        mainJobHandle.Complete();

        if (queueNewLifeUpdate)
        {
            queueNewLifeUpdate = false;
            SelectRandomAliveCells();

            matrixIdMappers.Clear();
            cellIdsForRemoval.Clear();

            new CalculateCellMatricesJobParallelBatched
            {
                cellStates = cellStates,
                newCellStateData = newCellStateData,

                matrixIdMappers = matrixIdMappers.AsParallelWriter(),
                cellIdsForRemoval = cellIdsForRemoval.AsParallelWriter(),

                gridSize = rules.gridSize,
            }
            .Schedule(GridLength, batchSize)
            .Complete();

            var _ = new JobHandle();

            instanceRenderer.UpdateMeshMatricesBulk(0, matrixIdMappers.AsArray(), cellIdsForRemoval.AsArray(), ref _);
        }

        matrixIdMappers.Clear();
        cellIdsForRemoval.Clear();
    

        var cellCycleJob = new UpdateCellCycleJobParallelBatched
        {
            cellStates = cellStates,

            birthRange = birthRange,
            survivalRange = survivalRange,

            newCellStateData = newCellStateData,

            gridSize = rules.gridSize,
            wrapGrid = rules.wrapGrid,
        };

        var calculateMatrixJob = new CalculateCellMatricesJobParallelBatched
        {
            cellStates = cellStates,
            newCellStateData = newCellStateData,

            matrixIdMappers = matrixIdMappers.AsParallelWriter(),
            cellIdsForRemoval = cellIdsForRemoval.AsParallelWriter(),

            gridSize = rules.gridSize,
        };

        var cellCycleJobHandle = cellCycleJob.Schedule(GridLength, batchSize);
        var matrixJobHandle = calculateMatrixJob.Schedule(GridLength, batchSize, cellCycleJobHandle);

        instanceRenderer.UpdateMeshMatricesBulk(0, matrixIdMappers.AsDeferredJobArray(), cellIdsForRemoval.AsDeferredJobArray(), ref matrixJobHandle);
    }

    private void OnDestroy()
    {
        mainJobHandle.Complete();

        instanceRenderer.Dispose();

        cellStates.DisposeIfCreated();
        newCellStateData.DisposeIfCreated();
        
        birthRange.DisposeIfCreated();
        survivalRange.DisposeIfCreated();

        matrixIdMappers.DisposeIfCreated();
        cellIdsForRemoval.DisposeIfCreated();
    }


#if UNITY_EDITOR
    private void OnValidate()
    {
        DEBUG_totalBatchSize = GridLength;
    }

    [Header("DEBUG")]
    [SerializeField] private int lastCycleCPUMs;

    [SerializeField] private bool drawGizmos = true;

    private void OnDrawGizmos()
    {
        if (drawGizmos == false || rules == null) return;

        Gizmos.DrawWireCube(Vector3.zero, new Vector3(rules.gridSize.x, rules.gridSize.y, rules.gridSize.z));
    }
#endif
}
