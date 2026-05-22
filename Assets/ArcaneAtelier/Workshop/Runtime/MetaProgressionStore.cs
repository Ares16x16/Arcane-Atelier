using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class MetaProgressionSaveData
    {
        public int SaveVersion = 1;
        public int SealedCycles;
        public int LegacySigils;
        public int TotalLegacySigilsEarned;
        public int BestTokensEarnedInRun;
        public string LastOutcome = string.Empty;
        public List<string> PurchasedUpgradeIds = new List<string>();
    }

    public sealed class LegacyArchiveUpgrade
    {
        public LegacyArchiveUpgrade(string id, string displayName, string description, int sigilCost)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            SigilCost = Mathf.Max(0, sigilCost);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int SigilCost { get; }
    }

    public static class MetaProgressionStore
    {
        public const string KindledStartId = "legacy.kindled_start";
        public const string WardenReserveId = "legacy.warden_reserve";
        public const string EmberFloatId = "legacy.ember_float";

        private const int SaveVersion = 1;
        private const int FinalBossClearSigilReward = 3;
        private const int KindledStartPrepTicks = 40;
        private const int WardenReserveShield = 8;
        private const int EmberFloatTokens = 40;
        private const string SaveFileName = "arcane_atelier_save.json";

        private static readonly LegacyArchiveUpgrade[] Upgrades =
        {
            new LegacyArchiveUpgrade(
                KindledStartId,
                "Kindled Start",
                "Each breach preparation opens with +40 workshop ticks.",
                1),
            new LegacyArchiveUpgrade(
                WardenReserveId,
                "Warden Reserve",
                "Every battle begins with +8 opening shield.",
                2),
            new LegacyArchiveUpgrade(
                EmberFloatId,
                "Ember Float",
                "Each new run starts with 40 run Tokens for the Atelier Exchange.",
                2)
        };

        private static MetaProgressionSaveData currentSave;
        private static bool loaded;

        public static IReadOnlyList<LegacyArchiveUpgrade> AvailableUpgrades => Upgrades;

        public static MetaProgressionSaveData CurrentSave
        {
            get
            {
                EnsureLoaded();
                return currentSave;
            }
        }

        public static bool HasSaveFile => File.Exists(SavePath);
        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
        public static int SealedCycles => CurrentSave.SealedCycles;
        public static int LegacySigils => CurrentSave.LegacySigils;
        public static int BestTokensEarnedInRun => CurrentSave.BestTokensEarnedInRun;
        public static string LastOutcome => CurrentSave.LastOutcome;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadBeforeScene()
        {
            Load();
        }

        public static void EnsureLoaded()
        {
            if (!loaded)
            {
                Load();
            }
        }

        public static void Load()
        {
            loaded = true;
            currentSave = ReadSaveFromDisk();
        }

        public static void StartNewSave()
        {
            currentSave = new MetaProgressionSaveData
            {
                SaveVersion = SaveVersion,
                LastOutcome = "First breach pending"
            };
            loaded = true;
            Save();
        }

        public static void Save()
        {
            EnsureDirectoryExists();
            string json = JsonUtility.ToJson(CurrentSave, true);
            File.WriteAllText(SavePath, json);
        }

        public static bool HasUpgrade(string upgradeId)
        {
            return !string.IsNullOrWhiteSpace(upgradeId) &&
                   CurrentSave.PurchasedUpgradeIds.Contains(upgradeId);
        }

        public static bool TryPurchaseUpgrade(string upgradeId, out string message)
        {
            LegacyArchiveUpgrade upgrade = Upgrades.FirstOrDefault(item => item.Id == upgradeId);
            if (upgrade == null)
            {
                message = "Archive entry not found.";
                return false;
            }

            if (HasUpgrade(upgradeId))
            {
                message = $"{upgrade.DisplayName} is already carved into the archive.";
                return false;
            }

            if (CurrentSave.LegacySigils < upgrade.SigilCost)
            {
                message = $"Need {upgrade.SigilCost} Legacy Sigils.";
                return false;
            }

            CurrentSave.LegacySigils -= upgrade.SigilCost;
            CurrentSave.PurchasedUpgradeIds.Add(upgradeId);
            CurrentSave.PurchasedUpgradeIds.Sort(StringComparer.Ordinal);
            CurrentSave.LastOutcome = $"Carved {upgrade.DisplayName}.";
            Save();
            message = $"Carved {upgrade.DisplayName}.";
            return true;
        }

        public static int RecordFinalBossClear(int runTokensEarned)
        {
            int reward = FinalBossClearSigilReward;
            CurrentSave.SealedCycles += 1;
            CurrentSave.LegacySigils += reward;
            CurrentSave.TotalLegacySigilsEarned += reward;
            CurrentSave.BestTokensEarnedInRun = Mathf.Max(CurrentSave.BestTokensEarnedInRun, Mathf.Max(0, runTokensEarned));
            CurrentSave.LastOutcome = $"+{reward} Legacy Sigils from the sealed breach.";
            Save();
            return reward;
        }

        public static int GetPreparationTickBonus()
        {
            return HasUpgrade(KindledStartId) ? KindledStartPrepTicks : 0;
        }

        public static int GetOpeningShieldBonus()
        {
            return HasUpgrade(WardenReserveId) ? WardenReserveShield : 0;
        }

        public static int GetStartingRunTokens()
        {
            return HasUpgrade(EmberFloatId) ? EmberFloatTokens : 0;
        }

        public static string BuildActiveBonusSummary()
        {
            List<string> bonuses = new List<string>();
            if (HasUpgrade(KindledStartId))
            {
                bonuses.Add($"+{KindledStartPrepTicks} prep ticks");
            }

            if (HasUpgrade(WardenReserveId))
            {
                bonuses.Add($"+{WardenReserveShield} opening shield");
            }

            if (HasUpgrade(EmberFloatId))
            {
                bonuses.Add($"+{EmberFloatTokens} run Tokens");
            }

            return bonuses.Count > 0
                ? $"Legacy Sigils: {string.Join("  /  ", bonuses)}"
                : "Legacy Sigils: no active carvings";
        }

        private static MetaProgressionSaveData ReadSaveFromDisk()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    return CreateEmptySave();
                }

                string json = File.ReadAllText(SavePath);
                MetaProgressionSaveData save = JsonUtility.FromJson<MetaProgressionSaveData>(json);
                return NormalizeSave(save);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"MetaProgressionStore: failed to load save. Starting fresh. {exception.Message}");
                return CreateEmptySave();
            }
        }

        private static MetaProgressionSaveData NormalizeSave(MetaProgressionSaveData save)
        {
            if (save == null)
            {
                return CreateEmptySave();
            }

            save.SaveVersion = SaveVersion;
            save.SealedCycles = Mathf.Max(0, save.SealedCycles);
            save.LegacySigils = Mathf.Max(0, save.LegacySigils);
            save.TotalLegacySigilsEarned = Mathf.Max(save.LegacySigils, save.TotalLegacySigilsEarned);
            save.BestTokensEarnedInRun = Mathf.Max(0, save.BestTokensEarnedInRun);
            save.LastOutcome = save.LastOutcome ?? string.Empty;
            save.PurchasedUpgradeIds = save.PurchasedUpgradeIds == null
                ? new List<string>()
                : save.PurchasedUpgradeIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(id => id, StringComparer.Ordinal)
                    .ToList();
            return save;
        }

        private static MetaProgressionSaveData CreateEmptySave()
        {
            return new MetaProgressionSaveData
            {
                SaveVersion = SaveVersion,
                LastOutcome = "No breach sealed yet"
            };
        }

        private static void EnsureDirectoryExists()
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
