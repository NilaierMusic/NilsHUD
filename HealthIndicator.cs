using HarmonyLib;
using NilsHUD;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "LateUpdate")]
public static class HealthBarPatch
{
    private static Dictionary<GameNetcodeStuff.PlayerControllerB, Vector3> lastPlayerPositions = new Dictionary<GameNetcodeStuff.PlayerControllerB, Vector3>();
    private const float POSITION_THRESHOLD = 0.1f;

    public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            return;
        }

        HealthBar healthBar = HealthBar.GetHealthBar(__instance);
        if (healthBar != null)
        {
            Vector3 currentPosition = __instance.transform.position;
            if (!lastPlayerPositions.ContainsKey(__instance) || Vector3.Distance(currentPosition, lastPlayerPositions[__instance]) > POSITION_THRESHOLD)
            {
                healthBar.UpdateHealthBarPosition();
                lastPlayerPositions[__instance] = currentPosition;
            }
        }
    }
}

public class HealthChangeHelper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void HandleHealthChange(Image healthBarImage, Image redHealthBarImage, float currentHealth, float maxHealth, float previousHealth)
    {
        Debug.Log($"HandleHealthChange called, healthBarImage: {healthBarImage}, redHealthBarImage: {redHealthBarImage}");

        float fillAmount = currentHealth / maxHealth;

        healthBarImage.fillAmount = fillAmount;
        redHealthBarImage.fillAmount = fillAmount;

        if (currentHealth < previousHealth)
        {
            // Player is being damaged
            Debug.Log($"Player is being damaged, fillAmount: {fillAmount}");
        }
        else if (currentHealth > previousHealth)
        {
            // Player is being healed
            Debug.Log($"Player is being healed, fillAmount: {fillAmount}");
        }
    }
}

public class HealthBar : MonoBehaviour
{
    private GameNetcodeStuff.PlayerControllerB player;
    public Image healthBarImage;
    public Image redHealthBarImage;
    private CanvasGroup healthBarAlpha;
    private CanvasGroup redHealthBarAlpha;
    private bool isLocalPlayer;
    private GameObject healthBarObject;
    private float lastHealth;
    private float fadeOutDelay;
    private float fadeOutTimer = 0f;
    private RectTransform healthBarTransform;
    private RectTransform redHealthBarTransform;
    private RectTransform healthBarFillTransform;

    private static GameNetcodeStuff.PlayerControllerB localPlayerController;
    private static Queue<HealthBar> healthBarPool = new Queue<HealthBar>();
    private float previousHealth;

    private Transform usernameBillboard;
    private Transform playerGlobalHead;
    private CanvasGroup usernameAlpha;

    private float fadeInRate;
    private float fadeOutRate;

    private HealthChangeHelper healthChangeHelper;

    private static int initialHealthBarCount = 4;
    private static List<HealthBar> healthBarList = new List<HealthBar>();
    private static Dictionary<GameNetcodeStuff.PlayerControllerB, HealthBar> playerHealthBarMap = new Dictionary<GameNetcodeStuff.PlayerControllerB, HealthBar>();

    public static HealthBar GetHealthBar(GameNetcodeStuff.PlayerControllerB player)
    {
        if (localPlayerController == null)
        {
            localPlayerController = PlayerUtils.GetPlayerControllerB();
        }

        if (player == localPlayerController)
        {
            return null;
        }

        if (playerHealthBarMap.TryGetValue(player, out HealthBar existingHealthBar))
        {
            if (existingHealthBar != null && existingHealthBar.gameObject.activeSelf)
            {
                return existingHealthBar;
            }
            else
            {
                // Remove the invalid entry from the dictionary
                playerHealthBarMap.Remove(player);
            }
        }

        HealthBar availableHealthBar = healthBarList.Find(hb => !hb.gameObject.activeSelf);

        if (availableHealthBar != null)
        {
            availableHealthBar.player = player;
            availableHealthBar.gameObject.SetActive(true);
            playerHealthBarMap[player] = availableHealthBar;
            return availableHealthBar;
        }
        else
        {
            GameObject healthBarObject = new GameObject("HealthBar");
            healthBarObject.SetActive(false);
            HealthBar healthBar = healthBarObject.AddComponent<HealthBar>();
            healthBar.player = player;
            healthBar.InitializeHealthBar();
            healthBarObject.SetActive(true);
            healthBarList.Add(healthBar);
            playerHealthBarMap[player] = healthBar;
            return healthBar;
        }
    }

