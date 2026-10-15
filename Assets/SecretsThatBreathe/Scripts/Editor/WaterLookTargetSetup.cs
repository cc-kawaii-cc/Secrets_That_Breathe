using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class WaterLookTargetSetup
    {
        private static bool queued;
        static WaterLookTargetSetup()
        {
            QueueInstall();
            EditorSceneManager.sceneOpened += (scene, mode) => QueueInstall();
            EditorApplication.hierarchyChanged += QueueInstall;
        }
        private static void QueueInstall()
        {
            if (queued || EditorApplication.isPlayingOrWillChangePlaymode) return;
            queued = true;
            EditorApplication.delayCall += () => { queued = false; Install(); };
        }
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(LawyerRoomSeatingSetup.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) return;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var machine in root.GetComponentsInChildren<WaterVendingMachineInteraction>(true))
                {
                    if (machine.lookTarget != null) continue;
                    var existing = machine.transform.Find("LookTarget");
                    GameObject target;
                    if (existing == null)
                    {
                        target = new GameObject("LookTarget");
                        Undo.RegisterCreatedObjectUndo(target, "Create water machine crosshair target");
                        target.transform.SetParent(machine.transform, false);
                    }
                    else target = existing.gameObject;
                    var box = target.GetComponent<BoxCollider>();
                    if (box == null)
                    {
                        box = Undo.AddComponent<BoxCollider>(target);
                        Bounds localBounds = default;
                        bool found = false;
                        foreach (var renderer in machine.GetComponentsInChildren<Renderer>(true))
                        {
                            Bounds world = renderer.bounds;
                            for (int corner = 0; corner < 8; corner++)
                            {
                                Vector3 point = world.center + Vector3.Scale(world.extents, new Vector3(
                                    (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                                point = target.transform.InverseTransformPoint(point);
                                if (!found) { localBounds = new Bounds(point, Vector3.zero); found = true; }
                                else localBounds.Encapsulate(point);
                            }
                        }
                        box.center = localBounds.center;
                        box.size = found ? localBounds.size : Vector3.one;
                        box.isTrigger = true;
                    }
                    Undo.RecordObject(machine, "Assign water machine crosshair target");
                    machine.lookTarget = box;
                    EditorUtility.SetDirty(machine);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(machine);
                    EditorSceneManager.MarkSceneDirty(scene);
                }
        }
    }
}
