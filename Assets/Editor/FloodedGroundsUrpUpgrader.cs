using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Upgrades the legacy Unity 5 materials shipped with Flooded Grounds so they
/// render in the Universal Render Pipeline used by this project.
/// </summary>
[InitializeOnLoad]
internal static class FloodedGroundsUrpUpgrader
{
    private const string Root = "Assets/Flooded_Grounds";
    private const string MenuPath = "Tools/Flooded Grounds/Upgrade All Materials to URP";

    private static bool s_UpgradeScheduled;

    static FloodedGroundsUrpUpgrader()
    {
        ScheduleUpgrade();
    }

    private sealed class MaterialSnapshot
    {
        public Texture baseMap;
        public string baseMapProperty;
        public Vector2 baseMapScale = Vector2.one;
        public Vector2 baseMapOffset = Vector2.zero;
        public Texture normalMap;
        public Texture metallicMap;
        public Texture occlusionMap;
        public Texture emissionMap;
        public Texture detailMap;
        public Texture detailNormalMap;
        public Color baseColor = Color.white;
        public Color emissionColor = Color.black;
        public float metallic;
        public float smoothness = 0.5f;
        public float normalScale = 1f;
        public float occlusionStrength = 1f;
        public float cutoff = 0.5f;
        public int renderQueue;
        public string oldShaderName;
    }

    private static void ScheduleUpgrade()
    {
        if (s_UpgradeScheduled)
            return;

        s_UpgradeScheduled = true;
        EditorApplication.delayCall += RunAutomaticUpgrade;
    }

    private static void RunAutomaticUpgrade()
    {
        s_UpgradeScheduled = false;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            ScheduleUpgrade();
            return;
        }

