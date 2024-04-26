using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using GameNetcodeStuff;
using System.Collections.Generic;
using NilsHUD;

[HarmonyPatch(typeof(UnlockableSuit), "SwitchSuitForPlayer")]
public static class UnlockableSuitPatch
{
    private static HUDManager? hudManager;
    private static StartOfRound? startOfRound;

    [HarmonyPostfix]
    public static void SwitchSuitForPlayer_Postfix(PlayerControllerB player, int suitID)
    {
        if (player != null)
        {
            UpdateHealthOverlayColor(player, suitID);
        }
    }

    public static void UpdateHealthOverlayColor(PlayerControllerB player, int suitID)
    {
        if (hudManager == null)
            hudManager = HUDManager.Instance;

        if (hudManager != null)
        {
            if (hudManager.selfRedCanvasGroup?.TryGetComponent(out Image healthImage) == true)
            {
                if (startOfRound == null)
                    startOfRound = StartOfRound.Instance;

                Material suitMaterial = startOfRound.unlockablesList.unlockables[suitID].suitMaterial;
                Color averageColor = SuitColorCache.GetSuitColor(suitID, suitMaterial);
                healthImage.color = averageColor;
            }
        }
    }

    public static class SuitColorCache
    {
        private static Dictionary<int, Color> colorCache = new Dictionary<int, Color>();
        private static Texture2D? readableTexture;

        public static Color GetSuitColor(int suitID, Material suitMaterial)
        {
            if (!colorCache.TryGetValue(suitID, out Color color))
            {
                color = GetAverageColorFromMaterial(suitMaterial);
                colorCache[suitID] = color;
            }
            return color;
        }

        public static Color GetAverageColorFromMaterial(Material material)
        {
            if (material != null && material.mainTexture is Texture2D texture)
            {
                RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height);
                Graphics.Blit(texture, renderTexture);

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;

                if (readableTexture == null || readableTexture.width != texture.width || readableTexture.height != texture.height)
                {
                    if (readableTexture != null)
                        Object.Destroy(readableTexture);

                    readableTexture = new Texture2D(texture.width, texture.height);
                }

                readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                readableTexture.Apply();

                Color avgColor = GetAverageColorFromTexture(readableTexture);

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTexture);

                return avgColor;
            }
            return Color.gray;
        }

        private static Color GetAverageColorFromTexture(Texture2D texture)
        {
            Color32[] pixels = texture.GetPixels32();
            int sampleCount = Mathf.Min(pixels.Length, 1000);
            int step = pixels.Length / sampleCount;

            float totalWeight = 0f;
            Color weightedSum = Color.clear;

            for (int i = 0; i < pixels.Length; i += step)
            {
                Color32 pixel = pixels[i];
                float brightness = (pixel.r + pixel.g + pixel.b) / (3f * 255f);
                float weight = Mathf.Clamp01(brightness * 2f);

                weightedSum += (Color)pixel * weight;
                totalWeight += weight;
            }

            if (totalWeight > 0f)
            {
                Color averageColor = weightedSum / totalWeight;

                // Apply brightness adjustment
                float brightness = PluginConfig.ConfigSuitOverlayBrightness.Value;
                averageColor = Color.Lerp(Color.black, averageColor, brightness);

                return averageColor;
            }
            else
            {
                return Color.gray;
            }
        }
    }

    public static void PrecomputeSuitColors()
    {
        if (startOfRound == null)
            startOfRound = StartOfRound.Instance;

        foreach (UnlockableItem unlockable in startOfRound.unlockablesList.unlockables)
        {
            if (unlockable.GetType().Name == "UnlockableSuit")
            {
                int suitID = (int)unlockable.GetType().GetProperty("suitID").GetValue(unlockable);
                Material suitMaterial = (Material)unlockable.GetType().GetProperty("suitMaterial").GetValue(unlockable);
                Color averageColor = SuitColorCache.GetSuitColor(suitID, suitMaterial);
                Debug.Log($"Precomputed color for suit ID {suitID}: {averageColor}");
            }
        }
    }
}