using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerationDonjon))]
public class GenerationDonjonEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GenerationDonjon script = (GenerationDonjon)target;

        if (GUILayout.Button("Randomize Seed"))
        {
            script.RandomizeSeed();
        }
    }
}
