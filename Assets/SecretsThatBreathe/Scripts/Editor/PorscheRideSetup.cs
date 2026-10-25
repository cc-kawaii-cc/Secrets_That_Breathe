using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SecretsThatBreathe.Editor
{
    [InitializeOnLoad]
    public static class PorscheRideSetup
    {
        private const string ScenePath = "Assets/Nemo_Upgrade/MainScene_2_Evening.unity";
        private static string WorkRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "../Tools/PorscheRide"));
        private static double nextPoll;
        [Serializable] private class Request { public string id; public string operation; }
        [Serializable] private class Report
        {
            public bool success;
            public string message;
            public string scene;
            public bool wasDirty;
            public Vector3 carPosition, sceneViewLocalPosition, seatLocalPosition;
            public Vector3 sceneViewLocalAngles;
            public string[] suspended;
            public string[] checks;
        }

        static PorscheRideSetup() { EditorApplication.update += Poll; }

        private static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            string requestPath = Path.Combine(WorkRoot, "request.json");
            if (!File.Exists(requestPath)) return;
            Report result;
            try
            {
                var request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
                if (request == null || string.IsNullOrWhiteSpace(request.id) || request.id.Any(c => !char.IsLetterOrDigit(c) && c != '-'))
                    throw new InvalidOperationException("A unique request ID is required.");
                string consumed = Path.Combine(WorkRoot, request.id + ".consumed.json");
                File.Move(requestPath, consumed);
                result = Run(request.operation);
            }
            catch (Exception error) { result = new Report { success = false, message = error.ToString() }; }
            File.WriteAllText(Path.Combine(WorkRoot, "result.json"), JsonUtility.ToJson(result, true));
        }

        [MenuItem("Tools/Secrets That Breathe/Porsche Ride/Set Up Evening Scene")]
        public static void SetupMenu() { Debug.Log(JsonUtility.ToJson(Run("setup"), true)); }

        [MenuItem("Tools/Secrets That Breathe/Porsche Ride/Align Driver Seat To Scene View")]
        public static void AlignSeat()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            var ride = scene.IsValid() ? scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<PorschePassengerRide>(true)).FirstOrDefault() : null;
            var view = SceneView.lastActiveSceneView;
            if (ride == null || ride.driverSeat == null || view == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Undo.RecordObject(ride.driverSeat, "Align driver seat to Scene view");
            ride.driverSeat.SetPositionAndRotation(view.camera.transform.position, view.camera.transform.rotation);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static Report Run(string operation)
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open MainScene_2_Evening first.");
            var car = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "PorscheV.2");
            var player = scene.GetRootGameObjects().FirstOrDefault(x => x.name == "Player");
            if (car == null || player == null) throw new InvalidOperationException("PorscheV.2 and Player must exist in this scene.");
            var camera = player.GetComponentsInChildren<Camera>(true).FirstOrDefault(x => x.CompareTag("MainCamera"));
            if (camera == null) throw new InvalidOperationException("Player MainCamera not found.");
            var report = new Report { success = true, scene = scene.path, wasDirty = scene.isDirty, carPosition = car.transform.position };
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                report.sceneViewLocalPosition = car.transform.InverseTransformPoint(sceneView.camera.transform.position);
                report.sceneViewLocalAngles = (Quaternion.Inverse(car.transform.rotation) * sceneView.camera.transform.rotation).eulerAngles;
            }
            if (operation == "inspect") { report.message = "Read-only inspection completed."; return report; }
            if (operation == "preview")
            {
                var configuredRide = car.GetComponent<PorschePassengerRide>();
                if (configuredRide == null) throw new InvalidOperationException("Set up the ride first.");
                RenderDriverView(configuredRide);
                report.checks = Verify(configuredRide);
                report.message = "Offscreen driver view rendered; no screen or input control used.";
                return report;
            }
            if (operation != "setup") throw new InvalidOperationException("Unknown Porsche ride operation.");

            Directory.CreateDirectory(WorkRoot);
            string backup = Path.Combine(WorkRoot, "BeforeRide.unity");
            if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene, backup, true))
                throw new IOException("Could not back up the current scene, including unsaved edits.");
            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Set up Porsche passenger ride");
            var ride = car.GetComponent<PorschePassengerRide>();
            bool isNew = ride == null;
            if (isNew) ride = Undo.AddComponent<PorschePassengerRide>(car);
            Undo.RecordObject(ride, "Configure Porsche route");
            ride.startPosition = new Vector3(209.3862f, 5.09909f, 142.75f);
            ride.endPosition = new Vector3(209.3862f, 5.09909f, 284.65f);
            if (isNew) { ride.speed = 1f; ride.yawLimit = 55f; ride.pitchLimit = 55f; }
            ride.playerRoot = player.transform;
            ride.playerCamera = camera;
            var seat = car.transform.Find("DriverSeat");
            if (seat == null)
            {
                var seatObject = new GameObject("DriverSeat");
                Undo.RegisterCreatedObjectUndo(seatObject, "Create driver eye point");
                seat = seatObject.transform;
                seat.SetParent(car.transform, false);
                Vector3 eye = report.sceneViewLocalPosition;
                bool isInside = eye.x > -0.8f && eye.x < 0f && eye.y > 0.85f && eye.y < 1.45f && eye.z > -0.8f && eye.z < 0.7f;
                seat.localPosition = isInside ? eye : new Vector3(-0.36f, 1.12f, 0.1f);
                seat.localRotation = Quaternion.identity;
            }
            ride.driverSeat = seat;
            var controls = new List<Behaviour>();
            string[] types = { "FirstPersonController", "PlayerSeatController", "SeatInteractor", "SeatCameraAnimation", "PlayerFootsteps", "CinemachineBrain", "PlayerMovement", "MouseLook", "PlayerInteractor" };
            foreach (var control in player.GetComponentsInChildren<Behaviour>(true))
                if (types.Contains(control.GetType().Name)) controls.Add(control);
            ride.suspendedControls = controls.ToArray();
            report.suspended = controls.Select(x => x.GetType().Name).ToArray();

            // Moving geometry must not participate in static batching or baked lighting.
            foreach (Transform item in car.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetStaticEditorFlags(item.gameObject) == 0) continue;
                Undo.RecordObject(item.gameObject, "Make moving car dynamic");
                GameObjectUtility.SetStaticEditorFlags(item.gameObject, 0);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
            }
            Undo.RecordObject(car.transform, "Place car at route start");
            car.transform.position = ride.startPosition;
            PrefabUtility.RecordPrefabInstancePropertyModifications(car.transform);
            EditorUtility.SetDirty(ride);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ride);
            EditorSceneManager.MarkSceneDirty(scene);
            Undo.CollapseUndoOperations(undo);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save configured scene.");
            report.seatLocalPosition = seat.localPosition;
            report.checks = Verify(ride);
            report.message = "Porsche ride configured and scene saved. Existing lights, phone flashlight and crosshair preserved.";
            return report;
        }

        private static void RenderDriverView(PorschePassengerRide ride)
        {
            GameObject temporary = new GameObject("Driver View Verification") { hideFlags = HideFlags.HideAndDontSave };
            RenderTexture target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            Texture2D image = null;
            var previous = RenderTexture.active;
            try
            {
                var camera = temporary.AddComponent<Camera>();
                camera.CopyFrom(ride.playerCamera);
                camera.enabled = false;
                var data = temporary.AddComponent<UniversalAdditionalCameraData>();
                var sourceData = ride.playerCamera.GetComponent<UniversalAdditionalCameraData>();
                if (sourceData != null) EditorUtility.CopySerialized(sourceData, data);
                data.renderType = CameraRenderType.Base;
                data.cameraStack.Clear();
                camera.transform.SetPositionAndRotation(ride.driverSeat.position, ride.driverSeat.rotation);
                camera.fieldOfView = ride.fieldOfView;
                camera.nearClipPlane = ride.nearClipPlane;
                camera.aspect = 16f / 9f;
                camera.targetTexture = target;
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(WorkRoot, "DriverView.png"), image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static string[] Verify(PorschePassengerRide ride)
        {
            var checks = new List<string>();
            Vector3 start = ride.startPosition, end = ride.endPosition;
            if (PorschePassengerRide.Advance(start, end, 1f, 1f) != start + Vector3.forward) throw new Exception("Speed check failed.");
            if (PorschePassengerRide.Advance(start, end, 0f, 1f) != start) throw new Exception("Zero speed check failed.");
            if (PorschePassengerRide.Advance(start, end, 1000f, 1f) != end) throw new Exception("Arrival overshoot check failed.");
            checks.Add("Movement: 1 metre per second, pause at zero speed, exact arrival without overshoot.");
            if (PorschePassengerRide.ClampLook(100f, 55f) != 55f || PorschePassengerRide.ClampLook(-100f, 55f) != -55f)
                throw new Exception("55-degree look clamp failed.");
            checks.Add("Look: clamped to +/-55 degrees.");
            if (ride.driverSeat == null || ride.playerRoot == null || ride.playerCamera == null) throw new Exception("Missing ride references.");
            if (ride.suspendedControls.Any(x => x != null && x.GetType().Name.Contains("Flashlight"))) throw new Exception("Flashlight accidentally suspended.");
            checks.Add("Camera/seat/player references assigned. Phone flashlight remains enabled.");
            return checks.ToArray();
        }
    }
}
