using UnityEngine;
using UnityEditor;

public class DebugListCanvas
{
    [MenuItem("AI-Tools/Debug List Canvas")]
    public static void ListCanvas()
    {
        var objs = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        string result = "Canvas count: " + objs.Length;
        foreach (var c in objs)
        {
            result += "\n" + c.gameObject.name + " active:" + c.gameObject.activeSelf;
            foreach (Transform child in c.transform)
                result += "\n  " + child.name + " active:" + child.gameObject.activeSelf;
        }
        Debug.Log(result);
    }
}
