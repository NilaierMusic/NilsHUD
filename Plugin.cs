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
    /// <summary>
    /// The main plugin class for NilsHUD.
    /// </summary>
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const string AsciiLogoResourceName = "NilsHUD.Resources.ascii_logo.txt";
        private static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private static readonly string[] ExcludedScenes = { "InitScene", "InitSceneLaunchOptions", "MainMenu", "InitSceneLANMode" };

        private Harmony harmonyInstance;
        private bool isPluginInitialized = false;

        private void Awake()
        {
            DisplayAsciiLogo();

            harmonyInstance = new Harmony(PluginInfo.PLUGIN_GUID);
            harmonyInstance.PatchAll(typeof(HUDManagerPatch));
            harmonyInstance.PatchAll(typeof(UnlockableSuitPatch));
            harmonyInstance.PatchAll(typeof(HealthBarPatch));

            Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Harmony patches applied.");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void DisplayAsciiLogo()
        {
            string logoText = LoadAsciiLogoFromResource();
            Debug.Log(string.IsNullOrEmpty(logoText) ? $"[{PluginInfo.PLUGIN_NAME}] ASCII logo not found or empty." : logoText);
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
            }

            return string.Empty;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Loaded scene: {scene.name}");

            if (!isPluginInitialized && !ExcludedScenes.Contains(scene.name))
            {
                PluginConfig.BindConfig(Config);
                isPluginInitialized = true;
                Debug.Log($"[{PluginInfo.PLUGIN_NAME}] Plugin initialized in scene: {scene.name}");
            }
        }
    }
}