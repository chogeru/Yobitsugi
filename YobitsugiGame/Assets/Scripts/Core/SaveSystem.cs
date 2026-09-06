using System.IO;
using UnityEngine;

namespace Yobitsugi.Core
{
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        private static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

        public static bool HasSave(int slot) => File.Exists(PathFor(slot));

        public static void Save(int slot, SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(PathFor(slot), json);
        }

        public static SaveData Load(int slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<SaveData>(json);
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
