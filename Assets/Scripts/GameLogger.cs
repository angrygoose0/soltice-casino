using UnityEngine;

/// <summary>
/// Centralized logging utility that can be disabled for production builds.
/// Use GameLogger.Log() instead of Debug.Log() to respect the logging flag.
/// </summary>
public static class GameLogger
{
    /// <summary>
    /// Set to false to disable all console logs (for production builds).
    /// Can also be controlled via scripting define symbols: DISABLE_LOGS
    /// </summary>
    public static bool EnableLogs = true;

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        if (EnableLogs)
            Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message, Object context)
    {
        if (EnableLogs)
            Debug.Log(message, context);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        if (EnableLogs)
            Debug.LogWarning(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message, Object context)
    {
        if (EnableLogs)
            Debug.LogWarning(message, context);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message)
    {
        if (EnableLogs)
            Debug.LogError(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(string message, Object context)
    {
        if (EnableLogs)
            Debug.LogError(message, context);
    }
}
