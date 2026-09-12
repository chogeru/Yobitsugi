using System;
using System.Text;
using Yobitsugi.Core;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.UI
{
    /// <summary>
    /// System menu logic (auto/skip, backlog, save & load, restart).
    /// Plain C#: depends only on the view contract and the systems' interfaces, never on MonoBehaviour.
    /// </summary>
    public class SystemMenuPresenter : IDisposable
    {
        private const int BacklogVisibleEntries = 20;

        /// <summary>Marks the most recent line so the log reads as "you are here", not just a wall of past dialogue.</summary>
        private const string LatestEntryColor = "#FFD97A";

        private readonly ISystemMenuView view;
        private readonly IGameModeController mode;
        private readonly VNPresenter vn;
        private readonly SaveCoordinator saves;
        private readonly Action restartAction;

        private bool isSaveMode;

        public SystemMenuPresenter(ISystemMenuView view, IGameModeController mode, VNPresenter vn,
            SaveCoordinator saves, Action restartAction)
        {
            this.view = view;
            this.mode = mode;
            this.vn = vn;
            this.saves = saves;
            this.restartAction = restartAction;

            view.MenuToggleRequested += ToggleMenu;
            view.AutoToggleRequested += ToggleAuto;
            view.SkipToggleRequested += ToggleSkip;
            view.BacklogRequested += OpenBacklog;
            view.SaveRequested += OpenSaveSlots;
            view.LoadRequested += OpenLoadSlots;
            view.RestartRequested += Restart;
            view.BackRequested += GoBack;
            view.SlotSelected += SelectSlot;
            view.AutoSlotSelected += SelectAutoSlot;
            view.BacklogEntryClicked += ReplayBacklogVoice;

            view.ShowPanel(SystemMenuPanel.None);
        }

        public void Dispose()
        {
            view.MenuToggleRequested -= ToggleMenu;
            view.AutoToggleRequested -= ToggleAuto;
            view.SkipToggleRequested -= ToggleSkip;
            view.BacklogRequested -= OpenBacklog;
            view.SaveRequested -= OpenSaveSlots;
            view.LoadRequested -= OpenLoadSlots;
            view.RestartRequested -= Restart;
            view.BackRequested -= GoBack;
            view.SlotSelected -= SelectSlot;
            view.AutoSlotSelected -= SelectAutoSlot;
            view.BacklogEntryClicked -= ReplayBacklogVoice;
        }

        public void ToggleMenu()
        {
            if (view.IsPanelVisible(SystemMenuPanel.Menu))
            {
                CloseAll();
                return;
            }

            mode.PauseForMenu();
            view.SetToggleLabels(vn.AutoMode, vn.SkipMode);
            view.ShowPanel(SystemMenuPanel.Menu);
        }

        /// <summary>Escape/cancel: step back one level, closing the menu entirely from the top level.</summary>
        public void GoBack()
        {
            if (view.IsPanelVisible(SystemMenuPanel.Backlog) || view.IsPanelVisible(SystemMenuPanel.Slots))
            {
                view.ShowPanel(SystemMenuPanel.Menu);
                return;
            }

            if (view.IsPanelVisible(SystemMenuPanel.Menu))
            {
                CloseAll();
                return;
            }

            ToggleMenu();
        }

        private void CloseAll()
        {
            view.ShowPanel(SystemMenuPanel.None);
            mode.ResumeFromMenu();
        }

        private void ToggleAuto()
        {
            vn.SetAuto(!vn.AutoMode);
            view.SetToggleLabels(vn.AutoMode, vn.SkipMode);
        }

        private void ToggleSkip()
        {
            vn.SetSkip(!vn.SkipMode);
            view.SetToggleLabels(vn.AutoMode, vn.SkipMode);
        }

        private void OpenBacklog()
        {
            view.SetBacklogText(BuildBacklogText());
            view.ShowPanel(SystemMenuPanel.Backlog);
        }

        private static string BuildBacklogText()
        {
            var entries = VNBacklog.Entries;
            if (entries.Count == 0) return "(まだ会話がありません)";

            var builder = new StringBuilder();
            int start = Math.Max(0, entries.Count - BacklogVisibleEntries);
            int lastIndex = entries.Count - 1;
            for (int i = start; i < entries.Count; i++)
            {
                bool voiced = entries[i].Voice != null;
                // Underline marks a line as clickable; the link id is this entry's index for ReplayBacklogVoice to look up.
                if (voiced) builder.Append($"<link=\"{i}\"><u>");
                if (i == lastIndex) builder.Append($"<color={LatestEntryColor}>");

                builder.Append(entries[i].DisplayText);

                if (i == lastIndex) builder.Append("</color>");
                if (voiced) builder.Append("</u></link>");

                if (i < lastIndex) builder.Append("\n\n");
            }

            return builder.ToString();
        }

        /// <summary>Reuses the normal voice pipeline (ducking included) so replaying a backlog line behaves like hearing it live.</summary>
        private void ReplayBacklogVoice(int index)
        {
            var entries = VNBacklog.Entries;
            if (index < 0 || index >= entries.Count) return;

            var clip = entries[index].Voice;
            if (clip != null) GameEvents.RaiseVoiceRequested(clip);
        }

        private void OpenSaveSlots()
        {
            isSaveMode = true;
            OpenSlots("セーブ");
        }

        private void OpenLoadSlots()
        {
            isSaveMode = false;
            OpenSlots("ロード");
        }

        private void OpenSlots(string title)
        {
            view.SetSlotsTitle(title);

            for (int i = 0; i < view.SlotCount; i++)
            {
                bool hasSave = SaveSystem.HasSave(i);
                string label = hasSave
                    ? $"スロット{i + 1}: {SaveSystem.Load(i)?.savedAtDisplay}"
                    : $"スロット{i + 1}: (空)";
                view.SetSlot(i, label, isSaveMode || hasSave);
            }

            // Autosave is never a manual save target — only ever offered when loading.
            bool hasAutoSave = SaveSystem.HasSave(SaveSystem.AutoSlot);
            string autoLabel = hasAutoSave
                ? $"オートセーブ: {SaveSystem.Load(SaveSystem.AutoSlot)?.savedAtDisplay}"
                : "オートセーブ: (空)";
            view.SetAutoSlot(autoLabel, hasAutoSave, visible: !isSaveMode);

            view.ShowPanel(SystemMenuPanel.Slots);
        }

        private void SelectSlot(int index)
        {
            if (isSaveMode)
            {
                saves.Save(index);
                OpenSlots("セーブ");
                return;
            }

            if (!saves.Load(index)) return;
            CloseAll();
        }

        private void SelectAutoSlot()
        {
            if (isSaveMode || !saves.Load(SaveSystem.AutoSlot)) return;
            CloseAll();
        }

        private void Restart() => restartAction?.Invoke();
    }
}
