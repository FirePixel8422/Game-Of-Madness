using UnityEngine;



/// <summary>
/// Performance optimized DebugLogger Logger that only logs messages when conditional "DEBUG" argument is provided in build settings.
/// </summary>
public static class DebugLogger
{
    private const string ScriptingDefineSymbol = "Enable_Debug_Logging";

    [System.Diagnostics.Conditional(ScriptingDefineSymbol)]
    public static void Log(string message)
    {
        Debug.Log(message);
    }

    [System.Diagnostics.Conditional(ScriptingDefineSymbol)]
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional(ScriptingDefineSymbol)]
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}