using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Linq;

[CustomEditor(typeof(MonoBehaviour), true)]
public class InspectorButtonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var methods = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttributes(typeof(InspectorButtonAttribute), true).Length > 0
                        && m.GetParameters().Length == 0);

        foreach (var method in methods)
        {
            if (GUILayout.Button(method.Name))
            {
                method.Invoke(target, null);
            }
        }
    }
} 