#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DebugDataDisplay))]
public class DebugDataDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DebugDataDisplay debugDataDisplay = (DebugDataDisplay)target;

        if (GUILayout.Button("Reload Expensive Stats"))
        {
            debugDataDisplay.ReloadExpensiveStats();
        }
    }
}
#endif
