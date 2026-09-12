using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine;

namespace Abiogenesis3d
{
    [Serializable]
    public class MultiCameraEventsCameraInfo
    {
        public Camera cam;

        [Min(0)]
        [Tooltip("0 = cam.farClipPlane")]
        public float raycastDistance = 0;

        [HideInInspector] public int storedEventMask;
    }

    [ExecuteInEditMode]
    public class MultiCameraEvents : MonoBehaviour
    {
        public bool blockedByUI = true;

        [Header("To ignore a camera add MultiCameraEventsIgnore component to it.")]
        public bool autoDetectCameras = true;

        public MultiCameraEventsCameraInfo[] cameraInfos = new MultiCameraEventsCameraInfo[0];

        GameObject lastColliderGO;
        GameObject lastMouseDownColliderGO;

        const SendMessageOptions msgOpts = SendMessageOptions.DontRequireReceiver;

        public RaycastHit raycastHit;

        GameObject lastDragGO;
        Vector3 lastDragMousePosition;

        float lastHandleInits;
        float handleInitsEvery = 0.1f;

        void OnValidate()
        {
            lastHandleInits = 0;
        }

        void CheckForInstances()
        {
            var existingInstances = FindObjectsOfType<MultiCameraEvents>();
            if (existingInstances.Length > 1)
            {
                Debug.Log($"MultiCameraEvents: There should only be one active instance in the scene. Deactivating: {name}");
                enabled = false;
                return;
            }
        }

        void OnEnable()
        {
            CheckForInstances();
        }

        void OnDisable()
        {
            RestoreEventMask();
        }

        void RestoreEventMask()
        {
            foreach (var camInfo in cameraInfos)
            {
                if (!camInfo.cam) continue;
                camInfo.cam.eventMask = camInfo.storedEventMask;
            }
        }

        void HandleInits()
        {
            // TODO: randomize this to not create processing spikes
            if (Time.time - lastHandleInits > handleInitsEvery)
            {
                lastHandleInits = Time.time;
                if (autoDetectCameras) AutoDetectCameras();
                cameraInfos = cameraInfos.Where(c => c.cam).OrderBy(c => c.cam.depth).ToArray();
            }
        }

        Type GetIgnoredType()
        {
            return typeof(MultiCameraEventsIgnore);
        }

        void AutoDetectCameras()
        {
            var allCameras = FindObjectsOfType<Camera>();

            foreach(var cam in allCameras)
            {
                var ignoreTag = cam.GetComponent(GetIgnoredType());
                var camInfo = cameraInfos.FirstOrDefault(c => c.cam == cam);

                if (camInfo == null)
                {
                    if (ignoreTag == null)
                    {
                        camInfo = new MultiCameraEventsCameraInfo {cam = cam};
                        cameraInfos = cameraInfos.Concat(new[] {camInfo}).ToArray();
                    }
                }
                else
                {
                    if (ignoreTag != null)
                        cameraInfos = cameraInfos.Where(c => c.cam != cam).ToArray();
                }
            }
        }

        bool IsCamInfoDisabled(MultiCameraEventsCameraInfo camInfo)
        {
            return camInfo.cam == null || !camInfo.cam.gameObject.activeInHierarchy;
        }

        void Awake()
        {
            if (Application.isPlaying)
            {
                if (EventSystem.current == null)
                {
                    Debug.LogWarning("EventSystem not found. Disabling blockedByUI");
                    blockedByUI = false;
                    return;
                }
            }
        }

        void Update()
        {
            HandleInits();
            if (!Application.isPlaying) return;

            // NOTE: this disables all events which are incorrect so we can emit the correct ones manually
            foreach (var camInfo in cameraInfos)
            {
                if (!camInfo.cam) continue;

                if (camInfo.cam.eventMask != 0)
                    camInfo.storedEventMask = camInfo.cam.eventMask;

                camInfo.cam.eventMask = 0;
            }

            SynthesizeEvents();
        }

