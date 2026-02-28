using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;

namespace KeybindLib.Classes
{
    public static class Keybinds
    {
        internal static readonly Dictionary<string, int> _modOrder = new Dictionary<string, int>();
        internal static int _nextRegisterIndex = 0;
        internal static int _nextModOrderIndex = 0;

        private static BepInPlugin GetPluginByCallingAssembly(Assembly assembly)
        {
            IEnumerable<Type> SafeTypes()
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null);
                }
                catch
                {
                    try
                    {
                        return assembly.GetExportedTypes();
                    }
                    catch
                    {
                        return Enumerable.Empty<Type>();
                    }
                }
            }
            return SafeTypes()
                .Select(t =>
                {
                    try { return t.GetCustomAttribute<BepInPlugin>(); }
                    catch { return null; }
                }).FirstOrDefault(attr => attr != null);
        }

        public static Keybind Bind(string name, string binding) => 
            Register(GetPluginByCallingAssembly(Assembly.GetCallingAssembly()), null, name, binding);

        public static Keybind Bind(string category, string name, string binding) => 
            Register(GetPluginByCallingAssembly(Assembly.GetCallingAssembly()), category, name, binding);

        public static Keybind Toggle(Keybind.Toggle toggle) => 
            Register(GetPluginByCallingAssembly(Assembly.GetCallingAssembly()), toggle: toggle, isToggle: true);

        public static Keybind Toggle(string category, Keybind.Toggle toggle) => 
            Register(GetPluginByCallingAssembly(Assembly.GetCallingAssembly()), category, toggle: toggle, isToggle: true);

        public static Keybind Toggle(Keybind keybind, Keybind.Toggle toggle)
        {
            BepInPlugin plugin =
                Assembly.GetCallingAssembly().GetTypes().Select(x => x.GetCustomAttribute<BepInPlugin>()).FirstOrDefault();
            if (plugin == null)
            {
                Plugin.instance.logger.LogMessage(
                    "Failed to apply toggle because the calling mod could not be identified.");
                return null;
            }
            if (keybind == null)
            {
                Plugin.instance.logger.LogMessage(
                    "Failed to apply toggle because the provided keybind was null.");
                return null;
            }
            bool isToggle = keybind.registeredAsToggle;
            string modGuid = keybind.modGuid;
            string category = keybind.category;
            string name = keybind.inputAction != null ? keybind.inputAction.name : null;
            bool hasToggle = keybind.toggle != null;
            if (modGuid == plugin.GUID && Plugin.instance.registeredKeybinds.Contains(keybind))
            {
                Plugin.instance.logger.LogMessage(
                    $"{(toggle == null ? "Removed" : (hasToggle ? "Replaced" : "Attached"))} toggle" +
                    (!isToggle ? (toggle == null ? " from" : " to") +
                    $" keybind{(!string.IsNullOrEmpty(name) ? $" \"{name}\"" : "")}" : "") +
                    (!string.IsNullOrEmpty(category) ? $" in category \"{category}\"" : "") +
                    $" for mod \"{modGuid}\".");
                keybind.toggle = toggle;
                return keybind;
            }
            Plugin.instance.logger.LogMessage(
                $"Failed to {(toggle == null ? "remove" : (hasToggle ? "replace" : "attach"))} toggle" +
                (!isToggle ? (toggle == null ? " from" : " to") +
                    $" keybind{(!string.IsNullOrEmpty(name) ? $" \"{name}\"" : "")}" : "") +
                $"{(!string.IsNullOrEmpty(keybind.category) ? $" in category \"{category}\"" : "")}" +
                $" for mod \"{modGuid}\".");
            return null;
        }

        internal static Keybind Register(BepInPlugin plugin, string category = null, string name = null, string binding = null,
            Keybind.Toggle toggle = null, bool isToggle = false)
        {
            if (plugin == null)
            {
                Plugin.instance.logger.LogMessage(
                    $"Failed to register a {(isToggle ? "toggle" : "keybind")} because the calling mod could not be identified.");
                return null;
            }
            string modGuid = plugin.GUID;
            InputKey ComputeInputKey()
            {
                if (string.IsNullOrEmpty(category))
                    category = string.Empty;
                if (string.IsNullOrEmpty(name))
                    name = string.Empty;
                string key = modGuid + "|" + category + "|" + name;
                unchecked
                {
                    uint hash = 2166136261u;
                    for (int i = 0; i < key.Length; i++)
                    {
                        hash ^= key[i];
                        hash *= 16777619u;
                    }
                    hash &= 0x3FFFFFFFu;
                    hash |= 0x40000000u;
                    return (InputKey)(int)hash;
                }
            }
            InputKey inputKey = ComputeInputKey();
            bool SameCategory(Keybind x) =>
                (string.IsNullOrEmpty(x.category) && string.IsNullOrEmpty(category)) ||
                (!string.IsNullOrEmpty(x.category) && !string.IsNullOrEmpty(category) &&
                 x.category.Equals(category, StringComparison.OrdinalIgnoreCase));
            bool keybindConflict = !isToggle && (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(binding) ||
                Plugin.instance.registeredKeybinds.Any(x =>
                    x.modGuid == modGuid &&
                    SameCategory(x) &&
                    !x.registeredAsToggle &&
                    x.inputKey == inputKey)
            );
            bool toggleConflict = isToggle && (toggle == null ||
                Plugin.instance.registeredKeybinds.Any(x =>
                    x.modGuid == modGuid &&
                    SameCategory(x) &&
                    x.registeredAsToggle &&
                    x.inputKey == inputKey)
            );
            if (keybindConflict || toggleConflict)
            {
                Plugin.instance.logger.LogMessage(
                    $"Failed to register {(isToggle ? "toggle" : $"keybind{(!string.IsNullOrEmpty(name) ? $" \"{name}\"" : "")}")}" +
                    $"{(!string.IsNullOrEmpty(category) ? $" in category \"{category}\"" : "")}" +
                    $" for mod \"{modGuid}\".");
                return null;
            }
            Keybind keybind = new Keybind()
            {
                modGuid = modGuid,
                category = category,
                inputKey = inputKey
            };
            if (isToggle)
                keybind.toggle = toggle;
            else
            {
                keybind.tag = $"[{(int)inputKey}]";
                keybind.inputAction = new InputAction(name, binding: binding);
            }
            keybind.registeredAsToggle = isToggle;
            if (!_modOrder.TryGetValue(modGuid, out int modIndex))
            {
                modIndex = _nextModOrderIndex++;
                _modOrder[modGuid] = modIndex;
            }
            keybind.modOrderIndex = modIndex;
            keybind.registerIndex = _nextRegisterIndex++;
            Plugin.instance.registeredKeybinds.Add(keybind);
            Plugin.instance.logger.LogMessage(
                $"Registered {(isToggle ? "toggle" : $"keybind \"{name}\"")}" +
                $"{(!string.IsNullOrEmpty(category) ? $" in category \"{category}\"" : "")}" +
                $" for mod \"{modGuid}\".");
            return keybind;
        }
    }
}
