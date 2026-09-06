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
            for (int i = start; i < entries.Count; i++)
            {
                builder.Append(entries[i]);
                if (i < entries.Count - 1) builder.Append("\n\n");
            }

            return builder.ToString();
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

        private void Restart() => restartAction?.Invoke();
    }
}
