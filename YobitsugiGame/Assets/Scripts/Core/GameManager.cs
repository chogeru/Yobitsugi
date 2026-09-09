using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yobitsugi.Interactables;

namespace Yobitsugi.Core
{
    public class GameManager : MonoBehaviour, ISaveParticipant
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { Playing, Cleared }

        [SerializeField] private int requiredClueCount = 3;

        public GameState State { get; private set; } = GameState.Playing;
        public int ClueCount { get; private set; }
        public int RequiredClueCount => requiredClueCount;
        public int SaveOrder => 0;

        private readonly HashSet<string> collectedClueIds = new HashSet<string>();

        public event Action<int, int> OnClueCountChanged;
        public event Action OnGameCleared;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool CollectClue(string clueId)
        {
            if (State != GameState.Playing || string.IsNullOrEmpty(clueId) || !collectedClueIds.Add(clueId))
                return false;

            ClueCount++;
            OnClueCountChanged?.Invoke(ClueCount, requiredClueCount);
            GameEvents.RaiseClueCollected(clueId);
            return true;
        }

        public bool HasEnoughClues(int required) => ClueCount >= required;

        // --- ISaveParticipant ---

        public void Capture(SaveData data)
        {
            var ids = new string[collectedClueIds.Count];
            collectedClueIds.CopyTo(ids);
            data.collectedClueIds = ids;
        }

        public void Restore(SaveData data)
        {
            collectedClueIds.Clear();
            ClueCount = 0;
            State = GameState.Playing;

            if (data.collectedClueIds != null)
            {
                foreach (var id in data.collectedClueIds)
                {
                    if (!string.IsNullOrEmpty(id) && collectedClueIds.Add(id))
                        ClueCount++;
                }
            }

            RestoreClueObjects();
            OnClueCountChanged?.Invoke(ClueCount, requiredClueCount);
        }

        /// <summary>Hides clues already collected in the loaded save, and re-shows any that were not.</summary>
        private void RestoreClueObjects()
        {
            var clues = FindObjectsByType<ClueItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var clue in clues)
                clue.gameObject.SetActive(!collectedClueIds.Contains(clue.Id));
        }

        public void ClearGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Cleared;
            OnGameCleared?.Invoke();
            GameEvents.RaiseGameCleared();
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
