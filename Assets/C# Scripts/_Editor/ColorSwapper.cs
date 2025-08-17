#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;



public class ColorSwapper : MonoBehaviour
{
    [SerializeField] private Color colorA;
    [SerializeField] private Color colorB;

    [SerializeField] private bool swap;
    [SerializeField] private bool reverseSwap;


    [ContextMenu("Swap Colors")]
    private void SwapColors()
    {
        DebugLogger.Log("Swapping colors...");

        Component[] sceneComponents = FindObjectsOfType<Component>(true);

        foreach (Component comp in sceneComponents)
        {
            if (comp.TryGetComponent(out Image image))
            {
                if (image.color == colorA)
                {
                    image.color = colorB;
                }
            }
        }
    }

    [ContextMenu("Undo")]
    private void Undo()
    {
        DebugLogger.Log("Swapping colors...");

        Component[] sceneComponents = FindObjectsOfType<Component>(true);

        foreach (Component comp in sceneComponents)
        {
            if (comp.TryGetComponent(out Image image))
            {
                if (image.color == colorB)
                {
                    image.color = colorA;
                }
            }
        }
    }

    private void OnValidate()
    {
        if (swap)
        {
            swap = false;
            SwapColors();
        }
        if (reverseSwap)
        {
            reverseSwap = false;
            Undo();
        }
    }
}
#endif