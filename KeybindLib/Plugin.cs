using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KeybindLib.Classes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace KeybindLib
{
    [BepInPlugin(modGUID, modName, modVer)]
    internal class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "bulletbot.keybindlib";
        private const string modName = "KeybindLib";
        private const string modVer = "1.0.5";

        internal static Plugin instance;
        internal ManualLogSource logger;
        private readonly Harmony harmony = new Harmony(modGUID);

        private bool hasPatched;
        internal List<Keybind> registeredKeybinds;

        void Awake()
        {
            instance = this;
            logger = BepInEx.Logging.Logger.CreateLogSource(modName);
            registeredKeybinds = new List<Keybind>();
            logger.LogMessage($"{modName} has started.");
            SceneManager.activeSceneChanged += delegate
            {
                if (hasPatched)
                    return;
                harmony.PatchAll();
                logger.LogMessage(
                    $"{registeredKeybinds.Count(x => !x.registeredAsToggle)} keybind(s) and " +
                    $"{registeredKeybinds.Count(x => x.registeredAsToggle)} toggle(s) has been initialized.");
                hasPatched = true;
            };
        }
    }
}