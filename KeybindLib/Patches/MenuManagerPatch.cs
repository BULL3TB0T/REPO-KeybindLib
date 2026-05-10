using HarmonyLib;
using KeybindLib.Classes;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace KeybindLib.Patches
{
    [HarmonyPatch(typeof(MenuManager))]
    internal class MenuManagerPatch
    {
        private static readonly float _headerOffset = 30;
        private static readonly float _categoryOffset = 26.5f;
        private static readonly float _categoryHeaderOffset = 6.5f;
        private static readonly float _buttonOffset = 42;
        private static readonly float _buttonHeaderOffset = 15;
        private static readonly float _buttonCategoryOffset = 11.5f;
        private static readonly float _buttonBoolSettingOffset = 27.25f;
        private static readonly float _buttonBoolSettingHeaderOffset = 12.5f;
        private static readonly float _buttonBoolSettingCategoryOffset = 9;
        private static readonly float _boolSettingOffset = 32.25f;
        private static readonly float _boolSettingHeaderOffset = 15.25f;
        private static readonly float _boolSettingCategoryOffset = 20.25f;

        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static void Start(ref List<MenuManager.MenuPages> ___menuPages)
        {
            if (Plugin.instance.registeredKeybinds.Count <= 0)
                return;
            MenuManager.MenuPages menuPages = ___menuPages.FirstOrDefault(x => x.menuPageIndex == MenuPageIndex.SettingsControls);
            Transform scrollBox = menuPages.menuPage.transform.GetChild(3);
            MenuScrollBox menuScrollBox = scrollBox.GetComponent<MenuScrollBox>();
            Transform content = scrollBox.GetChild(1).GetChild(0);
            GameObject header = content.Find("Header Player Expressions").gameObject;
            Vector3 headerPosition = header.transform.localPosition;
            GameObject category = content.Find("Header Grabbing").gameObject;
            Vector3 categoryPosition = category.transform.localPosition;
            GameObject button = content.Find("Big Button Map").gameObject;
            Vector3 buttonPosition = button.transform.localPosition;
            GameObject buttonBoolSetting = content.Find("Bool Setting - ").gameObject;
            Vector3 buttonBoolSettingPosition = buttonBoolSetting.transform.localPosition;
            GameObject boolSetting = null;
            foreach (Transform transform in content)
            {
                if (transform.name == "Bool Setting - ")
                    boolSetting = transform.gameObject;
            }
            Vector3 boolSettingPosition = boolSetting.transform.localPosition;
            List<string> currentHeaders = new List<string>();
            List<(string, string)> currentCategories = new List<(string, string)>();
            Plugin.instance.registeredKeybinds.Sort((a, b) =>
            {
                int compare = a.modOrderIndex.CompareTo(b.modOrderIndex);
                if (compare != 0)
                    return compare;
                bool aHasCategory = !string.IsNullOrEmpty(a.category);
                bool bHasCategory = !string.IsNullOrEmpty(b.category);
                if (aHasCategory != bHasCategory)
                    return aHasCategory.CompareTo(bHasCategory);
                bool aIsToggle = a.registeredAsToggle;
                bool bIsToggle = b.registeredAsToggle;
                if (aIsToggle != bIsToggle)
                    return bIsToggle.CompareTo(aIsToggle);
                return a.registerIndex.CompareTo(b.registerIndex);
            });
            float currentPosition = content.GetChild(content.childCount - 1).localPosition.y;
            bool RequiresHeader(Keybind keybind) => !currentHeaders.Contains(keybind.modGuid);
            bool RequiresCategory(Keybind keybind) => !string.IsNullOrEmpty(keybind.category) &&
                !currentCategories.Contains((keybind.modGuid, keybind.category));
            List<Keybind> registeredKeybinds = Plugin.instance.registeredKeybinds;
            for (int i = 0; i < registeredKeybinds.Count; i++)
            {
                Keybind keybind = registeredKeybinds[i];
                bool applyHeaderOffset = RequiresHeader(keybind);
                if (applyHeaderOffset)
                {
                    currentPosition -= _headerOffset;
                    GameObject headerObject = Object.Instantiate(header);
                    foreach (LocalizationChangedEvent e in headerObject.GetComponentsInChildren<LocalizationChangedEvent>())
                        Object.Destroy(e);
                    headerObject.name = $"Header {keybind.modGuid}";
                    headerObject.GetComponentInChildren<TextMeshProUGUI>().text = keybind.modGuid;
                    headerObject.transform.SetParent(content);
                    headerObject.transform.localPosition = new Vector3(headerPosition.x, currentPosition, headerPosition.z);
                    currentHeaders.Add(keybind.modGuid);
                }
                bool applyCategoryOffset = RequiresCategory(keybind);
                if (applyCategoryOffset)
                {
                    currentPosition -= _categoryOffset;
                    if (applyHeaderOffset)
                        currentPosition -= _categoryHeaderOffset;
                    GameObject categoryObject = Object.Instantiate(category);
                    foreach (LocalizationChangedEvent e in categoryObject.GetComponentsInChildren<LocalizationChangedEvent>())
                        Object.Destroy(e);
                    categoryObject.name = $"Category {keybind.modGuid} {keybind.category}";
                    categoryObject.GetComponentInChildren<TextMeshProUGUI>().text = keybind.category;
                    categoryObject.transform.SetParent(content);
                    categoryObject.transform.localPosition = new Vector3(categoryPosition.x, currentPosition, categoryPosition.z);
                    currentCategories.Add((keybind.modGuid, keybind.category));
                    applyCategoryOffset = true;
                }
                if (keybind.inputAction != null)
                {
                    currentPosition -= _buttonOffset;
                    if (applyHeaderOffset)
                        currentPosition -= _buttonHeaderOffset;
                    if (applyCategoryOffset)
                        currentPosition -= _buttonCategoryOffset;
                    GameObject buttonObject = Object.Instantiate(button);
                    foreach (LocalizationChangedEvent e in buttonObject.GetComponentsInChildren<LocalizationChangedEvent>())
                        Object.Destroy(e);
                    string name = keybind.inputAction.name;
                    buttonObject.name = $"Big Button {keybind.modGuid} {name}";
                    buttonObject.GetComponent<MenuBigButton>().buttonTitle = name;
                    buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = name;
                    buttonObject.GetComponent<MenuKeybind>().inputKey = keybind.inputKey;
                    buttonObject.transform.SetParent(content);
                    buttonObject.transform.localPosition = new Vector3(buttonPosition.x, currentPosition, buttonPosition.z);
                    if (keybind.toggle != null)
                    {
                        currentPosition -= _buttonBoolSettingOffset;
                        GameObject boolSettingObject = Object.Instantiate(buttonBoolSetting);
                        foreach (LocalizationChangedEvent e in boolSettingObject.GetComponentsInChildren<LocalizationChangedEvent>())
                            Object.Destroy(e);
                        boolSettingObject.name = $"Button Toggle {keybind.modGuid} {name}";
                        boolSettingObject.GetComponent<MenuKeybindToggle>().inputKey = keybind.inputKey;
                        MenuTwoOptions menuTwoOptions = boolSettingObject.GetComponent<MenuTwoOptions>();
                        menuTwoOptions.option2Text = keybind.toggle.falseText;
                        menuTwoOptions.option1Text = keybind.toggle.trueText;
                        boolSettingObject.transform.SetParent(content);
                        boolSettingObject.transform.localPosition =
                            new Vector3(buttonBoolSettingPosition.x, currentPosition, buttonBoolSettingPosition.z);
                    }
                    if (i + 1 < registeredKeybinds.Count)
                    {
                        Keybind nextKeybind = registeredKeybinds[i + 1];
                        if (RequiresHeader(nextKeybind))
                            currentPosition -= _buttonBoolSettingHeaderOffset;
                        if (RequiresCategory(nextKeybind))
                            currentPosition -= _buttonBoolSettingCategoryOffset;
                    }
                }
                else
                {
                    currentPosition -= _boolSettingOffset;
                    if (applyHeaderOffset)
                        currentPosition -= _boolSettingHeaderOffset;
                    if (applyCategoryOffset)
                        currentPosition -= _boolSettingCategoryOffset;
                    GameObject categoryBoolSettingObject = Object.Instantiate(boolSetting);
                    foreach (LocalizationChangedEvent e in categoryBoolSettingObject.GetComponentsInChildren<LocalizationChangedEvent>())
                        Object.Destroy(e);
                    categoryBoolSettingObject.name = $"Toggle {keybind.modGuid} {keybind.category}";
                    categoryBoolSettingObject.GetComponent<MenuKeybindToggle>().inputKey = keybind.inputKey;
                    MenuTwoOptions menuTwoOptions = categoryBoolSettingObject.GetComponent<MenuTwoOptions>();
                    menuTwoOptions.option2Text = keybind.toggle.falseText;
                    menuTwoOptions.option1Text = keybind.toggle.trueText;
                    categoryBoolSettingObject.transform.SetParent(content);
                    categoryBoolSettingObject.transform.localPosition =
                        new Vector3(boolSettingPosition.x, currentPosition, boolSettingPosition.z);
                }
                menuScrollBox.heightPadding += 4;
            }
        }
    }
}