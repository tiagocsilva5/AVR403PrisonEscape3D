#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RenameBonePrefix
{
    [MenuItem("Tools/Rename Selected Bone Prefix")]
    private static void Rename()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null) return;

        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (!bone.name.StartsWith("mixamorig8:")) continue;

            Undo.RecordObject(bone.gameObject, "Rename bone");
            bone.name = "mixamorig:" + bone.name.Substring("mixamorig8:".Length);
            EditorUtility.SetDirty(bone.gameObject);
        }
    }
}
#endif
