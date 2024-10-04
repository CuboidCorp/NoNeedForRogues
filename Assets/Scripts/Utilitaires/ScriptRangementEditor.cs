#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScriptRangement))]
public class ScriptRangementEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        ScriptRangement scriptRangement = (ScriptRangement)target;

        if (GUILayout.Button("Trier"))
        {
            scriptRangement.OnTri();
        }
    }
}
#endif
