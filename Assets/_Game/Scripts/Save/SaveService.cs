using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SuperBot.Save
{
    /// <summary>
    /// Offline JSON save in persistentDataPath. Writes go to a temp file first, the previous save is kept
    /// as .bak, and the payload carries a SHA-256 hash so corrupted/edited files fall back to the backup.
    /// </summary>
    public sealed class SaveService
    {
        private const string FileName = "save.json";
        private const string Salt = "SuperBot::v1::7f3c";

        private readonly string path;
        private readonly string tmpPath;
        private readonly string bakPath;

        public SaveData Data { get; private set; } = new SaveData();
        public bool IsDirty { get; private set; }

        public SaveService()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            tmpPath = path + ".tmp";
            bakPath = path + ".bak";
        }

        public void MarkDirty() => IsDirty = true;

        public void Load()
        {
            if (TryRead(path, out var data) || TryRead(bakPath, out data))
            {
                Data = data;
            }
            else
            {
                if (File.Exists(path)) Debug.LogWarning("[Save] Save file invalid and no valid backup; starting fresh.");
                Data = new SaveData();
            }
            Data.EnsureValid();
            IsDirty = false;
        }

        public void Save()
        {
            try
            {
                string payload = JsonUtility.ToJson(Data);
                var envelope = new SaveEnvelope { payload = payload, hash = ComputeHash(payload) };
                File.WriteAllText(tmpPath, JsonUtility.ToJson(envelope), Encoding.UTF8);

                if (File.Exists(path))
                {
                    File.Copy(path, bakPath, true);
                    File.Delete(path);
                }
                File.Move(tmpPath, path);
                IsDirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Write failed: {e.Message}");
            }
        }

        public void DeleteAll()
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(bakPath)) File.Delete(bakPath);
                if (File.Exists(tmpPath)) File.Delete(tmpPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Delete failed: {e.Message}");
            }
            Data = new SaveData();
            IsDirty = false;
        }

        private static bool TryRead(string filePath, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(filePath)) return false;
                var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(filePath, Encoding.UTF8));
                if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return false;
                if (!string.Equals(envelope.hash, ComputeHash(envelope.payload), StringComparison.Ordinal))
                {
                    Debug.LogWarning($"[Save] Hash mismatch in {Path.GetFileName(filePath)}");
                    return false;
                }
                data = JsonUtility.FromJson<SaveData>(envelope.payload);
                return data != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Read failed ({Path.GetFileName(filePath)}): {e.Message}");
                return false;
            }
        }

        private static string ComputeHash(string payload)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload + Salt));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}