    private void Awake()
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            // Disable the health bar game object
            gameObject.SetActive(false);
            return;
        }

        // Check if the player is the local player (using cached value)
        isLocalPlayer = (player == localPlayerController);

        lastHealth = player.health;
        fadeOutDelay = PluginConfig.ConfigFadeOutDelay.Value;

        // Cache the player components
        usernameBillboard = player.usernameBillboard;
        playerGlobalHead = player.playerGlobalHead;
        usernameAlpha = player.usernameAlpha;

        // Cache fade in/out rates
        fadeInRate = 1f / PluginConfig.ConfigFadeInDuration.Value;
        fadeOutRate = 1f / PluginConfig.ConfigFadeOutDuration.Value;

        previousHealth = player.health;

        // Find or create the HealthChangeHelper instance
        GameObject healthChangeHelperObject = GameObject.Find("HealthChangeHelper");
        if (healthChangeHelperObject == null)
        {
            healthChangeHelperObject = new GameObject("HealthChangeHelper");
            healthChangeHelper = healthChangeHelperObject.AddComponent<HealthChangeHelper>();
            Debug.Log("Created new HealthChangeHelper instance");
        }
        else
        {
            healthChangeHelper = healthChangeHelperObject.GetComponent<HealthChangeHelper>();
            Debug.Log("Found existing HealthChangeHelper instance");
        }
    }

    private void InitializeHealthBar()
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            return;
        }

        if (healthBarObject == null)
        {
            // Initialize the health bar components
            healthBarObject = new GameObject("HealthBarObject");
            healthBarObject.transform.SetParent(player.usernameBillboard.transform, false);


            // Create the red health bar image first
            redHealthBarImage = new GameObject("RedHealthBar").AddComponent<Image>();
            redHealthBarImage.transform.SetParent(healthBarObject.transform, false);
            redHealthBarAlpha = redHealthBarImage.gameObject.AddComponent<CanvasGroup>();
            redHealthBarAlpha.alpha = 0f;

            // Create the main health bar image on top of the red health bar
            healthBarImage = new GameObject("HealthBar").AddComponent<Image>();
            healthBarImage.transform.SetParent(healthBarObject.transform, false);
            healthBarAlpha = healthBarImage.gameObject.AddComponent<CanvasGroup>();
            healthBarAlpha.alpha = 0f;

            Debug.Log($"Initialized health bar: healthBarImage={healthBarImage}, redHealthBarImage={redHealthBarImage}");

            // Cache the RectTransform components
            redHealthBarTransform = redHealthBarImage.GetComponent<RectTransform>();
            healthBarTransform = healthBarImage.GetComponent<RectTransform>();

            // Set the position and size of the red health bar
            redHealthBarTransform.anchorMin = new Vector2(0.5f, 0f);
            redHealthBarTransform.anchorMax = new Vector2(0.5f, 0f);
            redHealthBarTransform.sizeDelta = new Vector2(PluginConfig.ConfigRedHealthBarWidth.Value, PluginConfig.ConfigRedHealthBarHeight.Value);
            redHealthBarTransform.anchoredPosition = new Vector2(PluginConfig.ConfigHealthBarPositionX.Value, PluginConfig.ConfigHealthBarPositionY.Value);

            // Set the position and size of the main health bar
            healthBarTransform.anchorMin = new Vector2(0f, 0f);
            healthBarTransform.anchorMax = new Vector2(1f, 0f);
            healthBarTransform.sizeDelta = new Vector2(PluginConfig.ConfigHealthBarWidth.Value, PluginConfig.ConfigHealthBarHeight.Value);
            healthBarTransform.anchoredPosition = new Vector2(PluginConfig.ConfigHealthBarPositionX.Value, PluginConfig.ConfigHealthBarPositionY.Value);

            // Set the color of the health bar
            string healthBarColorHex = PluginConfig.ConfigHealthBarColorHex.Value;
            if (ColorUtility.TryParseHtmlString($"#{healthBarColorHex}", out Color healthBarColor))
            {
                healthBarImage.color = healthBarColor;
            }

            // Set the color of the red health bar
            string redHealthBarColorHex = PluginConfig.ConfigRedHealthBarColorHex.Value;
            if (ColorUtility.TryParseHtmlString($"#{redHealthBarColorHex}", out Color redHealthBarColor))
            {
                redHealthBarImage.color = redHealthBarColor;
            }
        }
    }

    private void LateUpdate()
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            return;
        }

        // Update the fade-in/fade-out of the health bar
        UpdateHealthBarFade();

        // Check if the player's health has changed
        if (player.health != lastHealth)
        {
            OnHealthChanged();
        }
    }

    private void OnHealthChanged()
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            return;
        }

        float currentHealth = player.health;
        float maxHealth = 100f; // Adjust this value based on the maximum health of the player
        float previousHealth = lastHealth;

        Debug.Log($"OnHealthChanged called for player: {player.name}, currentHealth: {currentHealth}, previousHealth: {previousHealth}");

        if (player != localPlayerController)
        {
            if (currentHealth != previousHealth)
            {
                healthChangeHelper.HandleHealthChange(healthBarImage, redHealthBarImage, currentHealth, maxHealth, previousHealth);

                // Calculate the fill amount based on the current health
                float fillAmount = currentHealth / maxHealth;

                // Update the anchorMax of the health bar image
                healthBarTransform.anchorMax = new Vector2(fillAmount, healthBarTransform.anchorMax.y);

                // Calculate the new width for the health bar based on the fill amount
                float newHealthBarWidth = fillAmount * PluginConfig.ConfigHealthBarWidth.Value;

                // Update the sizeDelta of the health bar image
                healthBarTransform.sizeDelta = new Vector2(newHealthBarWidth, healthBarTransform.sizeDelta.y);

                // Update the lastHealth variable
                lastHealth = currentHealth;
            }
        }
    }

    private void UpdateHealthBarFade()
    {
        // Hide the health bar for the local player
        if (isLocalPlayer)
        {
            healthBarAlpha.alpha = 0f;
            redHealthBarAlpha.alpha = 0f;
            return;
        }

        // Cache the result of player.usernameAlpha.alpha > 0f
        bool isUsernameVisible = usernameAlpha.alpha > 0f;

        if (isUsernameVisible)
        {
            // Reset the fade-out timer
            fadeOutTimer = 0f;

            // Fade in the health bar
            if (healthBarAlpha.alpha < 1f)
            {
                healthBarAlpha.alpha += Time.deltaTime * fadeInRate;
                healthBarAlpha.alpha = Mathf.Clamp01(healthBarAlpha.alpha);
            }

            // Fade in the red health bar
            if (redHealthBarAlpha.alpha < 1f)
            {
                redHealthBarAlpha.alpha += Time.deltaTime * fadeInRate;
                redHealthBarAlpha.alpha = Mathf.Clamp01(redHealthBarAlpha.alpha);
            }
        }
        else
        {
            // Increment the fade-out timer
            fadeOutTimer += Time.deltaTime;

            // Start fading out the health bar after the delay
            if (fadeOutTimer >= fadeOutDelay)
            {
                // Fade out the health bar
                healthBarAlpha.alpha -= Time.deltaTime * fadeOutRate;
                healthBarAlpha.alpha = Mathf.Clamp01(healthBarAlpha.alpha);

                // Fade out the red health bar
                redHealthBarAlpha.alpha -= Time.deltaTime * fadeOutRate;
                redHealthBarAlpha.alpha = Mathf.Clamp01(redHealthBarAlpha.alpha);

                // Deactivate the health bar object when fully faded out
                if (healthBarAlpha.alpha <= 0f && redHealthBarAlpha.alpha <= 0f)
                {
                    healthBarPool.Enqueue(this);
                    gameObject.SetActive(false);
                }
            }
        }
    }

    public void UpdateHealthBarPosition()
    {
        // Check if the health bar is enabled in the configuration
        if (!PluginConfig.ConfigEnableHealthBar.Value)
        {
            return;
        }
        // Update the position of the health bar based on the player's head position
        Vector3 headPosition = playerGlobalHead.position;
        usernameBillboard.position = new Vector3(headPosition.x, headPosition.y + 0.55f, headPosition.z);
    }

    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), "Start")]
    public static class AddHealthBarPatch
    {
        public static void Postfix(GameNetcodeStuff.PlayerControllerB __instance)
        {
            // Add health bar to all player controllers in the scene, except for the local player
            if (__instance != PlayerUtils.GetPlayerControllerB())
            {
                if (healthBarList.Count < initialHealthBarCount)
                {
                    HealthBar.GetHealthBar(__instance);
                }
            }
        }
    }
}