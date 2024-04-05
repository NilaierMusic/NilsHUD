using BepInEx.Configuration;

namespace NilsHUD
{
    public class PluginConfig
    {
        // Default values
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

        // Overlay Settings
        /// <summary>
        /// The color of the overlay image in hexadecimal format (e.g., FF0000 for red).
        /// </summary>
        public static ConfigEntry<string> ConfigOverlayColorHex;

        /// <summary>
        /// Enables the fill animation. If false, the fill amount is set instantly.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableFillAnimation;

        /// <summary>
        /// The fill direction of the health indicator.
        /// </summary>
        public static ConfigEntry<FillDirection> ConfigFillDirection;

        /// <summary>
        /// The duration over which the health indicator's fill amount changes.
        /// Lower values make the transition faster, higher values make it smoother.
        /// </summary>
        public static ConfigEntry<float> ConfigFillTransitionDuration;

        /// <summary>
        /// Enables the feature where the player's suit changes the color of the damage overlay.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableSuitColorOverlay;

        // Fade-Out Settings
        /// <summary>
        /// Enables the fade-out effect for the filling overlay.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableFadeOut;

        /// <summary>
        /// The duration of the fade-out effect for the filling overlay (in seconds).
        /// </summary>
        public static ConfigEntry<float> ConfigFadeOutDuration;

        /// <summary>
        /// The intensity of the fade-out effect (0 for no fade-out, 1 for full fade-out).
        /// </summary>
        public static ConfigEntry<float> ConfigFadeOutIntensity;

        // Effects Settings
        /// <summary>
        /// Enables the critical hit animation and sound effect.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableCriticalHitEffect;

        /// <summary>
        /// Enables the small hit animation.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableSmallHitEffect;

        // Health Bar Settings
        /// <summary>
        /// Enables the health bar.
        /// </summary>
        public static ConfigEntry<bool> ConfigEnableHealthBar;

        /// <summary>
        /// The color of the health bar in hexadecimal format (e.g., FF8000 for orange).
        /// </summary>
        public static ConfigEntry<string> ConfigHealthBarColorHex;

        /// <summary>
        /// The color of the red health bar in hexadecimal format (e.g., FF0000 for red).
        /// </summary>
        public static ConfigEntry<string> ConfigRedHealthBarColorHex;

        /// <summary>
        /// The X position of the health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigHealthBarPositionX;

        /// <summary>
        /// The Y position of the health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigHealthBarPositionY;

        /// <summary>
        /// The duration of the fade-in effect for the health bar (in seconds).
        /// </summary>
        public static ConfigEntry<float> ConfigFadeInDuration;

        /// <summary>
        /// The delay before the health bar starts fading out (in seconds).
        /// </summary>
        public static ConfigEntry<float> ConfigFadeOutDelay;

        /// <summary>
        /// The width of the health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigHealthBarWidth;

        /// <summary>
        /// The height of the health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigHealthBarHeight;

        /// <summary>
        /// The width of the red health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigRedHealthBarWidth;

        /// <summary>
        /// The height of the red health bar.
        /// </summary>
        public static ConfigEntry<float> ConfigRedHealthBarHeight;

        public static void BindConfig(ConfigFile config)
        {
            // Overlay Settings
            ConfigOverlayColorHex = config.Bind("Overlay", "OverlayColorHex", DefaultOverlayColor, "The color of the overlay image in hexadecimal format (e.g., FF0000 for red).");
            ConfigEnableFillAnimation = config.Bind("Overlay", "EnableFillAnimation", true, "Enables the fill animation. If false, the fill amount is set instantly.");
            ConfigFillDirection = config.Bind("Overlay", "FillDirection", FillDirection.BottomToTop, "The fill direction of the health indicator.");
            ConfigFillTransitionDuration = config.Bind("Overlay", "FillTransitionDuration", DefaultFillTransitionDuration, "The duration over which the health indicator's fill amount changes. Lower values make the transition faster, higher values make it smoother.");
            ConfigEnableSuitColorOverlay = config.Bind("Overlay", "EnableSuitColorOverlay", false, "Enables the feature where the player's suit changes the color of the damage overlay.");

            // Fade-Out Settings
            ConfigEnableFadeOut = config.Bind("FadeOut", "EnableFadeOut", true, "Enables the fade-out effect for the filling overlay.");
            ConfigFadeOutDuration = config.Bind("FadeOut", "FadeOutDuration", DefaultFadeOutDuration, "The duration of the fade-out effect for the filling overlay (in seconds).");
            ConfigFadeOutIntensity = config.Bind("FadeOut", "FadeOutIntensity", DefaultFadeOutIntensity, "The intensity of the fade-out effect (0 for no fade-out, 1 for full fade-out).");

            // Effects Settings
            ConfigEnableCriticalHitEffect = config.Bind("Effects", "EnableCriticalHitEffect", true, "Enables the critical hit animation and sound effect.");
            ConfigEnableSmallHitEffect = config.Bind("Effects", "EnableSmallHitEffect", true, "Enables the small hit effect.");

            // Health Bar Settings
            ConfigEnableHealthBar = config.Bind("Experimental", "EnableHealthBar", false, "Enables the health bar.");
            ConfigHealthBarColorHex = config.Bind("Experimental", "HealthBarColorHex", DefaultHealthBarColor, "The color of the health bar in hexadecimal format (e.g., FF8000 for orange).");
            ConfigRedHealthBarColorHex = config.Bind("Experimental", "RedHealthBarColorHex", DefaultRedHealthBarColor, "The color of the red health bar in hexadecimal format (e.g., FF0000 for red).");
            ConfigHealthBarPositionX = config.Bind("Experimental", "HealthBarPositionX", DefaultHealthBarPositionX, "The X position of the health bar.");
            ConfigHealthBarPositionY = config.Bind("Experimental", "HealthBarPositionY", DefaultHealthBarPositionY, "The Y position of the health bar.");
            ConfigFadeInDuration = config.Bind("Experimental", "FadeInDuration", DefaultFadeInDuration, "The duration of the fade-in effect for the health bar (in seconds).");
            ConfigFadeOutDelay = config.Bind("Experimental", "FadeOutDelay", DefaultFadeOutDelay, "The delay before the health bar starts fading out (in seconds).");
            ConfigHealthBarWidth = config.Bind("Experimental", "HealthBarWidth", DefaultHealthBarWidth, "The width of the health bar.");
            ConfigHealthBarHeight = config.Bind("Experimental", "HealthBarHeight", DefaultHealthBarHeight, "The height of the health bar.");
            ConfigRedHealthBarWidth = config.Bind("Experimental", "RedHealthBarWidth", DefaultRedHealthBarWidth, "The width of the red health bar.");
            ConfigRedHealthBarHeight = config.Bind("Experimental", "RedHealthBarHeight", DefaultRedHealthBarHeight, "The height of the red health bar.");
        }
    }

    public enum FillDirection
    {
        BottomToTop,
        TopToBottom
    }
}