// Auto Generate Code.
using System.Collections.Generic;

namespace DdaIT.AddressableKey
{
    public static class AddressableKeys
    {
        public static class InGame
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioSource", "Assets/GameResource/Prefab/InGame/AudioSource.prefab" },
                { "Boot", "Assets/GameResource/Prefab/InGame/Boot.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Sounds
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "AudioMixer", "Assets/GameResource/Audio/Mixer/AudioMixer.mixer" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class Images
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "CharacterSlotOutLine", "Assets/GameResource/Images/GameUI/Borders/CharacterSlotOutLine.png" },
                { "UICircleIconSmall", "Assets/GameResource/Images/GameUI/Borders/UICircleIconSmall.png" },
                { "UICircleIconSmallFillMask", "Assets/GameResource/Images/GameUI/Borders/UICircleIconSmallFillMask.png" },
                { "UIPanelIcon", "Assets/GameResource/Images/GameUI/Borders/UIPanelIcon.png" },
                { "UIPanelIcon2", "Assets/GameResource/Images/GameUI/Borders/UIPanelIcon2.png" },
                { "UIPanelIconFillMask", "Assets/GameResource/Images/GameUI/Borders/UIPanelIconFillMask.png" },
                { "UIPanelIconFillMask2", "Assets/GameResource/Images/GameUI/Borders/UIPanelIconFillMask2.png" },
                { "UISquareLine", "Assets/GameResource/Images/GameUI/Borders/UISquareLine.png" },
                { "UISquareLine2", "Assets/GameResource/Images/GameUI/Borders/UISquareLine2.png" },
                { "UISquareLineMask", "Assets/GameResource/Images/GameUI/Borders/UISquareLineMask.png" },
                { "UISquareLineMask2", "Assets/GameResource/Images/GameUI/Borders/UISquareLineMask2.png" },
                { "Square", "Assets/GameResource/Images/GameUI/Diagram/Square.png" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

        public static class UI
        {
            private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>()
            {
                { "TitlePanel", "Assets/GameResource/Prefab/UI/TitlePanel.prefab" },
                { "UIBlocker", "Assets/GameResource/Prefab/UI/UIBlocker.prefab" },
                { "UIRoot", "Assets/GameResource/Prefab/UI/UIRoot.prefab" },
            };

            public static string Get<T>() => Keys.TryGetValue(typeof(T).Name, out var key) ? key : null;
            public static string Get(string keyName) => Keys.TryGetValue(keyName, out var key) ? key : null;
        }

    }
}
