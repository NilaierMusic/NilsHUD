using BepInEx.Configuration;
using UnityEngine;
using System;
using LethalConfig.ConfigItems.Options;
using LethalConfig.ConfigItems;
using System.IO;
using System.Reflection;

namespace NilsHUD
{
    public class PluginConfig
    {
        private const string DefaultOverlayColor = "FF0000";
        private const float DefaultFillTransitionDuration = 0.1f;
        private const float DefaultFadeOutDuration = 0.5f;
        private const float DefaultFadeOutIntensity = 0.5f;
        private const string DefaultHealthBarColor = "FF8000";
        private const string DefaultRedHealthBarColor = "FF0000";
        private const float DefaultHealthBarPositionX = 0f;
        private const float DefaultHealthBarPositionY = 30f;
        private const float DefaultFadeInDuration = 0.5f;
        private const float DefaultFadeOutDelay = 1f;
        private const float DefaultHealthBarWidth = 300f;
        private const float DefaultHealthBarHeight = 15f;
        private const float DefaultRedHealthBarWidth = 300f;
        private const float DefaultRedHealthBarHeight = 15f;

        public static ConfigEntry<string>? ConfigOverlayColorHex;
        public static ConfigEntry<bool>? ConfigEnableFillAnimation;
        public static ConfigEntry<FillDirection>? ConfigFillDirection;
        public static ConfigEntry<float>? ConfigFillTransitionDuration;
        public static ConfigEntry<bool>? ConfigStartFullyFilled;
        public static ConfigEntry<bool>? ConfigEnableSuitColorOverlay;
        public static ConfigEntry<float>? ConfigSuitOverlayBrightness;
        public static ConfigEntry<bool>? ConfigEnableFadeOut;
        public static ConfigEntry<float>? ConfigFadeOutDuration;
        public static ConfigEntry<float>? ConfigFadeOutIntensity;
        public static ConfigEntry<bool>? ConfigEnableCriticalHitEffect;
        public static ConfigEntry<bool>? ConfigEnableSmallHitEffect;
        public static ConfigEntry<bool>? ConfigEnableHealthBar;
        public static ConfigEntry<string>? ConfigHealthBarColorHex;
        public static ConfigEntry<string>? ConfigRedHealthBarColorHex;
        public static ConfigEntry<float>? ConfigHealthBarPositionX;
        public static ConfigEntry<float>? ConfigHealthBarPositionY;
        public static ConfigEntry<float>? ConfigFadeInDuration;
        public static ConfigEntry<float>? ConfigFadeOutDelay;
        public static ConfigEntry<float>? ConfigHealthBarWidth;
        public static ConfigEntry<float>? ConfigHealthBarHeight;
        public static ConfigEntry<float>? ConfigRedHealthBarWidth;
        public static ConfigEntry<float>? ConfigRedHealthBarHeight;

        private const string ModIconResourceName = "NilsHUD.Resources.icon.png";
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        public static void BindConfig(ConfigFile config)
        {
            try
            {
                ConfigOverlayColorHex = config.Bind("Overlay", "OverlayColorHex", DefaultOverlayColor);
                ConfigEnableFillAnimation = config.Bind("Overlay", "EnableFillAnimation", true);
                ConfigFillDirection = config.Bind("Overlay", "FillDirection", FillDirection.BottomToTop);
                ConfigFillTransitionDuration = config.Bind("Overlay", "FillTransitionDuration", DefaultFillTransitionDuration);
                ConfigStartFullyFilled = config.Bind("Overlay", "StartFullyFilled", false);
                ConfigEnableSuitColorOverlay = config.Bind("Overlay", "EnableSuitColorOverlay", false);
                ConfigSuitOverlayBrightness = config.Bind("Overlay", "SuitOverlayBrightness", 1f);
                ConfigEnableFadeOut = config.Bind("FadeOut", "EnableFadeOut", true);
                ConfigFadeOutDuration = config.Bind("FadeOut", "FadeOutDuration", DefaultFadeOutDuration);
                ConfigFadeOutIntensity = config.Bind("FadeOut", "FadeOutIntensity", DefaultFadeOutIntensity);
                ConfigEnableCriticalHitEffect = config.Bind("Effects", "EnableCriticalHitEffect", true);
                ConfigEnableSmallHitEffect = config.Bind("Effects", "EnableSmallHitEffect", true);
                ConfigEnableHealthBar = config.Bind("Experimental", "EnableHealthBar", false);
                ConfigHealthBarColorHex = config.Bind("Experimental", "HealthBarColorHex", DefaultHealthBarColor);
                ConfigRedHealthBarColorHex = config.Bind("Experimental", "RedHealthBarColorHex", DefaultRedHealthBarColor);
                ConfigHealthBarPositionX = config.Bind("Experimental", "HealthBarPositionX", DefaultHealthBarPositionX);
                ConfigHealthBarPositionY = config.Bind("Experimental", "HealthBarPositionY", DefaultHealthBarPositionY);
                ConfigFadeInDuration = config.Bind("Experimental", "FadeInDuration", DefaultFadeInDuration);
                ConfigFadeOutDelay = config.Bind("Experimental", "FadeOutDelay", DefaultFadeOutDelay);
                ConfigHealthBarWidth = config.Bind("Experimental", "HealthBarWidth", DefaultHealthBarWidth);
                ConfigHealthBarHeight = config.Bind("Experimental", "HealthBarHeight", DefaultHealthBarHeight);
                ConfigRedHealthBarWidth = config.Bind("Experimental", "RedHealthBarWidth", DefaultRedHealthBarWidth);
                ConfigRedHealthBarHeight = config.Bind("Experimental", "RedHealthBarHeight", DefaultRedHealthBarHeight);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NilsHUD] Error while binding config: {ex.Message}");
                Debug.LogError($"[NilsHUD] Stack trace: {ex.StackTrace}");
            }

