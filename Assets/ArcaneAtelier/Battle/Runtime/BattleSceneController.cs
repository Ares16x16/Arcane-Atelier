using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleSceneController : MonoBehaviour
    {
        [SerializeField] private BattleContentDatabase contentDatabase;
        [SerializeField] private string startingBossId = "boss.earth.golem";
        [SerializeField] private int playerMaxHealth = 100;

        public BattleUnit Player { get; private set; }
        public BattleUnit Boss { get; private set; }
        public BattleBossDefinition CurrentBossDefinition { get; private set; }

        private void Awake()
        {
            InitializePlayer();
            LoadWorkshopPayload();
            InitializeBoss();
        }

        private void InitializePlayer()
        {
            Player = new BattleUnit
            {
                DisplayName = "Player",
                MaxHealth = playerMaxHealth,
                CurrentHealth = playerMaxHealth,
                Shield = 0,
                Element = WorkshopElementAttribute.None
            };
        }

        private void LoadWorkshopPayload()
        {
            if (WorkshopBattlePayloadBridge.TryConsume(out WorkshopBattlePayload payload))
            {
                Debug.Log($"BattleScene: loaded {payload.Cards.Count} card type(s) from workshop.");
                foreach (WorkshopBattleCardEntry card in payload.Cards)
                {
                    Debug.Log($"  - {card.DisplayName} x{card.Amount} ({card.Role}, {card.Element})");
                }
            }
            else
            {
                Debug.LogWarning("BattleScene: no workshop payload found. Running with empty deck.");
            }
        }

        private void InitializeBoss()
        {
            if (contentDatabase == null)
            {
                Debug.LogError("BattleSceneController: contentDatabase is not assigned.");
                return;
            }

            CurrentBossDefinition = contentDatabase.FindBoss(startingBossId);
            if (CurrentBossDefinition == null)
            {
                Debug.LogError($"BattleSceneController: boss '{startingBossId}' not found in database.");
                return;
            }

            Boss = new BattleUnit
            {
                DisplayName = CurrentBossDefinition.DisplayName,
                MaxHealth = CurrentBossDefinition.MaxHealth,
                CurrentHealth = CurrentBossDefinition.MaxHealth,
                Shield = 0,
                Element = CurrentBossDefinition.Element
            };

            Debug.Log($"BattleScene: Boss '{Boss.DisplayName}' initialized with {Boss.MaxHealth} HP.");
        }
    }
}
