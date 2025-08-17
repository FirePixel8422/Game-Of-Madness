using System.Collections;
using UnityEngine;


/// <summary>
/// Component to identify the surface type of a GameObject.
/// </summary>
public class SurfaceTypeIdentifier : MonoBehaviour
{
    [SerializeField] private SurfaceType surfaceType;
    public SurfaceType SurfaceType => surfaceType;



#if UNITY_EDITOR
    private void Start()
    {
        // If there are no colliders on this GameObject, log a warning in the editor.
        if (transform.TryGetComponents(out Collider[] colliders) && colliders.Length == 0)
        {
            DebugLogger.LogWarning($"GameObject '{gameObject.name}' has no colliders. " + "SurfaceTypeIdentifier is useless.");
        }
    }
#endif
}