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

        int SlotCount { get; }

        void ShowPanel(SystemMenuPanel panel);
        bool IsPanelVisible(SystemMenuPanel panel);
        void SetToggleLabels(bool autoOn, bool skipOn);
        void SetBacklogText(string text);
        void SetSlotsTitle(string title);
        void SetSlot(int index, string label, bool interactable);
    }
}
