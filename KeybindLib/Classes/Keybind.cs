
using UnityEngine.InputSystem;

namespace KeybindLib.Classes
{
    public class Keybind
    {
        public class Toggle
        {
            public bool defaultValue;
            public string falseText = "hold";
            public string trueText = "toggle";
        }

        public string modGuid { get; internal set; }
        public string category { get; internal set; }
        public string tag { get; internal set; }
        public Toggle toggle { get; internal set; }
        public InputKey inputKey { get; internal set; }
        public InputAction inputAction { get; internal set; }

        internal bool registeredAsToggle;
        internal int modOrderIndex;
        internal int registerIndex;
    }
}
