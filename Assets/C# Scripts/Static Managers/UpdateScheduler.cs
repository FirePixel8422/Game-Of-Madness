using System;
using UnityEngine;


/// <summary>
/// Uitlity class to have an optimized easy acces to Updte Callbacks by using an Action based callback system
/// </summary>
public static class UpdateScheduler
{
#pragma warning disable UDR0002
    private static Action OnUpdate;
    private static Action OnFixedUpdate;
#pragma warning restore UDR0002


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        UpdateCallbackManager gameManager = new GameObject("UpdateCallbackManager").AddComponent<UpdateCallbackManager>();
        gameManager.gameObject.isStatic = true;

        GameObject.DontDestroyOnLoad(gameManager.gameObject);
    }


    /// <summary>
    /// Register a method to call every frame like Update()
    /// </summary>
    public static void RegisterUpdate(Action action)
    {
#pragma warning disable UDR0004
        OnUpdate += action;
#pragma warning restore UDR0004
    }
    /// <summary>
    /// Unregister a registerd method for Update()
    /// </summary>
    public static void UnregisterUpdate(Action action)
    {
        OnUpdate -= action;
    }
    /// <summary>
    /// Register or Unregister a method for Update() based on bool <paramref name="register"/>
    /// </summary>
    public static void ManageUpdate(Action action, bool register)
    {
        if (register)
        {
            RegisterUpdate(action);
        }
        else
        {
            UnregisterUpdate(action);
        }
    }

    /// <summary>
    /// Register a method to call every frame like FixedUpdate()
    /// </summary>
    public static void RegisterFixedUpdate(Action action)
    {
#pragma warning disable UDR0004
        OnFixedUpdate += action;
#pragma warning restore UDR0004
    }
    /// <summary>
    /// Unregister a registerd method for FixedUpdate()
    /// </summary>
    public static void UnregisterFixedUpdate(Action action)
    {
        OnFixedUpdate -= action;
    }
    /// <summary>
    /// Register or Unregister a method for FixedUpdate() based on bool <paramref name="register"/>
    /// </summary>
    public static void ManageFixedUpdate(Action action, bool register)
    {
        if (register)
        {
            RegisterFixedUpdate(action);
        }
        else
        {
            UnregisterFixedUpdate(action);
        }
    }


    /// <summary>
    /// Handle Update Callbacks and batch them for every script by an event based register system
    /// </summary>
    private class UpdateCallbackManager : MonoBehaviour
    {
        private void Update()
        {
            OnUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        private void OnDestroy()
        {
            OnUpdate = null;
            OnFixedUpdate = null;
        }
    }
}