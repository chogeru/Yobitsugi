using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    /// <summary>
    /// Shows/hides a target object based on the current game mode.
    /// Lives on an always-active object (never on the target itself, or it could not switch itself back on).
    /// </summary>
    public class ModeVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool visibleInVN;
        [SerializeField] private bool visibleInExploration = true;

        private void OnEnable() => GameEvents.OnModeChanged += Apply;
        private void OnDisable() => GameEvents.OnModeChanged -= Apply;

        private void Apply(bool isInVN)
        {
            if (target != null)
                target.SetActive(isInVN ? visibleInVN : visibleInExploration);
        }
    }
}
