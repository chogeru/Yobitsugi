using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.Player
{
    /// <summary>
    /// Shakes the active exploration camera on <see cref="GameEvents.OnScreenShakeRequested"/>.
    /// Self-attaches at boot like AudioService's design intent — nothing needs to reference or place this in the scene.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindAnyObjectByType<CameraShake>() != null) return;

            var host = new GameObject("[CameraShake]");
            host.AddComponent<CameraShake>();
            DontDestroyOnLoad(host);
        }

        private void OnEnable() => GameEvents.OnScreenShakeRequested += HandleShakeRequested;
        private void OnDisable() => GameEvents.OnScreenShakeRequested -= HandleShakeRequested;

        private void HandleShakeRequested(float duration, float strength)
        {
            var cam = Camera.main;
            if (cam == null) return;

            cam.transform.DOKill();
            cam.transform.DOShakePosition(duration, strength * 0.02f, 20, 90f, false, true).SetLink(cam.gameObject);
        }
    }
}
