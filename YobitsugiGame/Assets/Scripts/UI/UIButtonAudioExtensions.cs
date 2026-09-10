using UnityEngine;
using UnityEngine.UI;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    /// <summary>Wires the shared UI click sound and hover/press feedback onto buttons without every screen touching AudioService directly.</summary>
    public static class UIButtonAudioExtensions
    {
        /// <summary>Adds the click sound and hover/press feedback to every Button under this component (including itself).</summary>
        public static void WireButtonClickSounds(this Component root)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                button.WireButtonClickSound();
        }

        /// <summary>Adds the click sound and hover/press feedback to a single button, e.g. one instantiated at runtime.</summary>
        public static void WireButtonClickSound(this Button button)
        {
            button.onClick.AddListener(GameEvents.RaiseUIButtonClicked);
            if (button.GetComponent<UIButtonFeedback>() == null)
                button.gameObject.AddComponent<UIButtonFeedback>();
        }
    }
}
