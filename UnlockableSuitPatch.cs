using GameNetcodeStuff;
using HarmonyLib;
using NilsHUD;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

[HarmonyPatch(typeof(UnlockableSuit), "SwitchSuitForPlayer")]
public static class UnlockableSuitPatch
{
    private static readonly Dictionary<int, Color> suitColors = new Dictionary<int, Color>
    {
        { 0, new Color(1f, 0.5f, 0f) }, // Default/Unknown Suit (Orange)
        { 1, Color.green },             // Green Suit
        { 2, Color.yellow },            // Hazard Suit
        { 3, Color.cyan },              // Pajama Suit
        { 24, new Color(0.5f, 0f, 0.5f) } // Purple Suit
    };

    private const string FallbackColorHex = "#FF0000"; // Red color (RGB: 255, 0, 0)

    [HarmonyPostfix]
    public static void SwitchSuitForPlayer_Postfix(PlayerControllerB player, int suitID)
    {
        if (PluginConfig.ConfigEnableSuitColorOverlay.Value)
        {
            UpdateHealthOverlayColor(player, suitID);
        }
    }

    public static void UpdateHealthOverlayColor(PlayerControllerB player, int suitID)
    {
        Debug.Log($"UpdateHealthOverlayColor called with player: {player.name}, suitID: {suitID}");

        Image healthImage = HUDManager.Instance?.selfRedCanvasGroup?.GetComponent<Image>();
        if (healthImage != null)
        {
            if (suitColors.TryGetValue(suitID, out Color suitColor))
            {
                healthImage.color = suitColor;
            }
            else
            {
                ColorUtility.TryParseHtmlString(PluginConfig.ConfigOverlayColorHex.Value, out Color customColor);
                healthImage.color = customColor != default ? customColor : ColorUtility.TryParseHtmlString(FallbackColorHex, out Color fallbackColor) ? fallbackColor : Color.red;
            }
        }
    }
}