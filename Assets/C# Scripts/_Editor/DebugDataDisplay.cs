#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

public class DebugDataDisplay : MonoBehaviour
{
    [SerializeField, Tooltip("Average over this many seconds")]
    private float avgTime = 1f;

    [Space(10)]

    [SerializeField] private string avgFps;
    [SerializeField] private string avgFrameMs;
    [SerializeField] private int drawCalls;
    [SerializeField] private int setPassCalls;
    [SerializeField] private int tris;

    [Header("Global Memory (MB)")]
    [SerializeField] private string totalAllocatedMemoryMB;
    [SerializeField] private string totalReservedMemoryMB;
    [SerializeField] private string totalUnusedReservedMemoryMB;
    [SerializeField] private string monoHeapSizeMB;
    [SerializeField] private string monoUsedSizeMB;

    [Space(10)]

    [Header("Component Counts:")]
    [SerializeField] private int gameObjectCount;
    [SerializeField] private int componentCount;
    [SerializeField] private int activeComponentCount;
    [SerializeField] private int activeAudioSources;

    [SerializeField] private int networkObjectCount;

    private static readonly CultureInfo enCulture = new CultureInfo("en-US");

    private struct FrameData
    {
        public float timestamp;
        public float deltaTime;
    }

    private readonly Queue<FrameData> frameTimes = new Queue<FrameData>();

    private void OnEnable()
    {
        UpdateScheduler.RegisterUpdate(OnUpdate);
        ReloadExpensiveStats(); // Initial load of expensive stats
    }

    private void OnDisable()
    {
        UpdateScheduler.UnregisterUpdate(OnUpdate);
    }

    private void OnUpdate()
    {
        float currentTime = Time.time;
        float deltaTime = Time.deltaTime;

        // Track frame times for rolling avg
        frameTimes.Enqueue(new FrameData { timestamp = currentTime, deltaTime = deltaTime });
        while (frameTimes.Count > 0 && currentTime - frameTimes.Peek().timestamp > avgTime)
            frameTimes.Dequeue();

        // Calculate averages
        float totalDeltaTime = 0f;
        foreach (var frame in frameTimes)
            totalDeltaTime += frame.deltaTime;

        int count = frameTimes.Count;
        if (count > 0)
        {
            float avgDeltaTime = totalDeltaTime / count;
            avgFrameMs = (avgDeltaTime * 1000f).ToString("F2", enCulture) + " ms";
            avgFps = (1f / avgDeltaTime).ToString("F1", enCulture) + " fps";
        }
        else
        {
            avgFrameMs = "0 ms";
            avgFps = "0 FPS";
        }

        // Cheap per-frame stats
        drawCalls = UnityEditor.UnityStats.drawCalls;
        setPassCalls = UnityEditor.UnityStats.setPassCalls;
        tris = UnityEditor.UnityStats.triangles;

        totalAllocatedMemoryMB = (Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)).ToString("N0", enCulture) + " mb";
        totalReservedMemoryMB = (Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)).ToString("N0", enCulture) + " mb";
        totalUnusedReservedMemoryMB = (Profiler.GetTotalUnusedReservedMemoryLong() / (1024 * 1024)).ToString("N0", enCulture) + " mb";
        monoHeapSizeMB = (Profiler.GetMonoHeapSizeLong() / (1024 * 1024)).ToString("N0", enCulture) + " mb";
        monoUsedSizeMB = (Profiler.GetMonoUsedSizeLong() / (1024 * 1024)).ToString("N0", enCulture) + " mb";
    }

    // Manual reload method to refresh expensive stats (call from inspector button)
    public void ReloadExpensiveStats()
    {
        gameObjectCount = FindObjectsOfType<GameObject>(true).Length;

        componentCount = FindObjectsOfType<Component>(true).Count(c => c is not Transform);
        activeComponentCount = FindObjectsOfType<Component>().Count(c => c is not Transform);

        activeAudioSources = 0;
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        foreach (var src in sources)
        {
            if (src.isPlaying)
            {
                activeAudioSources += 1;
            }
        }
    }
}
#endif