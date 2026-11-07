using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildEveningPaddy
{
    const string PathToTerrain = "Assets/PaddyStage/EveningTerrain.asset";
    static readonly float[] NearDividers = { .19f, .45f, .74f };
    static readonly float[] FarDividers = { .26f, .53f, .79f };

    public static void Run()
    {
        try
        {
            var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(PathToTerrain);
            if (terrain == null || terrain.heightmapResolution != 513 ||
                (terrain.size - new Vector3(400, 600, 500)).sqrMagnitude > .01f)
                throw new InvalidOperationException("Unexpected terrain asset size.");
            int size = terrain.heightmapResolution;
            float[,] heights = terrain.GetHeights(0, 0, size, size);
            int samples = 0;
            float minimumDelta = 0, maximumDelta = 0;
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                if (!Paddy(x * terrain.size.x / (size - 1), z * terrain.size.z / (size - 1),
                           out float delta, out _)) continue;
                heights[z, x] = Mathf.Clamp01(heights[z, x] + delta / terrain.size.y);
                minimumDelta = Mathf.Min(minimumDelta, delta);
                maximumDelta = Mathf.Max(maximumDelta, delta);
                samples++;
            }
            if (samples < 1000 || minimumDelta > -.44f || maximumDelta < .15f)
                throw new InvalidOperationException("Paddy height profile is incomplete.");
            terrain.SetHeights(0, 0, heights);
            int dirt = Array.FindIndex(terrain.terrainLayers, layer => layer != null && layer.name == "NewLayer");
            if (dirt < 0) throw new InvalidOperationException("Existing soil Terrain Layer is unavailable.");
            int width = terrain.alphamapWidth, depth = terrain.alphamapHeight;
            float[,,] paint = terrain.GetAlphamaps(0, 0, width, depth);
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                if (!Paddy(x * terrain.size.x / (width - 1), z * terrain.size.z / (depth - 1),
                           out _, out float soil)) continue;
                for (int layer = 0; layer < paint.GetLength(2); layer++)
                    paint[z, x, layer] = Mathf.Lerp(paint[z, x, layer], layer == dirt ? 1f : 0f, soil);
            }
            terrain.SetAlphamaps(0, 0, paint);
            EditorUtility.SetDirty(terrain);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Assets/PaddyStage/paddy-result.txt",
                $"PASS terrain samples: {samples}, depth: {minimumDelta:F3}, bank: {maximumDelta:F3}, soil layer: {dirt}");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    static bool Paddy(float x, float z, out float delta, out float soil)
    {
        delta = soil = 0;
        const float near = 338.5f, far = 405.5f;
        const float downSlope = .90f;
        if (z <= near || z >= far) return false;
        float along = (z - near) / (far - near);
        float left = Mathf.Lerp(124f, 91f, along);
        float right = Mathf.Lerp(274f, 316f, along);
        float edge = Mathf.Min(Mathf.Min(x - left, right - x), Mathf.Min(z - near, far - z));
        if (edge <= 0f) return false;

        float broadGround = Mathf.PerlinNoise(x * .085f + 21f, z * .090f);
        float smallGround = Mathf.PerlinNoise(x * .24f, z * .23f + 31f);
        float bed = -.45f + (broadGround - .5f) * .11f + (smallGround - .5f) * .035f;
        float crest = .19f + (Mathf.PerlinNoise(x * .14f + 51f, z * .14f) - .5f) * .055f;
        float bankCrest = 1.35f + (Mathf.PerlinNoise(x * .06f, z * .06f + 39f) - .5f) * .40f;

        // The outer bank rises from the verge, then drops quickly into the field.
        float outsideBank = edge <= bankCrest
            ? crest * Ease(edge / bankCrest)
            : Mathf.Lerp(crest, bed, Ease((edge - bankCrest) / downSlope));
        float crossBank = 370.5f + (x - 195f) * .045f
            + Mathf.Sin((x - 110f) * .059f) * 2.1f + Mathf.Sin(x * .128f) * .65f;
        float bankDistance = Mathf.Abs(z - crossBank);
        for (int divider = 0; divider < NearDividers.Length; divider++)
        {
            float fraction = Mathf.Lerp(NearDividers[divider], FarDividers[divider], along);
            float bend = Mathf.Sin(along * 6.28f + divider * 1.4f) * 2.4f
                + Mathf.Sin(along * 12.56f + divider) * .75f;
            float boundary = Mathf.Lerp(left, right, fraction) + bend;
            bankDistance = Mathf.Min(bankDistance, Mathf.Abs(x - boundary));
        }
        float bankWidth = Mathf.Lerp(1.9f, 2.8f, Mathf.PerlinNoise(x * .081f, z * .075f + 17f));
        float bank = 1f - Ease(bankDistance / bankWidth);
        delta = edge <= bankCrest ? outsideBank : Mathf.Max(outsideBank, Mathf.Lerp(bed, crest, bank));
        float rim = 1f - Ease((edge - bankCrest) / downSlope);
        float bedSoil = Mathf.Lerp(.81f, 1f, broadGround);
        float bankSoil = Mathf.Lerp(.48f, .72f, smallGround);
        soil = Ease(edge / bankCrest) * Mathf.Lerp(bedSoil, bankSoil, Mathf.Max(rim, bank));
        return true;
    }

    static float Ease(float x) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(x));
}