            // Check if LethalConfig is available
            try
            {
                var lethalConfigManagerType = Type.GetType("LethalConfig.LethalConfigManager, LethalConfig");
                if (lethalConfigManagerType != null)
                {
                    AddLethalConfigItems();
                    SetModIconAndDescription();
                }
                else
                {
                    // LethalConfig is not present, log a message or handle it gracefully
                    Debug.Log("LethalConfig is not installed. Using default config values.");
                }
            }
            catch (FileNotFoundException)
            {
                // LethalConfig assembly is not found, log a message or handle it gracefully
                Debug.Log("LethalConfig assembly is not found. Using default config values.");
            }
        }

        private static void SetModIconAndDescription()
        {
            try
            {
                // Load and set the mod's icon
                Sprite modIcon = LoadModIcon();
                if (modIcon != null)
                {
                    var setModIconMethod = Type.GetType("LethalConfig.LethalConfigManager, LethalConfig")?.GetMethod("SetModIcon");
                    setModIconMethod?.Invoke(null, new object[] { modIcon });
                }

                // Set the mod's description
                var setModDescriptionMethod = Type.GetType("LethalConfig.LethalConfigManager, LethalConfig")?.GetMethod("SetModDescription");
                setModDescriptionMethod?.Invoke(null, new object[] { "Better Vanilla-like Health HUD" });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NilsHUD] Error while setting mod icon and description: {ex.Message}");
                Debug.LogError($"[NilsHUD] Stack trace: {ex.StackTrace}");
            }
        }

        private static Sprite LoadModIcon()
        {
            try
            {
                using (Stream stream = ExecutingAssembly.GetManifestResourceStream(ModIconResourceName))
                {
                    if (stream != null)
                    {
                        byte[] iconData = new byte[stream.Length];
                        stream.Read(iconData, 0, iconData.Length);

                        Texture2D iconTexture = new Texture2D(2, 2);
                        // Updated method to load image data into the texture
                        if (ImageConversion.LoadImage(iconTexture, iconData))
                        {
                            Sprite iconSprite = Sprite.Create(iconTexture, new Rect(0, 0, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                            return iconSprite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NilsHUD] Error loading mod icon: {ex.Message}");
                Debug.LogError($"[NilsHUD] Stack trace: {ex.StackTrace}");
            }

            return null;
        }

        private static void AddLethalConfigItems()
        {
            try
            {
                var lethalConfigManagerType = Type.GetType("LethalConfig.LethalConfigManager, LethalConfig");
                var addConfigItemMethod = lethalConfigManagerType?.GetMethod("AddConfigItem", new Type[] { typeof(BaseConfigItem) });

                if (addConfigItemMethod != null)
                {
                    addConfigItemMethod.Invoke(null, new object[] { new TextInputFieldConfigItem(ConfigOverlayColorHex) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableFillAnimation) });
                    addConfigItemMethod.Invoke(null, new object[] { new EnumDropDownConfigItem<FillDirection>(ConfigFillDirection, new EnumDropDownOptions()) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigFillTransitionDuration, new FloatSliderOptions { Min = 0f, Max = 60f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigStartFullyFilled) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableSuitColorOverlay) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigSuitOverlayBrightness, new FloatSliderOptions { Min = 0f, Max = 2f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableFadeOut) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigFadeOutDuration, new FloatSliderOptions { Min = 0f, Max = 60f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigFadeOutIntensity, new FloatSliderOptions { Min = 0f, Max = 1f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableCriticalHitEffect) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableSmallHitEffect) });
                    addConfigItemMethod.Invoke(null, new object[] { new BoolCheckBoxConfigItem(ConfigEnableHealthBar) });
                    addConfigItemMethod.Invoke(null, new object[] { new TextInputFieldConfigItem(ConfigHealthBarColorHex) });
                    addConfigItemMethod.Invoke(null, new object[] { new TextInputFieldConfigItem(ConfigRedHealthBarColorHex) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigHealthBarPositionX, new FloatSliderOptions { Min = -500f, Max = 500f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigHealthBarPositionY, new FloatSliderOptions { Min = -500f, Max = 500f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigFadeInDuration, new FloatSliderOptions { Min = 0f, Max = 60f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigFadeOutDelay, new FloatSliderOptions { Min = 0f, Max = 60f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigHealthBarWidth, new FloatSliderOptions { Min = 1f, Max = 500f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigHealthBarHeight, new FloatSliderOptions { Min = 1f, Max = 500f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigRedHealthBarWidth, new FloatSliderOptions { Min = 1f, Max = 500f }) });
                    addConfigItemMethod.Invoke(null, new object[] { new FloatSliderConfigItem(ConfigRedHealthBarHeight, new FloatSliderOptions { Min = 1f, Max = 500f }) });
                }
                else
                {
                    Debug.LogError("[NilsHUD] Failed to find AddConfigItem method in LethalConfig.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NilsHUD] Error while adding LethalConfig items: {ex.Message}");
                Debug.LogError($"[NilsHUD] Stack trace: {ex.StackTrace}");
            }
        }
    }

    public enum FillDirection
    {
        BottomToTop,
        TopToBottom
    }
}