using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NilsHUD
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string AsciiLogoResourceName = "NilsHUD.Resources.ascii_logo.txt";
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private static readonly string[] ExcludedScenes = { "InitScene", "InitSceneLaunchOptions", "InitSceneLANMode", "MainMenu", "ColdOpen1" };

        private Harmony? harmonyInstance;
        private bool isPluginInitialized = false;

        private void Awake()
        {
            try
            {
                DisplayAsciiLogo();

                // Bind the config
                PluginConfig.BindConfig(Config);

                Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Config loaded.");

                // Subscribe to the scene loaded event
                SceneManager.sceneLoaded += OnSceneLoaded;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Error in Awake: {ex.Message}");
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Stack trace: {ex.StackTrace}");
            }
        }

        private void DisplayAsciiLogo()
        {
            try
            {
                string logoText = LoadAsciiLogoFromResource();
                Debug.Log(string.IsNullOrEmpty(logoText) ? $"[{PluginInfo.PLUGIN_NAME}] ASCII logo not found or empty." : logoText);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Error displaying ASCII logo: {ex.Message}");
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Stack trace: {ex.StackTrace}");
            }
        }

        private void InitializePlugin()
        {
            try
            {
                harmonyInstance = new Harmony(PluginInfo.PLUGIN_GUID);
                harmonyInstance.PatchAll(typeof(HUDManagerPatch));
                harmonyInstance.PatchAll(typeof(UnlockableSuitPatch));
                harmonyInstance.PatchAll(typeof(HealthBarPatch));
                harmonyInstance.PatchAll(typeof(PlayerControllerBPatch));

                Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Harmony patches applied.");

                // Precompute suit colors
                UnlockableSuitPatch.PrecomputeSuitColors();

                // Initialize the health overlay
                HUDManagerPatch.InitializeHealthOverlay();

                // Initialize EmoteHUDManager and apply necessary patches
                EmoteHUDManager.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Error in InitializePlugin: {ex.Message}");
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Stack trace: {ex.StackTrace}");
            }
        }

        private string LoadAsciiLogoFromResource()
        {
            try
            {
                using (Stream stream = ExecutingAssembly.GetManifestResourceStream(AsciiLogoResourceName))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Error loading ASCII logo: {ex.Message}");
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Stack trace: {ex.StackTrace}");
            }

            return string.Empty;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Loaded scene: {scene.name}");

                if (!isPluginInitialized && !ExcludedScenes.Contains(scene.name))
                {
                    InitializePlugin();
                    isPluginInitialized = true;
                    Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Plugin initialized in scene: {scene.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Error in OnSceneLoaded: {ex.Message}");
                Debug.LogError($"[{PluginInfo.PLUGIN_NAME}] Stack trace: {ex.StackTrace}");
            }
        }
    }
}
