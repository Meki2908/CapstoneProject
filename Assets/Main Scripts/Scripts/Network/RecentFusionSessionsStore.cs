using System;
using System.Collections.Generic;
using UnityEngine;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Local-only history of Fusion session names (and optional passwords) for quick rejoin / re-host.
    /// Stored as JSON in PlayerPrefs; passwords are not encrypted.
    /// </summary>
    [Serializable]
    public class RecentFusionSessionEntry
    {
        public string roomName = "";
        public string password = "";
        public bool wasHost;
        public long lastUsedUtcTicks;
        public int hostPlayerCount = 4;
        public bool hostIsPrivate;
    }

    [Serializable]
    class RecentFusionSessionsJson
    {
        public RecentFusionSessionEntry[] entries = Array.Empty<RecentFusionSessionEntry>();
    }

    public static class RecentFusionSessionsStore
    {
        const string PrefsKey = "RecentFusionSessions_v1";
        const int MaxEntries = 10;

        public static int Count => GetOrdered().Count;

        public static IReadOnlyList<RecentFusionSessionEntry> GetOrdered()
        {
            var list = LoadList();
            list.Sort((a, b) => b.lastUsedUtcTicks.CompareTo(a.lastUsedUtcTicks));
            return list;
        }

        public static void AddOrUpdate(RecentFusionSessionEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.roomName)) return;

            entry.roomName = entry.roomName.Trim();
            entry.password ??= "";
            entry.lastUsedUtcTicks = DateTime.UtcNow.Ticks;

            var list = LoadList();
            int idx = list.FindIndex(e =>
                string.Equals(e.roomName, entry.roomName, StringComparison.Ordinal) &&
                e.wasHost == entry.wasHost);

            if (idx >= 0)
            {
                list[idx] = entry;
            }
            else
            {
                list.Add(entry);
            }

            list.Sort((a, b) => b.lastUsedUtcTicks.CompareTo(a.lastUsedUtcTicks));
            while (list.Count > MaxEntries)
                list.RemoveAt(list.Count - 1);

            SaveList(list);
        }

        public static void Remove(string roomName, bool wasHost)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return;
            roomName = roomName.Trim();
            var list = LoadList();
            list.RemoveAll(e =>
                string.Equals(e.roomName, roomName, StringComparison.Ordinal) && e.wasHost == wasHost);
            SaveList(list);
        }

        /// <summary>Clears locally stored password for a session you hosted (re-host will use no password unless you enter a new one).</summary>
        public static bool ClearStoredPasswordForHost(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return false;
            roomName = roomName.Trim();
            var list = LoadList();
            bool changed = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].wasHost && string.Equals(list[i].roomName, roomName, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(list[i].password))
                        changed = true;
                    list[i].password = "";
                }
            }
            if (changed)
                SaveList(list);
            return changed;
        }

        static List<RecentFusionSessionEntry> LoadList()
        {
            try
            {
                string json = PlayerPrefs.GetString(PrefsKey, "");
                if (string.IsNullOrEmpty(json))
                    return new List<RecentFusionSessionEntry>();

                var wrap = JsonUtility.FromJson<RecentFusionSessionsJson>(json);
                if (wrap?.entries == null || wrap.entries.Length == 0)
                    return new List<RecentFusionSessionEntry>();

                return new List<RecentFusionSessionEntry>(wrap.entries);
            }
            catch
            {
                return new List<RecentFusionSessionEntry>();
            }
        }

        static void SaveList(List<RecentFusionSessionEntry> list)
        {
            var wrap = new RecentFusionSessionsJson { entries = list.ToArray() };
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(wrap));
            PlayerPrefs.Save();
        }
    }
}
