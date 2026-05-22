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
        public int SaveVersion = 2;
        public int SealedCycles;
        public int LegacySigils;
        public int TotalLegacySigilsEarned;
        public int BestTokensEarnedInRun;
        public string LastOutcome = string.Empty;
        public List<string> PurchasedUpgradeIds = new List<string>();
        public List<LegacyArchivePurchaseData> PurchasedBoonInstances = new List<LegacyArchivePurchaseData>();
        public List<LegacyArchiveOfferSaveData> ActiveOfferRolls = new List<LegacyArchiveOfferSaveData>();
        public LegacyOmenSaveData ActiveOmen = new LegacyOmenSaveData();
    }

    [Serializable]
    public sealed class LegacyArchivePurchaseData
    {
        public string BoonId = string.Empty;
        public int Value;
        public int SigilCost;
    }

    [Serializable]
    public sealed class LegacyArchiveOfferSaveData
    {
        public string OfferId = string.Empty;
        public string BoonId = string.Empty;
        public int Value;
        public int SigilCost;
    }

    [Serializable]
    public sealed class LegacyOmenSaveData
    {
        public string OmenId = string.Empty;
        public string DisplayName = string.Empty;
        public string Description = string.Empty;
        public int EnemyHealthPercent;
        public int EnemyDamagePercent;
        public int VictoryTokenBonus;
    }

    public sealed class LegacyArchiveUpgrade
    {
        public LegacyArchiveUpgrade(
            string id,
            string boonId,
            string displayName,
            string categoryLabel,
            string description,
            int sigilCost,
            int rolledValue,
            int currentRank,
            int maxRank)
        {
            Id = id;
            BoonId = boonId;
            DisplayName = displayName;
            CategoryLabel = categoryLabel;
            Description = description;
            SigilCost = Mathf.Max(0, sigilCost);
            RolledValue = Mathf.Max(0, rolledValue);
            CurrentRank = Mathf.Max(0, currentRank);
            MaxRank = Mathf.Max(1, maxRank);
        }

        public string Id { get; }
        public string BoonId { get; }
        public string DisplayName { get; }
        public string CategoryLabel { get; }
        public string Description { get; }
        public int SigilCost { get; }
        public int RolledValue { get; }
        public int CurrentRank { get; }
        public int MaxRank { get; }
        public bool IsMaxed => CurrentRank >= MaxRank;
    }

    public sealed class LegacyOmenView
    {
        public LegacyOmenView(string displayName, string description, int enemyHealthPercent, int enemyDamagePercent, int victoryTokenBonus)
        {
            DisplayName = displayName;
            Description = description;
            EnemyHealthPercent = Mathf.Max(0, enemyHealthPercent);
            EnemyDamagePercent = Mathf.Max(0, enemyDamagePercent);
            VictoryTokenBonus = Mathf.Max(0, victoryTokenBonus);
        }

        public string DisplayName { get; }
        public string Description { get; }
        public int EnemyHealthPercent { get; }
        public int EnemyDamagePercent { get; }
        public int VictoryTokenBonus { get; }
    }

    public enum MetaHudIconKind
    {
        Prep = 0,
        Shield = 1,
        Tokens = 2,
        Vitality = 3,
        Healing = 4,
        Bounty = 5,
        BreachPressure = 6,
        Omen = 7
    }

    public static class MetaHudIconAtlas
    {
        private const string ResourcePath = "UI/legacy_meta_icons";
        private const int IconCount = 8;

        private static Texture2D cachedTexture;

        public static Texture2D GetTexture()
        {
            if (cachedTexture == null)
            {
                cachedTexture = Resources.Load<Texture2D>(ResourcePath);
            }

            return cachedTexture;
        }

        public static Rect GetUv(MetaHudIconKind kind)
        {
            Texture2D atlas = GetTexture();
            float width = 1f / IconCount;
            float x = (int)kind * width;
            if (atlas == null)
            {
                return new Rect(x, 0f, width, 1f);
            }

            float insetX = 0.5f / atlas.width;
            float insetY = 0.5f / atlas.height;
            return new Rect(
                x + insetX,
                insetY,
                Mathf.Max(0f, width - (insetX * 2f)),
                Mathf.Max(0f, 1f - (insetY * 2f)));
        }
    }

    public static class MetaProgressionStore
    {
        public const string KindledStartId = "legacy.kindled_start";
        public const string WardenReserveId = "legacy.warden_reserve";
        public const string EmberFloatId = "legacy.ember_float";
        public const string VitalScriptId = "legacy.vital_script";
        public const string AfterglowId = "legacy.afterglow";
        public const string BountySealId = "legacy.bounty_seal";

        private const int SaveVersion = 2;
        private const int FinalBossClearSigilReward = 3;
        private const int OfferSlotCount = 3;
        private const string SaveFileName = "arcane_atelier_save.json";

        private sealed class LegacyBoonDefinition
        {
            public LegacyBoonDefinition(string id, string displayName, string categoryLabel, int minRoll, int maxRoll, int baseCost, int maxStacks)
            {
                Id = id;
                DisplayName = displayName;
                CategoryLabel = categoryLabel;
                MinRoll = minRoll;
                MaxRoll = Mathf.Max(minRoll, maxRoll);
                BaseCost = Mathf.Max(1, baseCost);
                MaxStacks = Mathf.Max(1, maxStacks);
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string CategoryLabel { get; }
            public int MinRoll { get; }
            public int MaxRoll { get; }
            public int BaseCost { get; }
            public int MaxStacks { get; }
        }

        private sealed class LegacyOmenDefinition
        {
            public LegacyOmenDefinition(
                string id,
                string displayName,
                int minEnemyHealthPercent,
                int maxEnemyHealthPercent,
                int minEnemyDamagePercent,
                int maxEnemyDamagePercent,
                int minVictoryTokenBonus,
                int maxVictoryTokenBonus)
            {
                Id = id;
                DisplayName = displayName;
                MinEnemyHealthPercent = minEnemyHealthPercent;
                MaxEnemyHealthPercent = Mathf.Max(minEnemyHealthPercent, maxEnemyHealthPercent);
                MinEnemyDamagePercent = minEnemyDamagePercent;
                MaxEnemyDamagePercent = Mathf.Max(minEnemyDamagePercent, maxEnemyDamagePercent);
                MinVictoryTokenBonus = minVictoryTokenBonus;
                MaxVictoryTokenBonus = Mathf.Max(minVictoryTokenBonus, maxVictoryTokenBonus);
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int MinEnemyHealthPercent { get; }
            public int MaxEnemyHealthPercent { get; }
            public int MinEnemyDamagePercent { get; }
            public int MaxEnemyDamagePercent { get; }
            public int MinVictoryTokenBonus { get; }
            public int MaxVictoryTokenBonus { get; }
        }

        private static readonly LegacyBoonDefinition[] BoonDefinitions =
        {
            new LegacyBoonDefinition(KindledStartId, "Kindled Start", "Workshop", 26, 60, 1, 4),
            new LegacyBoonDefinition(WardenReserveId, "Warden Reserve", "Ward", 4, 11, 1, 4),
            new LegacyBoonDefinition(EmberFloatId, "Ember Float", "Currency", 18, 55, 1, 4),
            new LegacyBoonDefinition(VitalScriptId, "Vital Script", "Battle", 5, 12, 2, 3),
            new LegacyBoonDefinition(AfterglowId, "Afterglow Seal", "Recovery", 4, 9, 2, 3),
            new LegacyBoonDefinition(BountySealId, "Bounty Seal", "Spoils", 10, 28, 2, 3)
        };

        private static readonly LegacyOmenDefinition[] OmenDefinitions =
        {
            new LegacyOmenDefinition("omen.heavy_air", "Heavy Air", 12, 22, 2, 7, 8, 16),
            new LegacyOmenDefinition("omen.razor_choir", "Razor Choir", 4, 10, 10, 18, 12, 20),
            new LegacyOmenDefinition("omen.gilded_shards", "Gilded Shards", 10, 18, 7, 12, 14, 24),
            new LegacyOmenDefinition("omen.long_night", "Long Night", 16, 26, 4, 8, 18, 30)
        };

        private static MetaProgressionSaveData currentSave;
        private static bool loaded;

        public static IReadOnlyList<LegacyArchiveUpgrade> AvailableUpgrades
        {
            get
            {
                EnsureArchiveState();
                return CurrentSave.ActiveOfferRolls
                    .Select(BuildOfferView)
                    .Where(view => view != null)
                    .ToArray();
            }
        }

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
        public static LegacyOmenView ActiveOmen => BuildOmenView(CurrentSave.ActiveOmen);

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
            EnsureArchiveState();
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

        public static bool HasUpgrade(string boonId)
        {
            return GetPurchasedStackCount(boonId) > 0;
        }

        public static int GetPurchasedStackCount(string boonId)
        {
            if (string.IsNullOrWhiteSpace(boonId))
            {
                return 0;
            }

            return CurrentSave.PurchasedBoonInstances.Count(purchase => string.Equals(purchase.BoonId, boonId, StringComparison.Ordinal));
        }

        public static bool TryPurchaseUpgrade(string offerId, out string message)
        {
            EnsureArchiveState();
            LegacyArchiveOfferSaveData offer = CurrentSave.ActiveOfferRolls.FirstOrDefault(item => string.Equals(item.OfferId, offerId, StringComparison.Ordinal));
            if (offer == null)
            {
                message = "Archive entry not found.";
                return false;
            }

            LegacyBoonDefinition definition = FindBoonDefinition(offer.BoonId);
            if (definition == null)
            {
                message = "Archive entry is corrupted.";
                return false;
            }

            int currentRank = GetPurchasedStackCount(definition.Id);
            if (currentRank >= definition.MaxStacks)
            {
                message = $"{definition.DisplayName} is fully carved.";
                return false;
            }

            if (CurrentSave.LegacySigils < offer.SigilCost)
            {
                message = $"Need {offer.SigilCost} Legacy Sigils.";
                return false;
            }

            CurrentSave.LegacySigils -= offer.SigilCost;
            CurrentSave.PurchasedBoonInstances.Add(new LegacyArchivePurchaseData
            {
                BoonId = definition.Id,
                Value = offer.Value,
                SigilCost = offer.SigilCost
            });
            CurrentSave.LastOutcome = $"Carved {definition.DisplayName} (+{offer.ValueLabel(definition.Id)}).";

            int offerIndex = CurrentSave.ActiveOfferRolls.FindIndex(item => string.Equals(item.OfferId, offerId, StringComparison.Ordinal));
            if (offerIndex >= 0)
            {
                CurrentSave.ActiveOfferRolls.RemoveAt(offerIndex);
                LegacyArchiveOfferSaveData rerolledOffer = CreateOffer(CurrentSave.ActiveOfferRolls.Select(item => item.BoonId).ToArray());
                if (rerolledOffer != null)
                {
                    CurrentSave.ActiveOfferRolls.Insert(Mathf.Min(offerIndex, CurrentSave.ActiveOfferRolls.Count), rerolledOffer);
                }
            }

            Save();
            message = CurrentSave.LastOutcome;
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
            RollArchiveState();
            Save();
            return reward;
        }

        public static int GetPreparationTickBonus()
        {
            return GetPurchasedValueTotal(KindledStartId);
        }

        public static int GetOpeningShieldBonus()
        {
            return GetPurchasedValueTotal(WardenReserveId);
        }

        public static int GetStartingRunTokens()
        {
            return GetPurchasedValueTotal(EmberFloatId);
        }

        public static int GetPlayerMaxHealthBonus()
        {
            return GetPurchasedValueTotal(VitalScriptId);
        }

        public static int GetVictoryHealBonus()
        {
            return GetPurchasedValueTotal(AfterglowId);
        }

        public static int GetVictoryTokenBonus()
        {
            int boonBonus = GetPurchasedValueTotal(BountySealId);
            int omenBonus = ActiveOmen != null ? ActiveOmen.VictoryTokenBonus : 0;
            return boonBonus + omenBonus;
        }

        public static float GetEnemyHealthScaleMultiplier()
        {
            float cycleBonus = Mathf.Min(0.96f, SealedCycles * 0.12f);
            float omenBonus = ActiveOmen != null ? ActiveOmen.EnemyHealthPercent / 100f : 0f;
            return 1f + cycleBonus + omenBonus;
        }

        public static float GetEnemyDamageScaleMultiplier()
        {
            float cycleBonus = Mathf.Min(0.72f, SealedCycles * 0.08f);
            float omenBonus = ActiveOmen != null ? ActiveOmen.EnemyDamagePercent / 100f : 0f;
            return 1f + cycleBonus + omenBonus;
        }

        public static int GetEnemyStartingShieldBonus()
        {
            return Mathf.Max(0, SealedCycles * 2 - 2);
        }

        public static int ScaleEnemyMaxHealth(int baseHealth)
        {
            return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, baseHealth) * GetEnemyHealthScaleMultiplier()));
        }

        public static string BuildActiveBonusSummary()
        {
            List<string> fragments = new List<string>();
            AddBonusFragment(fragments, GetPreparationTickBonus(), "Prep");
            AddBonusFragment(fragments, GetOpeningShieldBonus(), "Ward");
            AddBonusFragment(fragments, GetStartingRunTokens(), "Start Tokens");
            AddBonusFragment(fragments, GetPlayerMaxHealthBonus(), "Vital");
            AddBonusFragment(fragments, GetVictoryHealBonus(), "Afterglow");
            AddBonusFragment(fragments, GetPurchasedValueTotal(BountySealId), "Bounty");

            return fragments.Count > 0
                ? $"Sigils: {string.Join("  /  ", fragments)}"
                : "Sigils: no active carvings";
        }

        public static string BuildBreachPressureSummary()
        {
            int healthPercent = Mathf.Max(0, Mathf.RoundToInt((GetEnemyHealthScaleMultiplier() - 1f) * 100f));
            int damagePercent = Mathf.Max(0, Mathf.RoundToInt((GetEnemyDamageScaleMultiplier() - 1f) * 100f));
            int ward = GetEnemyStartingShieldBonus();

            if (healthPercent <= 0 && damagePercent <= 0 && ward <= 0)
            {
                return "Pressure: the breach is quiet";
            }

            return $"Pressure: +{healthPercent}% HP  /  +{damagePercent}% DMG  /  +{ward} Ward";
        }

        public static string BuildCurrentOmenSummary()
        {
            LegacyOmenView omen = ActiveOmen;
            if (omen == null || string.IsNullOrWhiteSpace(omen.DisplayName))
            {
                return "Omen: none";
            }

            return $"Omen: {omen.DisplayName}  /  +{omen.VictoryTokenBonus} Tokens";
        }

        public static string BuildRunModifierSummary()
        {
            return $"{BuildBreachPressureSummary()}  //  {BuildCurrentOmenSummary()}";
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
            save.PurchasedBoonInstances = NormalizePurchases(save.PurchasedBoonInstances);
            if (save.PurchasedBoonInstances.Count == 0 && save.PurchasedUpgradeIds.Count > 0)
            {
                UpgradeLegacyPurchases(save);
            }

            save.ActiveOfferRolls = NormalizeOffers(save.ActiveOfferRolls);
            save.ActiveOmen = NormalizeOmen(save.ActiveOmen);
            return save;
        }

        private static List<LegacyArchivePurchaseData> NormalizePurchases(List<LegacyArchivePurchaseData> purchases)
        {
            List<LegacyArchivePurchaseData> normalized = new List<LegacyArchivePurchaseData>();
            if (purchases == null)
            {
                return normalized;
            }

            foreach (LegacyBoonDefinition definition in BoonDefinitions)
            {
                List<LegacyArchivePurchaseData> matching = purchases
                    .Where(item => item != null && string.Equals(item.BoonId, definition.Id, StringComparison.Ordinal))
                    .Take(definition.MaxStacks)
                    .Select(item => new LegacyArchivePurchaseData
                    {
                        BoonId = definition.Id,
                        Value = Mathf.Clamp(item.Value, definition.MinRoll, definition.MaxRoll),
                        SigilCost = Mathf.Max(1, item.SigilCost)
                    })
                    .ToList();
                normalized.AddRange(matching);
            }

            return normalized;
        }

        private static List<LegacyArchiveOfferSaveData> NormalizeOffers(List<LegacyArchiveOfferSaveData> offers)
        {
            List<LegacyArchiveOfferSaveData> normalized = new List<LegacyArchiveOfferSaveData>();
            if (offers == null)
            {
                return normalized;
            }

            foreach (LegacyArchiveOfferSaveData offer in offers)
            {
                LegacyBoonDefinition definition = FindBoonDefinition(offer != null ? offer.BoonId : string.Empty);
                if (offer == null || definition == null)
                {
                    continue;
                }

                normalized.Add(new LegacyArchiveOfferSaveData
                {
                    OfferId = string.IsNullOrWhiteSpace(offer.OfferId) ? Guid.NewGuid().ToString("N") : offer.OfferId,
                    BoonId = definition.Id,
                    Value = Mathf.Clamp(offer.Value, definition.MinRoll, definition.MaxRoll),
                    SigilCost = Mathf.Max(1, offer.SigilCost)
                });
            }

            return normalized;
        }

        private static LegacyOmenSaveData NormalizeOmen(LegacyOmenSaveData omen)
        {
            if (omen == null)
            {
                return new LegacyOmenSaveData();
            }

            omen.OmenId = omen.OmenId ?? string.Empty;
            omen.DisplayName = omen.DisplayName ?? string.Empty;
            omen.Description = omen.Description ?? string.Empty;
            omen.EnemyHealthPercent = Mathf.Max(0, omen.EnemyHealthPercent);
            omen.EnemyDamagePercent = Mathf.Max(0, omen.EnemyDamagePercent);
            omen.VictoryTokenBonus = Mathf.Max(0, omen.VictoryTokenBonus);
            return omen;
        }

        private static void UpgradeLegacyPurchases(MetaProgressionSaveData save)
        {
            if (save == null)
            {
                return;
            }

            if (save.PurchasedUpgradeIds.Contains(KindledStartId))
            {
                save.PurchasedBoonInstances.Add(new LegacyArchivePurchaseData { BoonId = KindledStartId, Value = 40, SigilCost = 1 });
            }

            if (save.PurchasedUpgradeIds.Contains(WardenReserveId))
            {
                save.PurchasedBoonInstances.Add(new LegacyArchivePurchaseData { BoonId = WardenReserveId, Value = 8, SigilCost = 2 });
            }

            if (save.PurchasedUpgradeIds.Contains(EmberFloatId))
            {
                save.PurchasedBoonInstances.Add(new LegacyArchivePurchaseData { BoonId = EmberFloatId, Value = 40, SigilCost = 2 });
            }

            save.PurchasedBoonInstances = NormalizePurchases(save.PurchasedBoonInstances);
        }

        private static MetaProgressionSaveData CreateEmptySave()
        {
            return new MetaProgressionSaveData
            {
                SaveVersion = SaveVersion,
                LastOutcome = "No breach sealed yet"
            };
        }

        private static void EnsureArchiveState()
        {
            if (CurrentSave.SealedCycles <= 0)
            {
                CurrentSave.ActiveOfferRolls = new List<LegacyArchiveOfferSaveData>();
                CurrentSave.ActiveOmen = new LegacyOmenSaveData();
                return;
            }

            bool dirty = false;
            if (CurrentSave.ActiveOfferRolls == null)
            {
                CurrentSave.ActiveOfferRolls = new List<LegacyArchiveOfferSaveData>();
                dirty = true;
            }

            CurrentSave.ActiveOfferRolls = CurrentSave.ActiveOfferRolls
                .Where(item => item != null && FindBoonDefinition(item.BoonId) != null && GetPurchasedStackCount(item.BoonId) < FindBoonDefinition(item.BoonId).MaxStacks)
                .Take(OfferSlotCount)
                .ToList();

            while (CurrentSave.ActiveOfferRolls.Count < OfferSlotCount)
            {
                LegacyArchiveOfferSaveData offer = CreateOffer(CurrentSave.ActiveOfferRolls.Select(item => item.BoonId).ToArray());
                if (offer == null)
                {
                    break;
                }

                CurrentSave.ActiveOfferRolls.Add(offer);
                dirty = true;
            }

            if (string.IsNullOrWhiteSpace(CurrentSave.ActiveOmen?.OmenId))
            {
                CurrentSave.ActiveOmen = CreateOmen();
                dirty = true;
            }

            if (dirty)
            {
                Save();
            }
        }

        private static void RollArchiveState()
        {
            CurrentSave.ActiveOfferRolls = new List<LegacyArchiveOfferSaveData>();
            for (int i = 0; i < OfferSlotCount; i++)
            {
                LegacyArchiveOfferSaveData offer = CreateOffer(CurrentSave.ActiveOfferRolls.Select(item => item.BoonId).ToArray());
                if (offer == null)
                {
                    break;
                }

                CurrentSave.ActiveOfferRolls.Add(offer);
            }

            CurrentSave.ActiveOmen = CreateOmen();
        }

        private static LegacyArchiveOfferSaveData CreateOffer(params string[] excludedBoonIds)
        {
            HashSet<string> exclusions = new HashSet<string>(excludedBoonIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            List<LegacyBoonDefinition> candidates = BoonDefinitions
                .Where(definition => GetPurchasedStackCount(definition.Id) < definition.MaxStacks && !exclusions.Contains(definition.Id))
                .ToList();
            if (candidates.Count == 0)
            {
                candidates = BoonDefinitions
                    .Where(definition => GetPurchasedStackCount(definition.Id) < definition.MaxStacks)
                    .ToList();
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            System.Random rng = BuildRng(candidates.Count + CurrentSave.SealedCycles + CurrentSave.TotalLegacySigilsEarned + CurrentSave.PurchasedBoonInstances.Count);
            LegacyBoonDefinition definition = candidates[rng.Next(candidates.Count)];
            int value = rng.Next(definition.MinRoll, definition.MaxRoll + 1);
            return new LegacyArchiveOfferSaveData
            {
                OfferId = Guid.NewGuid().ToString("N"),
                BoonId = definition.Id,
                Value = value,
                SigilCost = ComputeOfferCost(definition, value)
            };
        }

        private static LegacyOmenSaveData CreateOmen()
        {
            System.Random rng = BuildRng(CurrentSave.SealedCycles * 17 + CurrentSave.TotalLegacySigilsEarned * 3 + CurrentSave.PurchasedBoonInstances.Count);
            LegacyOmenDefinition definition = OmenDefinitions[rng.Next(OmenDefinitions.Length)];
            int healthPercent = rng.Next(definition.MinEnemyHealthPercent, definition.MaxEnemyHealthPercent + 1);
            int damagePercent = rng.Next(definition.MinEnemyDamagePercent, definition.MaxEnemyDamagePercent + 1);
            int victoryTokens = rng.Next(definition.MinVictoryTokenBonus, definition.MaxVictoryTokenBonus + 1);
            return new LegacyOmenSaveData
            {
                OmenId = definition.Id,
                DisplayName = definition.DisplayName,
                Description = BuildOmenDescription(definition.DisplayName, healthPercent, damagePercent, victoryTokens),
                EnemyHealthPercent = healthPercent,
                EnemyDamagePercent = damagePercent,
                VictoryTokenBonus = victoryTokens
            };
        }

        private static LegacyArchiveUpgrade BuildOfferView(LegacyArchiveOfferSaveData offer)
        {
            LegacyBoonDefinition definition = FindBoonDefinition(offer != null ? offer.BoonId : string.Empty);
            if (offer == null || definition == null)
            {
                return null;
            }

            return new LegacyArchiveUpgrade(
                offer.OfferId,
                definition.Id,
                definition.DisplayName,
                definition.CategoryLabel,
                BuildBoonDescription(definition.Id, offer.Value),
                offer.SigilCost,
                offer.Value,
                GetPurchasedStackCount(definition.Id),
                definition.MaxStacks);
        }

        private static LegacyOmenView BuildOmenView(LegacyOmenSaveData omen)
        {
            if (omen == null || string.IsNullOrWhiteSpace(omen.OmenId))
            {
                return null;
            }

            return new LegacyOmenView(
                omen.DisplayName,
                omen.Description,
                omen.EnemyHealthPercent,
                omen.EnemyDamagePercent,
                omen.VictoryTokenBonus);
        }

        private static LegacyBoonDefinition FindBoonDefinition(string boonId)
        {
            return BoonDefinitions.FirstOrDefault(item => string.Equals(item.Id, boonId, StringComparison.Ordinal));
        }

        private static int GetPurchasedValueTotal(string boonId)
        {
            if (string.IsNullOrWhiteSpace(boonId))
            {
                return 0;
            }

            return CurrentSave.PurchasedBoonInstances
                .Where(item => item != null && string.Equals(item.BoonId, boonId, StringComparison.Ordinal))
                .Sum(item => Mathf.Max(0, item.Value));
        }

        private static int ComputeOfferCost(LegacyBoonDefinition definition, int value)
        {
            int span = Mathf.Max(1, definition.MaxRoll - definition.MinRoll);
            float normalizedRoll = (value - definition.MinRoll) / (float)span;
            int cost = definition.BaseCost;
            if (normalizedRoll >= 0.45f)
            {
                cost += 1;
            }

            if (normalizedRoll >= 0.82f && definition.BaseCost >= 2)
            {
                cost += 1;
            }

            return cost;
        }

        private static string BuildBoonDescription(string boonId, int value)
        {
            switch (boonId)
            {
                case KindledStartId:
                    return $"+{value} workshop prep ticks at the start of every run.";
                case WardenReserveId:
                    return $"+{value} opening shield whenever a battle begins.";
                case EmberFloatId:
                    return $"+{value} run Tokens stocked before the workshop store opens.";
                case VitalScriptId:
                    return $"+{value} maximum health for every battle this cycle.";
                case AfterglowId:
                    return $"+{value} extra healing after each victory.";
                case BountySealId:
                    return $"+{value} Tokens added to every victorious payout.";
                default:
                    return $"+{value} archive strength.";
            }
        }

        private static string BuildOmenDescription(string displayName, int enemyHealthPercent, int enemyDamagePercent, int victoryTokenBonus)
        {
            return $"{displayName}: enemies gain +{enemyHealthPercent}% vitality and +{enemyDamagePercent}% force, but victories pay +{victoryTokenBonus} Tokens.";
        }

        private static void AddBonusFragment(List<string> fragments, int value, string label)
        {
            if (value > 0)
            {
                fragments.Add($"{label} +{value}");
            }
        }

        private static System.Random BuildRng(int salt)
        {
            int seed = Environment.TickCount;
            seed ^= CurrentSave.SealedCycles * 48611;
            seed ^= CurrentSave.LegacySigils * 92821;
            seed ^= salt * 3119;
            return new System.Random(seed);
        }

        private static string ValueLabel(this LegacyArchiveOfferSaveData offer, string boonId)
        {
            if (offer == null)
            {
                return "0";
            }

            switch (boonId)
            {
                case VitalScriptId:
                    return $"{offer.Value} HP";
                case WardenReserveId:
                    return $"{offer.Value} ward";
                case EmberFloatId:
                case BountySealId:
                    return $"{offer.Value} Tokens";
                case KindledStartId:
                    return $"{offer.Value} ticks";
                case AfterglowId:
                    return $"{offer.Value} heal";
                default:
                    return offer.Value.ToString();
            }
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
