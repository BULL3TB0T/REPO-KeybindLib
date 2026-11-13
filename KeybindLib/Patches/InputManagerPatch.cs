using HarmonyLib;
using KeybindLib.Classes;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine.InputSystem;

namespace KeybindLib.Patches
{
    [HarmonyPatch(typeof(InputManager))]
    internal class InputManagerPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StartTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            matcher.MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "DefaultKeyBindings.es3")
            );
            matcher.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(InputManager), "tagDictionary")),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InputManagerPatch), "Start"))
            );
            return matcher.InstructionEnumeration();
        }

        static void Start(ref Dictionary<string, InputKey> tagDictionary)
        {
            foreach (Keybind keybind in Plugin.instance.registeredKeybinds)
            {
                if (string.IsNullOrEmpty(keybind.tag))
                    continue;
                tagDictionary.Add(keybind.tag, keybind.inputKey);
            }
        }

        [HarmonyPatch("InitializeInputs")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> InitializeInputsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            CodeMatcher matcher = new CodeMatcher(instructions);
            int lastMatchPos = -1;
            while (true)
            {
                matcher.MatchForward(false,
                    new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Dictionary<InputKey, InputAction>), "set_Item"))
                );
                if (!matcher.IsValid)
                    break;
                lastMatchPos = matcher.Pos;
                matcher.Advance(1);
            }
            if (lastMatchPos != -1)
            {
                matcher.Start();
                matcher.Advance(lastMatchPos + 1);
                matcher.Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(InputManager), "inputActions")),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldflda, AccessTools.Field(typeof(InputManager), "inputToggleKeyboardMouse")),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InputManagerPatch), "InitializeInputs"))
                );
            }
            return matcher.InstructionEnumeration();
        }

        static void InitializeInputs(ref Dictionary<InputKey, InputAction> inputActions, ref Dictionary<InputKey, bool> inputToggle)
        {
            foreach (Keybind keybind in Plugin.instance.registeredKeybinds)
            {
                if (keybind.inputAction != null)
                    inputActions[keybind.inputKey] = keybind.inputAction;
                if (keybind.toggle != null)
                    inputToggle.Add(keybind.inputKey, keybind.toggle.defaultValue);
            }
        }
    }
}