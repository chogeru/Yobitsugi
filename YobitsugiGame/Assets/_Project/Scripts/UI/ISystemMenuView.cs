using System;

namespace Yobitsugi.UI
{
    public enum SystemMenuPanel { None, Menu, Backlog, Slots }

    /// <summary>MVP view contract for the system menu. Renders state and reports intent; no logic.</summary>
    public interface ISystemMenuView
    {
        event Action MenuToggleRequested;
        event Action AutoToggleRequested;
        event Action SkipToggleRequested;
        event Action BacklogRequested;
        event Action SaveRequested;
        event Action LoadRequested;
        event Action RestartRequested;
        event Action BackRequested;
        event Action<int> SlotSelected;
        event Action AutoSlotSelected;

        /// <summary>A voiced backlog line was clicked; the payload is that entry's index into VNBacklog.Entries.</summary>
        event Action<int> BacklogEntryClicked;

        int SlotCount { get; }

        void ShowPanel(SystemMenuPanel panel);
        bool IsPanelVisible(SystemMenuPanel panel);
        void SetToggleLabels(bool autoOn, bool skipOn);
        void SetBacklogText(string text);
        void SetSlotsTitle(string title);
        void SetSlot(int index, string label, bool interactable);

        /// <summary>The reserved autosave slot: shown only when loading (never a manual save target).</summary>
        void SetAutoSlot(string label, bool interactable, bool visible);
    }
}
