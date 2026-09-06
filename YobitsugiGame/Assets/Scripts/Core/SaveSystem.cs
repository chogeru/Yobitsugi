using System.IO;
using UnityEngine;

namespace Yobitsugi.Core
{
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        private static string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

        public static bool HasSave(int slot) => File.Exists(PathFor(slot));

        public static bool Save(int slot, SaveData data)
        {
            data.version = SaveData.CurrentVersion;

            string path = PathFor(slot);
            string tempPath = path + ".tmp";

            try
            {
                // Write to a temp file first, then swap: a crash mid-write cannot corrupt an existing save.
                File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));

                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
                return true;
            }
            catch (IOException e)
            {
                Debug.LogError($"セーブに失敗しました (スロット{slot}): {e.Message}");
                return false;
            }
        }

        public static SaveData Load(int slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return null;

            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null)
                {
                    Debug.LogError($"セーブデータを読み込めませんでした (スロット{slot}): 内容が不正です。");
                    return null;
                }

                if (data.version > SaveData.CurrentVersion)
                {
                    Debug.LogError($"セーブデータ (スロット{slot}) はより新しいバージョン {data.version} で保存されています。");
                    return null;
                }

                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"セーブデータが壊れています (スロット{slot}): {e.Message}");
                return null;
            }
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
