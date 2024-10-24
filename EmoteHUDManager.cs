using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Reflection;

public class EmoteHUDManager
{
    private static Type emoteWheelType;
    private static MethodInfo selectEmoteMethod;
    private static MethodInfo deselectEmoteMethod;
    private static MethodInfo updateEmoteWheelMethod;

    static EmoteHUDManager()
    {
        try
        {
            emoteWheelType = Type.GetType("LethalEmotesAPI.EmoteWheel, LethalEmotesAPI");
            if (emoteWheelType != null)
            {
                selectEmoteMethod = emoteWheelType.GetMethod("SelectEmote", BindingFlags.Instance | BindingFlags.Public);
                deselectEmoteMethod = emoteWheelType.GetMethod("DeselectEmote", BindingFlags.Instance | BindingFlags.Public);
                updateEmoteWheelMethod = emoteWheelType.GetMethod("UpdateEmoteWheel", BindingFlags.Instance | BindingFlags.Public);

                if (selectEmoteMethod != null && deselectEmoteMethod != null && updateEmoteWheelMethod != null)
                {
                    var harmony = new Harmony("com.nilshud.emotehudmanager");
                    harmony.Patch(selectEmoteMethod, postfix: new HarmonyMethod(typeof(EmoteHUDManager).GetMethod("OnEmoteSelected", BindingFlags.Static | BindingFlags.NonPublic)));
                    harmony.Patch(deselectEmoteMethod, postfix: new HarmonyMethod(typeof(EmoteHUDManager).GetMethod("OnEmoteDeselected", BindingFlags.Static | BindingFlags.NonPublic)));
                    harmony.Patch(updateEmoteWheelMethod, postfix: new HarmonyMethod(typeof(EmoteHUDManager).GetMethod("OnEmoteWheelUpdated", BindingFlags.Static | BindingFlags.NonPublic)));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EmoteHUDManager] Error initializing EmoteHUDManager: {ex.Message}");
        }
    }

    private static void OnEmoteSelected()
    {
        UpdateHUD();
    }

    private static void OnEmoteDeselected()
    {
        UpdateHUD();
    }

    private static void OnEmoteWheelUpdated()
    {
        UpdateHUD();
    }

    private static void UpdateHUD()
    {
        try
        {
            var player = PlayerUtils.GetPlayerControllerB();
            if (player != null)
            {
                var healthBarImage = HealthBar.GetHealthBar(player)?.healthBarImage;
                var redHealthBarImage = HealthBar.GetHealthBar(player)?.redHealthBarImage;

                if (healthBarImage != null && redHealthBarImage != null)
                {
                    healthBarImage.fillAmount = player.health / 100f;
                    redHealthBarImage.fillAmount = player.health / 100f;
                }

                if (player.isPlayerDead)
                {
                    // Handle player death
                    healthBarImage.fillAmount = 0f;
                    redHealthBarImage.fillAmount = 0f;
                }

                // Update health overlay color based on suit ID
                UnlockableSuitPatch.UpdateHealthOverlayColor(player, player.currentSuitID);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EmoteHUDManager] Error updating HUD: {ex.Message}");
        }
    }
}