        UpgradeAllMaterials(false);
    }

    [MenuItem(MenuPath)]
    private static void UpgradeFromMenu()
    {
        UpgradeAllMaterials(true);
    }

    private static void UpgradeAllMaterials(bool showDialog)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Shader skyboxShader = Shader.Find("Skybox/Cubemap");

        if (litShader == null || unlitShader == null || particleShader == null || skyboxShader == null)
        {
            Debug.LogError("[Flooded Grounds] Required URP shaders were not found. Material upgrade was not run.");
            return;
        }

        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { Root });
        int upgraded = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in materialGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null || IsAlreadyCompatible(material))
                {
                    skipped++;
                    continue;
                }

                MaterialSnapshot snapshot = Capture(material);
                Shader targetShader = SelectTargetShader(path, material.name, particleShader, skyboxShader, unlitShader, litShader);

                material.shader = targetShader;
                RestoreCommonProperties(material, snapshot);
                ConfigureMaterial(material, path, snapshot);
                EditorUtility.SetDirty(material);
                upgraded++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        if (upgraded > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        int unsupported = CountUnsupportedMaterials(materialGuids);
        string message = $"Flooded Grounds URP upgrade finished: {upgraded} upgraded, {skipped} already compatible, {materialGuids.Length} total, {unsupported} unsupported.";
        Debug.Log("[Flooded Grounds] " + message);

        if (showDialog)
            EditorUtility.DisplayDialog("Flooded Grounds", message, "OK");
    }

    private static int CountUnsupportedMaterials(string[] materialGuids)
    {
        int unsupported = 0;

        foreach (string guid in materialGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null && (material.shader == null || !material.shader.isSupported))
            {
                unsupported++;
                Debug.LogError($"[Flooded Grounds] Unsupported material remains: {path}");
            }
        }

        return unsupported;
    }

    private static bool IsAlreadyCompatible(Material material)
    {
        if (material.shader == null)
            return false;

        string shaderName = material.shader.name;
        return shaderName.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)
               || shaderName == "Skybox/Cubemap";
    }

    private static Shader SelectTargetShader(
        string path,
        string materialName,
        Shader particleShader,
        Shader skyboxShader,
        Shader unlitShader,
        Shader litShader)
    {
        if (materialName.Equals("BGR_Sky1", StringComparison.OrdinalIgnoreCase))
            return skyboxShader;

        if (materialName.StartsWith("ATM_", StringComparison.OrdinalIgnoreCase))
            return particleShader;

        if (path.IndexOf("/PostProcessing/", StringComparison.OrdinalIgnoreCase) >= 0
            || materialName.StartsWith("HLP_", StringComparison.OrdinalIgnoreCase)
            || materialName.Equals("MSC_HumanScale", StringComparison.OrdinalIgnoreCase))
            return unlitShader;

        return litShader;
    }

    private static MaterialSnapshot Capture(Material material)
    {
        var snapshot = new MaterialSnapshot
        {
            oldShaderName = material.shader != null ? material.shader.name : "<missing>",
            renderQueue = material.renderQueue,
            baseColor = ReadColor(material, Color.white, "_BaseColor", "_Color", "_Tint", "_TintColor"),
            emissionColor = ReadColor(material, Color.black, "_EmissionColor", "_EmisColor"),
            metallic = ReadFloat(material, 0f, "_Metallic", "_layer1Metal"),
            smoothness = ReadFloat(material, 0.5f, "_Smoothness", "_Glossiness", "_Smth", "_Shininess"),
            normalScale = ReadFloat(material, 1f, "_BumpScale"),
            occlusionStrength = ReadFloat(material, 1f, "_OcclusionStrength"),
            cutoff = ReadFloat(material, 0.5f, "_Cutoff")
        };

        snapshot.baseMap = ReadTexture(material, out snapshot.baseMapProperty, "_BaseMap", "_MainTex", "_layer1Tex", "_Tex");
        snapshot.normalMap = ReadTexture(material, out _, "_BumpMap", "_BumpMap1", "_NormalMap", "_layer1Norm");
        snapshot.metallicMap = ReadTexture(material, out _, "_MetallicGlossMap", "_Spc", "_layer1Metal");
        snapshot.occlusionMap = ReadTexture(material, out _, "_OcclusionMap", "_AO");
        snapshot.emissionMap = ReadTexture(material, out _, "_EmissionMap");
        snapshot.detailMap = ReadTexture(material, out _, "_DetailAlbedoMap", "_layer1Tex");
        snapshot.detailNormalMap = ReadTexture(material, out _, "_DetailNormalMap", "_layer1Norm", "_DetailBump");

        if (!string.IsNullOrEmpty(snapshot.baseMapProperty) && material.HasProperty(snapshot.baseMapProperty))
        {
            snapshot.baseMapScale = material.GetTextureScale(snapshot.baseMapProperty);
            snapshot.baseMapOffset = material.GetTextureOffset(snapshot.baseMapProperty);
        }

        float emissionStrength = ReadFloat(material, 0f, "_Emis", "_EmissionScaleUI");
        if (snapshot.emissionColor.maxColorComponent <= 0f && emissionStrength > 0f)
            snapshot.emissionColor = snapshot.baseColor * emissionStrength;

        return snapshot;
    }

    private static void RestoreCommonProperties(Material material, MaterialSnapshot snapshot)
    {
        SetTexture(material, "_BaseMap", snapshot.baseMap);
        SetTexture(material, "_MainTex", snapshot.baseMap);
        SetTextureScaleAndOffset(material, "_BaseMap", snapshot.baseMapScale, snapshot.baseMapOffset);
        SetTextureScaleAndOffset(material, "_MainTex", snapshot.baseMapScale, snapshot.baseMapOffset);
        SetColor(material, "_BaseColor", snapshot.baseColor);
        SetColor(material, "_Color", snapshot.baseColor);

        SetTexture(material, "_BumpMap", snapshot.normalMap);
        SetFloat(material, "_BumpScale", snapshot.normalScale);
        SetTexture(material, "_MetallicGlossMap", snapshot.metallicMap);
        SetFloat(material, "_Metallic", snapshot.metallic);
        SetFloat(material, "_Smoothness", Mathf.Clamp01(snapshot.smoothness));
        SetTexture(material, "_OcclusionMap", snapshot.occlusionMap);
        SetFloat(material, "_OcclusionStrength", snapshot.occlusionStrength);
        SetTexture(material, "_EmissionMap", snapshot.emissionMap);
        SetColor(material, "_EmissionColor", snapshot.emissionColor);
        SetTexture(material, "_DetailAlbedoMap", snapshot.detailMap);
        SetTexture(material, "_DetailNormalMap", snapshot.detailNormalMap);
        SetFloat(material, "_Cutoff", snapshot.cutoff);

        if (snapshot.normalMap != null)
            material.EnableKeyword("_NORMALMAP");

        if (snapshot.metallicMap != null)
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

        if (snapshot.occlusionMap != null)
            material.EnableKeyword("_OCCLUSIONMAP");

        if (snapshot.detailMap != null || snapshot.detailNormalMap != null)
            material.EnableKeyword("_DETAIL_MULX2");

        if (snapshot.emissionMap != null || snapshot.emissionColor.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        // Skybox/Cubemap uses these legacy property names directly.
        SetTexture(material, "_Tex", snapshot.baseMap);
        SetColor(material, "_Tint", snapshot.baseColor);
    }

    private static void ConfigureMaterial(Material material, string path, MaterialSnapshot snapshot)
    {
        string name = material.name;

        if (name.Equals("BGR_Sky1", StringComparison.OrdinalIgnoreCase))
        {
            material.renderQueue = -1;
            return;
        }

        bool isParticle = name.StartsWith("ATM_", StringComparison.OrdinalIgnoreCase);
        bool isWater = name.Equals("BGR_Water", StringComparison.OrdinalIgnoreCase);
        bool isGlass = name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0;
        bool isGrid = name.Equals("HLP_Grid", StringComparison.OrdinalIgnoreCase);
        bool isCutout = name.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Bush", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Branch", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.IndexOf("Leaf", StringComparison.OrdinalIgnoreCase) >= 0
                        || path.IndexOf("/Trees/", StringComparison.OrdinalIgnoreCase) >= 0;

        if (isParticle || isWater || isGlass || isGrid)
        {
            Color color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : snapshot.baseColor;

            if ((isWater || isGlass) && color.a >= 0.99f)
                color.a = isWater ? 0.72f : 0.3f;

            SetColor(material, "_BaseColor", color);
            SetColor(material, "_Color", color);
            SetFloat(material, "_Surface", 1f);
            SetFloat(material, "_Blend", 0f);
            SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
            SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            SetFloat(material, "_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (isParticle || isGlass)
                SetFloat(material, "_Cull", (float)CullMode.Off);

            return;
        }

        SetFloat(material, "_Surface", 0f);
        SetFloat(material, "_SrcBlend", (float)BlendMode.One);
        SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
        SetFloat(material, "_ZWrite", 1f);
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");

        if (isCutout)
        {
            SetFloat(material, "_AlphaClip", 1f);
            SetFloat(material, "_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
        }
        else
        {
            SetFloat(material, "_AlphaClip", 0f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Geometry;
        }
    }

    private static Texture ReadTexture(Material material, out string propertyUsed, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (!material.HasProperty(propertyName))
                continue;

            Texture texture = material.GetTexture(propertyName);
            if (texture != null)
            {
                propertyUsed = propertyName;
                return texture;
            }
        }

        propertyUsed = null;
        return null;
    }

    private static Color ReadColor(Material material, Color fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
                return material.GetColor(propertyName);
        }

        return fallback;
    }

    private static float ReadFloat(Material material, float fallback, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
                return material.GetFloat(propertyName);
        }

        return fallback;
    }

    private static void SetTexture(Material material, string propertyName, Texture value)
    {
        if (value != null && material.HasProperty(propertyName))
            material.SetTexture(propertyName, value);
    }

    private static void SetTextureScaleAndOffset(Material material, string propertyName, Vector2 scale, Vector2 offset)
    {
        if (!material.HasProperty(propertyName))
            return;

        material.SetTextureScale(propertyName, scale);
        material.SetTextureOffset(propertyName, offset);
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
            material.SetColor(propertyName, value);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
            material.SetFloat(propertyName, value);
    }
}
