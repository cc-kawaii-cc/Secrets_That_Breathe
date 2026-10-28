using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecretsThatBreathe.Editor
{
    // One-shot, opt-in regression in an isolated preview scene. Never changes Play Mode
    // or the user's open scene, and does no work without its exact request file.
    public static class PoliceSirenRegressionCheck
    {
        private const string Request = "ArtSource/IsuzuSiren/RunUnityRegression.request";
        private const string Result = "ArtSource/IsuzuSiren/unity-regression.json";
        private const string Prefab = "Assets/Model Palm/Police Car1/isuzu.prefab";

        [InitializeOnLoadMethod]
        private static void Queue() => EditorApplication.delayCall += RunRequested;

        // Used only by a separate disposable validation project, never the user's editor.
        public static void RunBatch()
        {
            Directory.CreateDirectory("ArtSource/IsuzuSiren");
            File.WriteAllText(Request, "isolated batch validation");
            RunRequested();
            bool success = File.Exists(Result) && JsonUtility.FromJson<Report>(File.ReadAllText(Result)).success;
            EditorApplication.Exit(success ? 0 : 1);
        }

        [Serializable]
        private sealed class Report
        {
            public bool success;
            public bool ranWhilePlaying;
            public int assertions;
            public string unityVersion;
            public bool previewSceneOnly = true;
            public string[] checks;
            public string error;
        }

        private static void RunRequested()
        {
            if (!File.Exists(Request)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RunRequested; return; }
            File.Delete(Request);
            var report = new Report { ranWhilePlaying = EditorApplication.isPlaying, unityVersion = Application.unityVersion };
            var checks = new List<string>();
            var scene = EditorSceneManager.NewPreviewScene();
            var exceptions = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if ((type == LogType.Exception || type == LogType.Error) &&
                    (message.Contains("CreateImpl") || stack.Contains("PoliceSirenLights")))
                    exceptions.Add(message + "\n" + stack);
            };
            Application.logMessageReceived += capture;
            try
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
                Require(asset != null, "Prefab imports", report, checks);
                var clone = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                clone.transform.position = Vector3.zero;
                var siren = clone.GetComponent<PoliceSirenLights>();
                Require(siren != null, "Controller deserializes without native constructor calls", report, checks);
                var type = typeof(PoliceSirenLights);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Action<string> invoke = name => type.GetMethod(name, flags).Invoke(siren, null);
                Action<float> frame = phase => type.GetMethod("ApplyFrame", flags).Invoke(siren, new object[] { phase });
                invoke("OnEnable");
                var renderer = (Renderer)type.GetField("lensRenderer", flags).GetValue(siren);
                var left = (Light)type.GetField("leftLight", flags).GetValue(siren);
                var right = (Light)type.GetField("rightLight", flags).GetValue(siren);
                Require(renderer != null && left != null && right != null, "Renderer and both actual Light references resolve", report, checks);
                var block = new MaterialPropertyBlock();
                int emission = Shader.PropertyToID("_EmissionColor");
                Func<int, float> red = index =>
                { renderer.GetPropertyBlock(block, index); return block.GetColor(emission).r; };
                frame(0f);
                Require(red(0) > red(1) * 10, "Left material bank peaks at phase zero", report, checks);
                Require(left.enabled && right.enabled && left.intensity > 0 && right.intensity > 0,
                    "Real rotating Spot Lights are enabled and illuminated", report, checks);
                var rotation = left.transform.localRotation;
                frame(.25f);
                Require(Quaternion.Angle(rotation, left.transform.localRotation) > 80f,
                    "Real Light transform rotates a quarter turn", report, checks);
                frame(.5f);
                Require(red(1) > red(0) * 10, "Right material bank peaks on the opposite phase", report, checks);
                type.GetField("pattern", flags).SetValue(siren, PoliceSirenLights.FlashPattern.AlternatingDoubleFlash);
                frame(0f); Require(left.enabled && !right.enabled, "Double flash starts on left real light", report, checks);
                frame(.125f); Require(!left.enabled && !right.enabled, "Double flash has a real dark interval", report, checks);
                frame(.20f); Require(left.enabled && !right.enabled, "Second left real-light flash", report, checks);
                frame(.5f); Require(!left.enabled && right.enabled, "Right real-light flash alternates", report, checks);
                siren.SirenOn = false; frame(.6f);
                Require(!left.enabled && !right.enabled && red(0) == 0 && red(1) == 0,
                    "Siren off clears native emission blocks and disables both lights", report, checks);
                // Simulate native-only caches disappearing on a Unity script/domain reload.
                type.GetField("leftBlock", flags).SetValue(siren, null);
                type.GetField("rightBlock", flags).SetValue(siren, null);
                invoke("OnDisable");
                checks.Add("OnDisable tolerates missing native caches after reload"); report.assertions++;
                siren.SirenOn = true; invoke("OnEnable"); frame(0f);
                Require(red(0) > 1f && left.enabled, "OnEnable recreates native blocks and resumes flashing after reload", report, checks);
                invoke("OnDisable");
                Require(!left.enabled && !right.enabled && red(0) == 0 && red(1) == 0,
                    "Normal component disable clears real lights and emission", report, checks);
                Require(exceptions.Count == 0, "No native creation or property-block errors in Unity", report, checks);
                report.success = true;
            }
            catch (Exception exception)
            { report.error = exception.ToString(); Debug.LogError("Police siren regression failed: " + exception); }
            finally
            {
                Application.logMessageReceived -= capture;
                EditorSceneManager.ClosePreviewScene(scene);
                report.checks = checks.ToArray();
                File.WriteAllText(Result, JsonUtility.ToJson(report, true));
                Debug.Log("Police siren native Unity regression: " + (report.success ? "PASS" : "FAIL"));
            }
        }

        private static void Require(bool condition, string check, Report report, List<string> checks)
        { if (!condition) throw new InvalidOperationException(check); checks.Add(check); report.assertions++; }
    }
}
