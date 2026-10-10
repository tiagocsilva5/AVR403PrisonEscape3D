using UnityEditor;
using UnityEngine;

public static class PrisonScreenTools
{
    [MenuItem("Tools/Prison Escape/Create Level Exit")]
    private static void CreateExit() { CreateZone<LevelExit>("Level Exit", new Vector3(3f, 3f, 2f)); }

    [MenuItem("Tools/Prison Escape/Create Death Zone")]
    private static void CreateDeath() { CreateZone<PlayerDeathZone>("Death Zone", new Vector3(20f, 2f, 20f)); }

    private static void CreateZone<T>(string name, Vector3 size) where T : Component
    {
        GameObject zone = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(zone, "Create " + name);
        if (Selection.activeTransform != null)
            zone.transform.position = Selection.activeTransform.position;
        Undo.AddComponent<T>(zone);
        zone.GetComponent<BoxCollider>().isTrigger = true;
        zone.GetComponent<BoxCollider>().size = size;
        zone.GetComponent<Rigidbody>().isKinematic = true;
        zone.GetComponent<Rigidbody>().useGravity = false;
        Selection.activeGameObject = zone;
    }

    [MenuItem("Tools/Prison Escape/Preview Screens/Start")]
    private static void PreviewStart() { GameScreens.Instance.ShowStartScreen(); }

    [MenuItem("Tools/Prison Escape/Preview Screens/Caught")]
    private static void PreviewCaught() { GameScreens.Instance.ShowCaught(1, 3, () => { }); }

    [MenuItem("Tools/Prison Escape/Preview Screens/Death")]
    private static void PreviewDeath() { GameScreens.Instance.ShowDeath(); }

    [MenuItem("Tools/Prison Escape/Preview Screens/Game Over")]
    private static void PreviewGameOver() { GameScreens.Instance.ShowGameOver(3); }

    [MenuItem("Tools/Prison Escape/Preview Screens/Next Level")]
    private static void PreviewNext() { GameScreens.Instance.ShowNextLevel("Scene 2"); }

    [MenuItem("Tools/Prison Escape/Preview Screens/Start", true)]
    [MenuItem("Tools/Prison Escape/Preview Screens/Caught", true)]
    [MenuItem("Tools/Prison Escape/Preview Screens/Death", true)]
    [MenuItem("Tools/Prison Escape/Preview Screens/Game Over", true)]
    [MenuItem("Tools/Prison Escape/Preview Screens/Next Level", true)]
    private static bool CanPreview() { return EditorApplication.isPlaying; }
}