        // NOTE: the project has Active Input Handling set to "Input System Package", where the legacy
        // UnityEngine.Input class throws InvalidOperationException on every call.
        static Vector3 GetMousePosition()
        {
#if ABIOGENESIS3D_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
#else
            return Input.mousePosition;
#endif
        }

        static bool GetMouseButtonDown(int button)
        {
#if ABIOGENESIS3D_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return false;
            switch (button)
            {
                case 0: return mouse.leftButton.wasPressedThisFrame;
                case 1: return mouse.rightButton.wasPressedThisFrame;
                case 2: return mouse.middleButton.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetMouseButtonDown(button);
#endif
        }

        static bool GetMouseButtonUp(int button)
        {
#if ABIOGENESIS3D_INPUT_SYSTEM
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return false;
            switch (button)
            {
                case 0: return mouse.leftButton.wasReleasedThisFrame;
                case 1: return mouse.rightButton.wasReleasedThisFrame;
                case 2: return mouse.middleButton.wasReleasedThisFrame;
                default: return false;
            }
#else
            return Input.GetMouseButtonUp(button);
#endif
        }

        bool IsPointerOverUIObject()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = GetMousePosition();

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            results.RemoveAll(r => r.gameObject.GetComponent(GetIgnoredType()) != null);

            return results.Count > 0;
        }

        void SynthesizeEvents()
        {
            if (blockedByUI)
            {
                // var pointerOverGO = EventSystem.current?.IsPointerOverGameObject() ?? false;
                var pointerOverGO = IsPointerOverUIObject();
                if (pointerOverGO)
                {
                    if (lastColliderGO != null)
                        lastColliderGO.SendMessage("OnMouseExit", msgOpts);

                    lastColliderGO = null;
                    raycastHit = default;

                    return;
                }
            }

            raycastHit = default;
            // reverse cameras order, last camera is first to hit
            foreach (var camInfo in cameraInfos.Reverse())
            {
                if (IsCamInfoDisabled(camInfo)) continue;

                var ray = camInfo.cam.ScreenPointToRay(GetMousePosition());
                var raycastDistance = camInfo.raycastDistance != 0 ? camInfo.raycastDistance : camInfo.cam.farClipPlane;
                var didHit = Physics.Raycast(ray, out raycastHit, raycastDistance, camInfo.cam.cullingMask);

                if (!didHit) continue;

                // changing to a new target
                if (raycastHit.collider.gameObject != lastColliderGO)
                {
                    // exiting previous target
                    if (lastColliderGO != null)
                        lastColliderGO.SendMessage("OnMouseExit", msgOpts);

                    // entering new target
                    raycastHit.collider.SendMessage("OnMouseEnter", msgOpts);
                    lastColliderGO = raycastHit.collider.gameObject;
                }
                // staying on the same target
                else raycastHit.collider.SendMessage("OnMouseOver", msgOpts);

                // clicks
                for (var i = 0; i < 3; i++)
                {
                    if (GetMouseButtonDown(i))
                    {
                        lastMouseDownColliderGO = raycastHit.collider.gameObject;
                        raycastHit.collider.SendMessage("OnMouseDown", msgOpts);

                        lastDragGO = lastMouseDownColliderGO;
                        lastDragMousePosition = GetMousePosition();
                    }
                    else if (GetMouseButtonUp(i))
                    {
                        if (lastMouseDownColliderGO == raycastHit.collider.gameObject)
                            raycastHit.collider.SendMessage("OnMouseUpAsButton", msgOpts);
                        raycastHit.collider.SendMessage("OnMouseUp", msgOpts);

                        lastDragGO = null;
                    }
                }

                if (didHit) break;
            }

            // drag
            if (lastDragGO)
            {
                var mouseDelta = GetMousePosition() - lastDragMousePosition;
                if (mouseDelta != Vector3.zero)
                {
                    lastDragGO.SendMessage("OnMouseDrag", mouseDelta, msgOpts);
                    lastDragMousePosition = GetMousePosition();
                }
            }
        }
    }
}
