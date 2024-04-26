using GameNetcodeStuff;
using HarmonyLib;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NilsHUD
{
    public static class PlayerUtils
    {
        private static PlayerControllerB? localPlayerController;

        public static PlayerControllerB? GetPlayerControllerB()
        {
            if (localPlayerController == null)
            {
                if (GameNetworkManager.Instance != null)
                {
                    localPlayerController = GameNetworkManager.Instance.localPlayerController;
                    Debug.Log($"[{PluginInfo.PLUGIN_NAME}] GetPlayerControllerB returned: {localPlayerController?.name ?? "null"}");
                }
                else
                {
                    Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] GameNetworkManager.Instance is null.");
                }
            }
            return localPlayerController;
        }
    }

    public static class HUDManagerPatch
    {
        private const string HealFromCriticalTrigger = "HealFromCritical";
        private const string CriticalHitTrigger = "CriticalHit";
        private const string SmallHitTrigger = "SmallHit";
        private const int CriticalHealthThreshold = 20;

        private static bool playerIsCriticallyInjured;
        private static float targetFillAmount;

        [HarmonyPatch(typeof(HUDManager), "Awake")]
        [HarmonyPostfix]
        public static void AwakePostfix()
        {
            InitializeHealthOverlay();
        }

        [HarmonyPatch(typeof(HUDManager), "UpdateHealthUI")]
        [HarmonyPrefix]
        public static bool UpdateHealthUIPrefix(int health, bool hurtPlayer = true)
        {
            HUDManager hudManager = HUDManager.Instance;

            if (hudManager == null)
            {
                Debug.LogError("HUDManager instance is null!");
                return true;
            }

            if (hudManager.selfRedCanvasGroup == null)
            {
                Debug.LogError("selfRedCanvasGroup is null!");
                return true;
            }

            var image = hudManager.selfRedCanvasGroup.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("Image component not found on selfRedCanvasGroup!");
                return false;
            }

            PlayerControllerB playerController = PlayerUtils.GetPlayerControllerB();

            // Check if the player is dead
            if (playerController != null && playerController.isPlayerDead)
            {
                // Player is dead, skip updating the fill amount
                return false;
            }

            if (PluginConfig.ConfigStartFullyFilled.Value)
            {
                targetFillAmount = (100f - health) / 100f;
            }
            else
            {
                targetFillAmount = health / 100f;
            }

            if (!hudManager.gameObject.GetComponent<HealthIndicatorInitializer>())
            {
                hudManager.gameObject.AddComponent<HealthIndicatorInitializer>();
                if (PluginConfig.ConfigStartFullyFilled.Value)
                {
                    image.fillAmount = 1f; // Set the initial fill amount to 0 (bottom)
                }
                else
                {
                    image.fillAmount = 0f;
                }
            }

            float previousFillAmount;
            float newFillAmount;

            if (PluginConfig.ConfigStartFullyFilled.Value)
            {
                previousFillAmount = image.fillAmount == 0f ? 1f : image.fillAmount;
                newFillAmount = health / 100f;
            }
            else
            {
                previousFillAmount = image.fillAmount;
                newFillAmount = (100f - health) / 100f;
            }

            // Check if the player's health has actually changed
            if (Mathf.Approximately(previousFillAmount, newFillAmount) && !PlayerUtils.GetPlayerControllerB().isPlayerDead)
            {
                // Health hasn't changed and player is not dead, no need to update the UI or trigger animations
                return false;
            }

            // Set the image's fill amount to the previous fill amount before starting the smooth fill transition
            image.fillAmount = previousFillAmount;

            ConfigureHealthImage(image, health, playerController);

            StartHealthMonitoring(hudManager); // Start monitoring health changes
            StartSmoothFillTransition(hudManager, image, previousFillAmount);

            if (hudManager.HUDAnimator == null)
            {
                Debug.LogError("HUDAnimator is null!");
                return false;
            }

            HandlePlayerHealthState(hudManager, health, hurtPlayer);

            return false;
        }

        public static void InitializeHealthOverlay()
        {
            HUDManager hudManager = HUDManager.Instance;

            if (hudManager == null)
            {
                Debug.LogError("HUDManager instance is null!");
                return;
            }

            if (hudManager.selfRedCanvasGroup == null)
            {
                Debug.LogError("selfRedCanvasGroup is null!");
                return;
            }

            var image = hudManager.selfRedCanvasGroup.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("Image component not found on selfRedCanvasGroup!");
                return;
            }

            hudManager.selfRedCanvasGroup.alpha = 1f;

            PlayerControllerB playerController = PlayerUtils.GetPlayerControllerB();
            int health = playerController != null ? playerController.health : 100;

            if (PluginConfig.ConfigStartFullyFilled.Value)
            {
                image.fillAmount = health == 100 ? 1f : 0f;
                hudManager.selfRedCanvasGroup.alpha = health / 100f;
            }
            else
            {
                image.fillAmount = (float)health / 100f;
                hudManager.selfRedCanvasGroup.alpha = (100f - health) / 100f;
            }

            ConfigureHealthImage(image, health, playerController);
        }

        private static void StartHealthMonitoring(HUDManager hudManager)
        {
            if (!hudManager.gameObject.GetComponent<HealthMonitorCoroutine>())
            {
                hudManager.gameObject.AddComponent<HealthMonitorCoroutine>();
            }
        }

        private static void StartSmoothFillTransition(HUDManager hudManager, Image image, float previousFillAmount)
        {
            if (hudManager == null)
            {
                Debug.LogError("HUDManager instance is null!");
                return;
            }

            hudManager.StopAllCoroutines();

            if (PluginConfig.ConfigEnableFillAnimation.Value == true)
            {
                float fillDuration = Mathf.Clamp(PluginConfig.ConfigFillTransitionDuration.Value, 0.01f, 1f);
                bool drainEffect = !PluginConfig.ConfigStartFullyFilled.Value;
                hudManager.StartCoroutine(SmoothFillTransition(image, previousFillAmount, targetFillAmount, fillDuration, drainEffect));
            }
            else
            {
                image.fillAmount = targetFillAmount;
            }

            if (PluginConfig.ConfigEnableFadeOut.Value == true)
            {
                float fadeOutDuration = Mathf.Clamp(PluginConfig.ConfigFadeOutDuration.Value, 0f, 5f);
                float fillDuration = Mathf.Clamp(PluginConfig.ConfigFillTransitionDuration.Value, 0.01f, 1f);
                hudManager.StartCoroutine(SmoothFillAndFadeTransition(image, previousFillAmount, targetFillAmount, fillDuration, fadeOutDuration));
            }
        }

        private static IEnumerator StartFadeOutEffect(Image image, float fadeOutDuration)
        {
            float elapsedTime = 0f;
            Color startColor = image.color;
            float targetAlpha = Mathf.Clamp01(1f - PluginConfig.ConfigFadeOutIntensity.Value);
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

            while (elapsedTime < fadeOutDuration)
            {
                float t = elapsedTime / fadeOutDuration;
                image.color = Color.Lerp(startColor, targetColor, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.color = targetColor;
        }

        private static IEnumerator SmoothFillTransition(Image image, float startFillAmount, float targetFillAmount, float duration, bool drainEffect = false)
        {
            float elapsedTime = 0f;

            Debug.LogFormat("Start fill amount: {0}", startFillAmount);
            Debug.LogFormat("Target fill amount: {0}", targetFillAmount);
            Debug.LogFormat("Fill duration: {0}", duration);

            // If the start and target fill amounts are the same, set a minimum duration
            if (Mathf.Approximately(startFillAmount, targetFillAmount))
            {
                duration = Mathf.Max(duration, 0.1f);
            }

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (PluginConfig.ConfigStartFullyFilled.Value)
                {
                    if (drainEffect)
                    {
                        image.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, smoothT);
                    }
                    else
                    {
                        image.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, smoothT);
                    }
                }
                else
                {
                    if (drainEffect)
                    {
                        image.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, smoothT);
                    }
                    else
                    {
                        image.fillAmount = Mathf.Lerp(startFillAmount, targetFillAmount, smoothT);
                    }
                }

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.fillAmount = targetFillAmount;
        }

        private static IEnumerator SmoothFillAndFadeTransition(Image image, float startFillAmount, float targetFillAmount, float smoothFillDuration, float fadeOutDuration)
        {
            Debug.LogFormat("Start fill amount: {0}", image.fillAmount);
            Debug.LogFormat("Target fill amount: {0}", targetFillAmount);
            Debug.LogFormat("Fill duration: {0}", smoothFillDuration);
            Debug.LogFormat("Fade-out duration: {0}", fadeOutDuration);

            // If the start and target fill amounts are the same, set a minimum fill duration
            if (Mathf.Approximately(image.fillAmount, targetFillAmount))
            {
                smoothFillDuration = Mathf.Max(smoothFillDuration, 0.1f);
            }

            // Smooth fill transition
            yield return SmoothFillTransition(image, startFillAmount, targetFillAmount, smoothFillDuration);

            // Fade-out effect
            float elapsedTime = 0f;
            Color startColor = image.color;
            float targetAlpha = Mathf.Clamp01(1f - PluginConfig.ConfigFadeOutIntensity.Value);
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);

            Debug.Log($"Starting fade-out effect. Start color: {startColor}, Target color: {targetColor}, Fade-out duration: {fadeOutDuration}");

            while (elapsedTime < fadeOutDuration)
            {
                float t = elapsedTime / fadeOutDuration;
                image.color = Color.Lerp(startColor, targetColor, t);

                Debug.Log($"Fade-out effect progress: {t}, Current color: {image.color}");

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            image.color = targetColor;
            Debug.Log($"Fade-out effect completed. Final color: {image.color}");
        }

        private static void ConfigureHealthImage(Image image, int health, PlayerControllerB controllerInstance)
        {
            if (PluginConfig.ConfigStartFullyFilled.Value)
            {
                targetFillAmount = health / 100f;
                if (health == 100)
                {
                    image.fillAmount = 1f; // Set the fill amount to 1 (fully filled) when health is 100
                }
                else
                {
                    image.fillAmount = 0f; // Set the initial fill amount to 0 (empty) for other health values
                }
            }
            else
            {
                targetFillAmount = (100f - health) / 100f;
                image.fillAmount = targetFillAmount;
            }

            if (image.type != Image.Type.Filled)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Vertical;
            }

            // Set the fill origin based on the ReverseFillDirection option
            if (PluginConfig.ConfigFillDirection.Value == FillDirection.TopToBottom)
            {
                image.fillOrigin = (int)Image.OriginVertical.Top;
            }
            else
            {
                image.fillOrigin = (int)Image.OriginVertical.Bottom;
            }

            if (PluginConfig.ConfigEnableSuitColorOverlay.Value == true && controllerInstance != null)
            {
                // Get the player's current suit ID
                int currentSuitID = controllerInstance.currentSuitID;
                Debug.LogFormat("Current Suit ID in HealthIndicatorPatch: {0}", currentSuitID);

                // Update the health overlay color based on the suit ID
                UnlockableSuitPatch.UpdateHealthOverlayColor(controllerInstance, currentSuitID);

                Debug.Log("Health overlay color updated in HealthIndicatorPatch");
            }
            else
            {
                if (ColorUtility.TryParseHtmlString("#" + PluginConfig.ConfigOverlayColorHex.Value, out Color overlayColor))
                {
                    image.color = overlayColor;
                }
                else
                {
                    image.color = Color.red;
                }
            }
        }

        private static void HandlePlayerHealthState(HUDManager hudManager, int health, bool hurtPlayer)
        {
            if (hudManager.selfRedCanvasGroup != null)
            {
                if (PluginConfig.ConfigStartFullyFilled.Value)
                {
                    hudManager.selfRedCanvasGroup.alpha = health / 100f;
                }
                else
                {
                    hudManager.selfRedCanvasGroup.alpha = (100f - health) / 100f;
                }
            }
            else
            {
                Debug.LogError("selfRedCanvasGroup is null!");
            }

            if (health >= CriticalHealthThreshold && playerIsCriticallyInjured)
            {
                playerIsCriticallyInjured = false;
                if (hudManager.HUDAnimator != null)
                {
                    hudManager.HUDAnimator.SetTrigger(HealFromCriticalTrigger);
                }
                else
                {
                    Debug.LogError("HUDAnimator is null!");
                }
            }

            if (health >= CriticalHealthThreshold && playerIsCriticallyInjured)
            {
                playerIsCriticallyInjured = false;
                hudManager.HUDAnimator.SetTrigger(HealFromCriticalTrigger);
            }

            if (hurtPlayer && health > 0)
            {
                ProcessPlayerInjury(hudManager, health);
            }
        }

        private static void ProcessPlayerInjury(HUDManager hudManager, int health)
        {
            if (health < CriticalHealthThreshold)
            {
                playerIsCriticallyInjured = true;
                if (PluginConfig.ConfigEnableCriticalHitEffect.Value == true)
                {
                    if (hudManager.HUDAnimator != null)
                    {
                        hudManager.HUDAnimator.SetTrigger(CriticalHitTrigger);
                    }
                    else
                    {
                        Debug.LogError("HUDAnimator is null!");
                    }

                    if (hudManager.UIAudio != null && hudManager.criticalInjury != null)
                    {
                        hudManager.UIAudio.PlayOneShot(hudManager.criticalInjury, 1f);
                    }
                    else
                    {
                        Debug.LogError("UIAudio or criticalInjury is null!");
                    }
                }
            }
            else if (PluginConfig.ConfigEnableSmallHitEffect.Value == true)
            {
                if (hudManager.HUDAnimator != null)
                {
                    hudManager.HUDAnimator.SetTrigger(SmallHitTrigger);
                }
                else
                {
                    Debug.LogError("HUDAnimator is null!");
                }
            }
        }
    }

    public class HealthIndicatorInitializer : MonoBehaviour
    {
        public float StartFillAmount { get; set; }
        public float TargetFillAmount { get; set; }
        public float PreviousStartFillAmount { get; set; }
    }

    public static class PlayerControllerBPatch
    {
        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPrefix]
        public static void KillPlayerPrefix(PlayerControllerB __instance)
        {
            HUDManager hudManager = HUDManager.Instance;
            if (hudManager?.selfRedCanvasGroup == null)
            {
                Debug.LogError("HUDManager instance or selfRedCanvasGroup is null!");
                return;
            }

            var image = hudManager.selfRedCanvasGroup.GetComponent<Image>();
            if (image == null)
            {
                Debug.LogError("Image component not found on selfRedCanvasGroup!");
                return;
            }

            // Stop any ongoing coroutines related to fill transitions
            if (hudManager != null)
            {
                hudManager.StopAllCoroutines();
            }
            else
            {
                Debug.LogError("HUDManager instance is null!");
            }

            // Set the start and target fill amounts to 0
            HealthIndicatorInitializer initializer = hudManager.gameObject.GetComponent<HealthIndicatorInitializer>();
            if (initializer != null)
            {
                if (PluginConfig.ConfigStartFullyFilled.Value)
                {
                    hudManager.selfRedCanvasGroup.alpha = 1f;
                    initializer.StartFillAmount = 1f;
                    initializer.TargetFillAmount = 1f;
                    image.fillAmount = 1f;
                }
                else
                {
                    initializer.StartFillAmount = 0f;
                    initializer.TargetFillAmount = 0f;
                    image.fillAmount = 0f;
                }
                initializer.PreviousStartFillAmount = initializer.StartFillAmount;
            }

            Debug.Log("Player died. Setting fill amount to 0 and resetting start/target fill amounts.");
        }
    }

    public class HealthMonitorCoroutine : MonoBehaviour
    {
        private int lastHealth = -1;

        private void Start()
        {
            StartCoroutine(MonitorHealth());
        }

        private IEnumerator MonitorHealth()
        {
            while (true)
            {
                PlayerControllerB playerController = PlayerUtils.GetPlayerControllerB();
                if (playerController != null)
                {
                    int currentHealth = playerController.health;
                    if (currentHealth != lastHealth)
                    {
                        lastHealth = currentHealth;
                        HUDManagerPatch.UpdateHealthUIPrefix(currentHealth, false);
                    }
                }
                else
                {
                    Debug.LogError("PlayerControllerB is null!");
                }
                yield return new WaitForSeconds(0.01f); // Adjust the delay as needed
            }
        }
    }
}