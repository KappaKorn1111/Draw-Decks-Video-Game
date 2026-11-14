using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DrawDecks.Client.Gameplay
{
    [Serializable]
    public sealed class LocalDeckProfile
    {
        public string id;
        public string name;
        public int schemaVersion = 1;
        public string[] cardIds = Array.Empty<string>();
        public long updatedUtcTicks;
    }

    [Serializable]
    public sealed class LocalRulesProfile
    {
        public string id;
        public string name;
        public int schemaVersion = 1;
        public int gameMode;
        public int targetScore = 100;
        public int startingHandSize = 5;
        public int maximumHandSize = 7;
        public int maximumPlayers = 4;
        public int startingHealth = 20;
        public int damagePerHit = 5;
        public int survivalMaximumWaves;
        public int survivalScorePerCpu = 10;
        public bool allowStacking;
        public long updatedUtcTicks;
    }

    [Serializable]
    internal sealed class LocalDeckProfileCollection { public LocalDeckProfile[] items = Array.Empty<LocalDeckProfile>(); }

    [Serializable]
    internal sealed class LocalRulesProfileCollection { public LocalRulesProfile[] items = Array.Empty<LocalRulesProfile>(); }

    /// <summary>
    /// Versioned device-local profile persistence. Writes a recovery copy before replacing
    /// the active payload and validates JSON/schema before exposing profiles to the UI.
    /// </summary>
    public static class LocalCustomizationStore
    {
        private const string DecksKey = "DrawDecks.Customization.Decks.v1";
        private const string DecksBackupKey = DecksKey + ".backup";
        private const string RulesKey = "DrawDecks.Customization.Rules.v1";
        private const string RulesBackupKey = RulesKey + ".backup";

        public static List<LocalDeckProfile> LoadDecks()
        {
            return LoadCollection<LocalDeckProfileCollection, LocalDeckProfile>(DecksKey, DecksBackupKey)
                .Where(IsValid).ToList();
        }

        public static List<LocalRulesProfile> LoadRules()
        {
            return LoadCollection<LocalRulesProfileCollection, LocalRulesProfile>(RulesKey, RulesBackupKey)
                .Where(IsValid).ToList();
        }

        public static void SaveDeck(LocalDeckProfile profile)
        {
            if (!IsValid(profile)) throw new ArgumentException("Deck profile is invalid.", nameof(profile));
            var items = LoadDecks();
            items.RemoveAll(item => item.id == profile.id);
            profile.updatedUtcTicks = DateTime.UtcNow.Ticks;
            items.Add(profile);
            SaveCollection(DecksKey, DecksBackupKey, new LocalDeckProfileCollection { items = items.ToArray() });
        }

        public static void SaveRules(LocalRulesProfile profile)
        {
            if (!IsValid(profile)) throw new ArgumentException("Rules profile is invalid.", nameof(profile));
            var items = LoadRules();
            items.RemoveAll(item => item.id == profile.id);
            profile.updatedUtcTicks = DateTime.UtcNow.Ticks;
            items.Add(profile);
            SaveCollection(RulesKey, RulesBackupKey, new LocalRulesProfileCollection { items = items.ToArray() });
        }

        public static void DeleteDeck(string id)
        {
            var items = LoadDecks();
            items.RemoveAll(item => item.id == id);
            SaveCollection(DecksKey, DecksBackupKey, new LocalDeckProfileCollection { items = items.ToArray() });
        }

        public static void DeleteRules(string id)
        {
            var items = LoadRules();
            items.RemoveAll(item => item.id == id);
            SaveCollection(RulesKey, RulesBackupKey, new LocalRulesProfileCollection { items = items.ToArray() });
        }

        private static bool IsValid(LocalDeckProfile item)
        {
            return item != null && item.schemaVersion == 1 && !string.IsNullOrWhiteSpace(item.id) &&
                !string.IsNullOrWhiteSpace(item.name) && item.cardIds != null && item.cardIds.Length > 0 &&
                item.cardIds.Length <= 120;
        }

        private static bool IsValid(LocalRulesProfile item)
        {
            return item != null && item.schemaVersion == 1 && !string.IsNullOrWhiteSpace(item.id) &&
                !string.IsNullOrWhiteSpace(item.name) && item.targetScore >= 10 && item.targetScore <= 1000 &&
                item.startingHandSize >= 1 && item.startingHandSize <= 20 &&
                item.maximumHandSize >= item.startingHandSize && item.maximumHandSize <= 40 &&
                item.maximumPlayers >= 2 && item.maximumPlayers <= 4 &&
                item.startingHealth >= 1 && item.startingHealth <= 500 &&
                item.damagePerHit >= 1 && item.damagePerHit <= 100 &&
                item.survivalMaximumWaves >= 0 && item.survivalMaximumWaves <= 1000 &&
                item.survivalScorePerCpu >= 0 && item.survivalScorePerCpu <= 1000;
        }

        private static List<T> LoadCollection<TCollection, T>(string key, string backupKey)
            where TCollection : class
        {
            string json = PlayerPrefs.GetString(key, string.Empty);
            if (TryParse<TCollection>(json, out var current))
                return Extract<TCollection, T>(current);
            string backup = PlayerPrefs.GetString(backupKey, string.Empty);
            if (TryParse<TCollection>(backup, out var recovered))
            {
                PlayerPrefs.SetString(key, backup);
                PlayerPrefs.Save();
                return Extract<TCollection, T>(recovered);
            }
            return new List<T>();
        }

        private static bool TryParse<T>(string json, out T value) where T : class
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { value = JsonUtility.FromJson<T>(json); return value != null; }
            catch (Exception) { return false; }
        }

        private static List<T> Extract<TCollection, T>(TCollection collection) where TCollection : class
        {
            if (collection is LocalDeckProfileCollection decks)
                return (decks.items ?? Array.Empty<LocalDeckProfile>()).Cast<T>().ToList();
            if (collection is LocalRulesProfileCollection rules)
                return (rules.items ?? Array.Empty<LocalRulesProfile>()).Cast<T>().ToList();
            return new List<T>();
        }

        private static void SaveCollection<T>(string key, string backupKey, T collection)
        {
            string current = PlayerPrefs.GetString(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(current))
                PlayerPrefs.SetString(backupKey, current);
            string next = JsonUtility.ToJson(collection);
            if (string.IsNullOrWhiteSpace(next))
                throw new InvalidOperationException("Profile serialization returned an empty payload.");
            PlayerPrefs.SetString(key, next);
            PlayerPrefs.Save();
        }
    }
}
