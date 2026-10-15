using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class RoomBoxLookTargetSetup
    {
        private static bool queued;
        static RoomBoxLookTargetSetup()
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
            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var chair in root.GetComponentsInChildren<ChairInteractable>(true))
                    if (chair.lookTarget == null || chair.lookTargetFitVersion < 1)
                    {
                        Undo.RecordObject(chair, "Assign chair box target");
                        if (chair.lookTarget == null) chair.lookTarget = CreateBox(chair.transform, "LookTarget", ref changed);
                        if (FitChairTarget(chair.transform, chair.lookTarget)) chair.lookTargetFitVersion = 1;
                        Save(chair); changed = true;
                    }
                foreach (var chair in root.GetComponentsInChildren<ChairInspectionInteractable>(true))
                    if (chair.lookTarget == null || chair.lookTargetFitVersion < 1)
                    {
                        Undo.RecordObject(chair, "Assign inspection chair box target");
                        if (chair.lookTarget == null) chair.lookTarget = CreateBox(chair.transform, "LookTarget", ref changed);
                        if (FitChairTarget(chair.transform, chair.lookTarget)) chair.lookTargetFitVersion = 1;
                        Save(chair); changed = true;
                    }
                foreach (var phone in root.GetComponentsInChildren<PhoneCallInteraction>(true))
                    if (phone.lookTarget == null)
                    {
                        Undo.RecordObject(phone, "Assign phone box target");
                        phone.lookTarget = Target(phone.transform, ref changed);
                        Save(phone); changed = true;
                    }
                foreach (var meal in root.GetComponentsInChildren<NoodleEatingController>(true))
                {
                    if (meal.noodles == null || (meal.lookTarget != null && meal.interactionZone != null)) continue;
                    Undo.RecordObject(meal, "Assign noodle cup interaction boxes");
                    if (meal.lookTarget == null) meal.lookTarget = Target(meal.noodles, ref changed);
                    if (meal.interactionZone == null)
                    {
                        var zone = CreateBox(meal.noodles, "InteractionZone", ref changed);
                        zone.transform.position = meal.lookTarget.transform.TransformPoint(meal.lookTarget.center);
                        var scale = zone.transform.lossyScale;
                        float diameter = Mathf.Max(.1f, meal.lookDistance) * 2f;
                        zone.size = new Vector3(diameter / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                            diameter / Mathf.Max(.001f, Mathf.Abs(scale.y)), diameter / Mathf.Max(.001f, Mathf.Abs(scale.z)));
                        zone.center = Vector3.zero;
                        meal.interactionZone = zone;
                    }
                    Save(meal); changed = true;
                }
                // Prepare editable targets for these models, without inventing new E actions.
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (item.name == "Main Door" || item.name == "MEMEEE_Empty") Target(item, ref changed);
            }
            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }
        private static void Save(Component component)
        {
            EditorUtility.SetDirty(component);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
        private static bool FitChairTarget(Transform owner, BoxCollider box)
        {
            var renderers = owner.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return false;
            // Use the visual model's axes, which may differ from its imported parent axes.
            Renderer basis = renderers[0];
            float largestVolume = -1f;
            foreach (var renderer in renderers)
            {
                Vector3 size = renderer.localBounds.size;
                Vector3 scale = renderer.transform.lossyScale;
                float volume = Mathf.Abs(size.x * size.y * size.z * scale.x * scale.y * scale.z);
                if (volume > largestVolume) { largestVolume = volume; basis = renderer; }
            }
            Undo.RecordObject(box.transform, "Align chair LookTarget with model");
            Undo.RecordObject(box, "Fit chair LookTarget in local coordinates");
            box.transform.SetPositionAndRotation(owner.position, basis.transform.rotation);
            Bounds fitted = default;
            bool found = false;
            foreach (var renderer in renderers)
            {
                // Start from local bounds, not the already-expanded world axis-aligned bounds.
                Bounds local = renderer.localBounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = local.center + Vector3.Scale(local.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    point = box.transform.InverseTransformPoint(renderer.transform.TransformPoint(point));
                    if (!found) { fitted = new Bounds(point, Vector3.zero); found = true; }
                    else fitted.Encapsulate(point);
                }
            }
            box.center = fitted.center;
            box.size = fitted.size;
            box.isTrigger = true;
            Save(box.transform);
            Save(box);
            return true;
        }
        private static BoxCollider CreateBox(Transform owner, string name, ref bool changed)
        {
            foreach (var candidate in owner.GetComponentsInChildren<BoxCollider>(true))
                if (candidate.name == name) return candidate;
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name + " box");
            go.transform.SetParent(owner, false);
            var box = Undo.AddComponent<BoxCollider>(go);
            box.isTrigger = true;
            changed = true;
            return box;
        }
        private static BoxCollider Target(Transform owner, ref bool changed)
        {
            foreach (var candidate in owner.GetComponentsInChildren<BoxCollider>(true))
                if (candidate.name == "LookTarget") return candidate;
            var box = CreateBox(owner, "LookTarget", ref changed);
            Bounds local = default;
            bool found = false;
            // Renderer bounds are read once during editor setup only, not for runtime aiming.
            foreach (var renderer in owner.GetComponentsInChildren<Renderer>(true))
            {
                Bounds world = renderer.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = world.center + Vector3.Scale(world.extents, new Vector3(
                        (corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    point = box.transform.InverseTransformPoint(point);
                    if (!found) { local = new Bounds(point, Vector3.zero); found = true; }
                    else local.Encapsulate(point);
                }
            }
            box.center = local.center;
            box.size = found ? local.size : Vector3.one * .5f;
            EditorUtility.SetDirty(box);
            return box;
        }
    }
}
