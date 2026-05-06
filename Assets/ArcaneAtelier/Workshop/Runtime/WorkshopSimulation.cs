using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopNodeState
    {
        private const string ElementConduitId = "node.factory.conduit";
        private const string TurningConduitId = "node.factory.turn_conduit";
        private const string TurningConduitMirrorId = "node.factory.turn_conduit.mirror";
        private const string SpellConduitId = "node.factory.spell_conduit";
        private const string TurningSpellConduitId = "node.factory.turn_spell_conduit";
        private const string TurningSpellConduitMirrorId = "node.factory.turn_spell_conduit.mirror";
        private const string DeckCollectorId = "node.factory.deck_collector";

        private readonly Dictionary<WorkshopItemDefinition, int> buffer = new Dictionary<WorkshopItemDefinition, int>();
        private int bufferedItemCount;
        private float activityPulseSecondsRemaining;
        private NodePortMask inputPorts;
        private NodePortMask outputPorts;

        public WorkshopNodeState(WorkshopNodeDefinition definition, Vector2Int position, int rotationQuarterTurns)
        {
            Definition = definition;
            Position = position;
            RotationQuarterTurns = ((rotationQuarterTurns % 4) + 4) % 4;
            SpeedMultiplier = 1f;
            inputPorts = WorkshopDirectionUtility.Rotate(Definition.InputPorts, RotationQuarterTurns);
            outputPorts = WorkshopDirectionUtility.Rotate(Definition.OutputPorts, RotationQuarterTurns);
        }

        public WorkshopNodeDefinition Definition { get; }
        public Vector2Int Position { get; }
        public int RotationQuarterTurns { get; private set; }
        public float SpeedMultiplier { get; private set; }
        public float CycleProgress { get; set; }
        public IReadOnlyDictionary<WorkshopItemDefinition, int> Buffer => buffer;

        public NodePortMask RotatedInputPorts => inputPorts;
        public NodePortMask RotatedOutputPorts => outputPorts;
        public int BufferedItemCount => bufferedItemCount;
        public bool IsRecentlyActive => activityPulseSecondsRemaining > 0f;
        public bool HasEditablePorts => Definition != null && !string.IsNullOrWhiteSpace(Definition.Id) && Definition.Id.StartsWith("node.factory.spell_fusion.", StringComparison.Ordinal);

        public void RotateClockwise()
        {
            RotationQuarterTurns = (RotationQuarterTurns + 1) % 4;
            inputPorts = WorkshopDirectionUtility.Rotate(inputPorts, 1);
            outputPorts = WorkshopDirectionUtility.Rotate(outputPorts, 1);
        }

        public void ApplyEfficiencyBonus(float bonus)
        {
            SpeedMultiplier = Mathf.Max(0.1f, SpeedMultiplier + bonus);
        }

        public void AdvanceActivity(float deltaTime)
        {
            activityPulseSecondsRemaining = Mathf.Max(0f, activityPulseSecondsRemaining - Mathf.Max(0f, deltaTime));
        }

        public void MarkActive(float durationSeconds = 0.45f)
        {
            activityPulseSecondsRemaining = Mathf.Max(activityPulseSecondsRemaining, durationSeconds);
        }

        public string CycleEditablePort(NodePortMask direction)
        {
            if (!HasEditablePorts || !WorkshopDirectionUtility.IsCardinalDirection(direction))
            {
                return "Ports on this node cannot be edited.";
            }

            if ((inputPorts & direction) != 0)
            {
                inputPorts &= ~direction;
                outputPorts = NodePortMask.None;
                outputPorts |= direction;
                return $"Set {direction} as output.";
            }

            if ((outputPorts & direction) != 0)
            {
                outputPorts &= ~direction;
                return $"Cleared {direction} port.";
            }

            if (CountPorts(inputPorts) < 2)
            {
                inputPorts |= direction;
                return $"Set {direction} as input.";
            }

            if (outputPorts == NodePortMask.None)
            {
                outputPorts = direction;
                return $"Set {direction} as output.";
            }

            return "Spell fusion can only have two inputs and one output.";
        }

        private static int CountPorts(NodePortMask mask)
        {
            var count = 0;
            foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
            {
                if ((mask & direction) != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public bool CanAccept(WorkshopItemDefinition item)
        {
            if (item == null || BufferedItemCount >= Definition.BufferCapacity)
            {
                return false;
            }

            if (Definition.Category == WorkshopNodeCategory.Storage)
            {
                return StorageAcceptsItem(Definition, item);
            }

            if (Definition.AcceptsAnyResource)
            {
                return item.Kind == WorkshopItemKind.Resource;
            }

            return Definition.Recipes
                .Where(recipe => recipe != null)
                .SelectMany(recipe => recipe.Inputs ?? Array.Empty<WorkshopItemStack>())
                .Any(stack => stack != null && stack.Item == item);
        }

        internal static bool StorageAcceptsItem(WorkshopNodeDefinition definition, WorkshopItemDefinition item)
        {
            if (definition == null || item == null)
            {
                return false;
            }

            if (definition.Id == ElementConduitId || definition.Id == TurningConduitId || definition.Id == TurningConduitMirrorId)
            {
                return item.Kind == WorkshopItemKind.Resource;
            }

            if (definition.Id == SpellConduitId || definition.Id == TurningSpellConduitId || definition.Id == TurningSpellConduitMirrorId || definition.Id == DeckCollectorId)
            {
                return item.Kind == WorkshopItemKind.Card;
            }

            return true;
        }

        public bool TryAddToBuffer(WorkshopItemDefinition item, int amount)
        {
            if (item == null || amount <= 0 || bufferedItemCount + amount > Definition.BufferCapacity)
            {
                return false;
            }

            if (!buffer.TryAdd(item, amount))
            {
                buffer[item] += amount;
            }

            bufferedItemCount += amount;
            return true;
        }

        public int RemoveOne(WorkshopItemDefinition item)
        {
            if (item == null || !buffer.TryGetValue(item, out var amount) || amount <= 0)
            {
                return 0;
            }

            if (amount == 1)
            {
                buffer.Remove(item);
            }
            else
            {
                buffer[item] = amount - 1;
            }

            bufferedItemCount--;
            return 1;
        }

        public int CountItem(WorkshopItemDefinition item)
        {
            return item != null && buffer.TryGetValue(item, out var amount) ? amount : 0;
        }

        /// <summary>
        /// Returns buffer entries in an unspecified order. Use in hot simulation paths to avoid LINQ sort overhead.
        /// </summary>
        public IEnumerable<KeyValuePair<WorkshopItemDefinition, int>> EnumerateBufferUnsorted()
        {
            return buffer;
        }

        /// <summary>
        /// Returns buffer entries sorted by display name. Use only for UI/inspector views.
        /// </summary>
        public IEnumerable<KeyValuePair<WorkshopItemDefinition, int>> EnumerateBuffer()
        {
            return buffer.OrderBy(pair => pair.Key.DisplayName);
        }

        public WorkshopRunNodeSnapshot CaptureRunState()
        {
            WorkshopRunNodeSnapshot snapshot = new WorkshopRunNodeSnapshot
            {
                NodeId = Definition != null ? Definition.Id : string.Empty,
                Position = Position,
                RotationQuarterTurns = RotationQuarterTurns,
                SpeedMultiplier = SpeedMultiplier,
                CycleProgress = CycleProgress,
                InputPortsMask = (int)inputPorts,
                OutputPortsMask = (int)outputPorts
            };

            foreach (KeyValuePair<WorkshopItemDefinition, int> pair in buffer)
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                snapshot.Buffer.Add(new WorkshopRunItemStackSnapshot
                {
                    ItemId = pair.Key.Id,
                    Amount = pair.Value
                });
            }

            return snapshot;
        }

        public void RestoreRunState(WorkshopRunNodeSnapshot snapshot, IReadOnlyDictionary<string, WorkshopItemDefinition> itemLookup)
        {
            if (snapshot == null)
            {
                return;
            }

            SpeedMultiplier = Mathf.Max(0.1f, snapshot.SpeedMultiplier);
            CycleProgress = Mathf.Max(0f, snapshot.CycleProgress);
            inputPorts = (NodePortMask)snapshot.InputPortsMask;
            outputPorts = (NodePortMask)snapshot.OutputPortsMask;
            buffer.Clear();
            bufferedItemCount = 0;
            activityPulseSecondsRemaining = 0f;

            if (itemLookup == null)
            {
                return;
            }

            foreach (WorkshopRunItemStackSnapshot itemSnapshot in snapshot.Buffer)
            {
                if (itemSnapshot == null || string.IsNullOrWhiteSpace(itemSnapshot.ItemId) || itemSnapshot.Amount <= 0)
                {
                    continue;
                }

                if (!itemLookup.TryGetValue(itemSnapshot.ItemId, out WorkshopItemDefinition itemDefinition) || itemDefinition == null)
                {
                    continue;
                }

                TryAddToBuffer(itemDefinition, itemSnapshot.Amount);
            }
        }
    }

    public readonly struct PlacementResult
    {
        public PlacementResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }
    }

    public readonly struct WorkshopInventoryView
    {
        public WorkshopInventoryView(
            IReadOnlyDictionary<WorkshopItemDefinition, int> networkItems,
            IReadOnlyDictionary<WorkshopItemDefinition, int> preparedCards)
        {
            NetworkItems = networkItems;
            PreparedCards = preparedCards;
        }

        public IReadOnlyDictionary<WorkshopItemDefinition, int> NetworkItems { get; }
        public IReadOnlyDictionary<WorkshopItemDefinition, int> PreparedCards { get; }
    }

    public readonly struct WorkshopFlowStatsView
    {
        public WorkshopFlowStatsView(float elapsedSeconds, float elementProductionPerSecond, float elementConsumptionPerSecond, float spellProductionPerSecond)
        {
            ElapsedSeconds = elapsedSeconds;
            ElementProductionPerSecond = elementProductionPerSecond;
            ElementConsumptionPerSecond = elementConsumptionPerSecond;
            SpellProductionPerSecond = spellProductionPerSecond;
        }

        public float ElapsedSeconds { get; }
        public float ElementProductionPerSecond { get; }
        public float ElementConsumptionPerSecond { get; }
        public float SpellProductionPerSecond { get; }
    }

    public sealed class WorkshopSimulation
    {
        private static readonly ProfilerMarker StepMarker = new ProfilerMarker("ArcaneAtelier.Workshop.Step");
        private static readonly ProfilerMarker TransferMarker = new ProfilerMarker("ArcaneAtelier.Workshop.TransferBufferedItems");
        private static readonly ProfilerMarker RecipeMarker = new ProfilerMarker("ArcaneAtelier.Workshop.ExecuteRecipe");

        private readonly Dictionary<Vector2Int, WorkshopNodeState> nodes = new Dictionary<Vector2Int, WorkshopNodeState>();
        private readonly Dictionary<WorkshopItemDefinition, int> preparedCards = new Dictionary<WorkshopItemDefinition, int>();
        private readonly Dictionary<WorkshopItemDefinition, int> reserveItems = new Dictionary<WorkshopItemDefinition, int>();
        private readonly HashSet<string> unlockedNodeIds = new HashSet<string>();
        private readonly Vector2Int gridSize;
        private readonly List<WorkshopNodeState> stepNodeIterationCache = new List<WorkshopNodeState>();
        private readonly List<WorkshopNodeState> transferNodeIterationCache = new List<WorkshopNodeState>();
        private readonly List<KeyValuePair<WorkshopItemDefinition, int>> transferBufferCache = new List<KeyValuePair<WorkshopItemDefinition, int>>();
        private float simulatedSeconds;
        private int totalElementProduced;
        private int totalElementConsumed;
        private int totalSpellsProduced;
        private int notificationSuppressionDepth;
        private bool notificationQueued;

        public WorkshopSimulation(WorkshopContentDatabase contentDatabase)
        {
            ContentDatabase = contentDatabase;
            gridSize = contentDatabase.GridSize;

            foreach (var node in (contentDatabase.PlaceableNodes ?? Array.Empty<WorkshopNodeDefinition>()).Where(node => node != null && node.UnlockedByDefault))
            {
                unlockedNodeIds.Add(node.Id);
            }

            foreach (var seed in (contentDatabase.DefaultLayout ?? Array.Empty<WorkshopPlacedNodeSeed>()).Where(seed => seed != null && seed.NodeDefinition != null))
            {
                PlaceNodeInternal(seed.Position, seed.NodeDefinition, seed.RotationQuarterTurns);
            }
        }

        public event Action StateChanged;

        public WorkshopContentDatabase ContentDatabase { get; }
        public Vector2Int GridSize => gridSize;
        public IReadOnlyDictionary<Vector2Int, WorkshopNodeState> Nodes => nodes;
        public IReadOnlyDictionary<WorkshopItemDefinition, int> PreparedCards => preparedCards;

        public bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < gridSize.x && cell.y < gridSize.y;
        }

        public bool IsUnlocked(WorkshopNodeDefinition node)
        {
            return node != null && unlockedNodeIds.Contains(node.Id);
        }

        public PlacementResult PlaceNode(Vector2Int cell, WorkshopNodeDefinition definition, int rotationQuarterTurns, bool bypassUnlock = false)
        {
            if (definition == null)
            {
                return new PlacementResult(false, "No node selected.");
            }

            if (!IsInsideGrid(cell))
            {
                return new PlacementResult(false, "Outside placement bounds.");
            }

            if (!bypassUnlock && !IsUnlocked(definition))
            {
                return new PlacementResult(false, $"{definition.DisplayName} is still locked.");
            }

            PlaceNodeInternal(cell, definition, rotationQuarterTurns);
            RaiseStateChanged();
            return new PlacementResult(true, $"Placed {definition.DisplayName}.");
        }

        public PlacementResult RemoveNode(Vector2Int cell)
        {
            if (!nodes.Remove(cell))
            {
                return new PlacementResult(false, "No node on that cell.");
            }

            RaiseStateChanged();
            return new PlacementResult(true, "Removed node.");
        }

        public bool TryGetNode(Vector2Int cell, out WorkshopNodeState nodeState)
        {
            return nodes.TryGetValue(cell, out nodeState);
        }

        public void RotateNodeClockwise(Vector2Int cell)
        {
            if (!nodes.TryGetValue(cell, out var nodeState))
            {
                return;
            }

            nodeState.RotateClockwise();
            RaiseStateChanged();
        }

        public PlacementResult CycleNodePort(Vector2Int cell, NodePortMask direction)
        {
            if (!nodes.TryGetValue(cell, out var nodeState))
            {
                return new PlacementResult(false, "No node on that cell.");
            }

            string message = nodeState.CycleEditablePort(direction);
            RaiseStateChanged();
            return new PlacementResult(true, message);
        }

        public void UnlockNode(WorkshopNodeDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            unlockedNodeIds.Add(definition.Id);
            RaiseStateChanged();
        }

        public void ApplyReward(WorkshopRewardDefinition reward)
        {
            if (reward == null)
            {
                return;
            }

            switch (reward.RewardKind)
            {
                case WorkshopRewardKind.UnlockNode:
                    UnlockNode(reward.TargetNode);
                    break;
                case WorkshopRewardKind.EfficiencyBoost:
                    foreach (var nodeState in nodes.Values.Where(nodeState =>
                                 reward.TargetNode == null || nodeState.Definition == reward.TargetNode))
                    {
                        nodeState.ApplyEfficiencyBonus(reward.EfficiencyBonus);
                    }

                    RaiseStateChanged();
                    break;
                case WorkshopRewardKind.GrantItems:
                    if (reward.GrantedItems == null)
                    {
                        break;
                    }

                    foreach (var stack in reward.GrantedItems.Where(stack => stack?.Item != null && stack.Amount > 0))
                    {
                        if (!reserveItems.TryAdd(stack.Item, stack.Amount))
                        {
                            reserveItems[stack.Item] += stack.Amount;
                        }
                    }

                    RaiseStateChanged();
                    break;
            }
        }

        public void ResetToDefaultLayout()
        {
            ResetToLayout(ContentDatabase.DefaultLayout);
        }

        public void ResetToLayout(IEnumerable<WorkshopPlacedNodeSeed> layout)
        {
            BeginNotificationBatch();
            try
            {
                ResetRuntimeState();

                foreach (var seed in (layout ?? Array.Empty<WorkshopPlacedNodeSeed>()).Where(seed => seed != null && seed.NodeDefinition != null))
                {
                    PlaceNodeInternal(seed.Position, seed.NodeDefinition, seed.RotationQuarterTurns);
                }
            }
            finally
            {
                EndNotificationBatch();
            }
        }

        private void ResetRuntimeState()
        {
            nodes.Clear();
            preparedCards.Clear();
            reserveItems.Clear();
            unlockedNodeIds.Clear();
            simulatedSeconds = 0f;
            totalElementProduced = 0;
            totalElementConsumed = 0;
            totalSpellsProduced = 0;

            foreach (var node in (ContentDatabase.PlaceableNodes ?? Array.Empty<WorkshopNodeDefinition>()).Where(node => node != null && node.UnlockedByDefault))
            {
                unlockedNodeIds.Add(node.Id);
            }
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            using (StepMarker.Auto())
            {
                var dirty = false;
                simulatedSeconds += deltaTime;
                CacheNodesForStep();

                foreach (var nodeState in stepNodeIterationCache)
                {
                    nodeState.AdvanceActivity(deltaTime);
                    nodeState.CycleProgress += deltaTime * nodeState.SpeedMultiplier;

                    // Cap progress so stalled nodes don't fire a burst of catch-up cycles
                    // when inputs finally become available.
                    var maxCycleSeconds = 0f;
                    foreach (var r in nodeState.Definition.Recipes)
                    {
                        if (r != null && r.CycleSeconds > maxCycleSeconds)
                        {
                            maxCycleSeconds = r.CycleSeconds;
                        }
                    }

                    if (maxCycleSeconds > 0f)
                    {
                        nodeState.CycleProgress = Mathf.Min(nodeState.CycleProgress, maxCycleSeconds);
                    }

                    while (TryFindExecutableRecipe(nodeState, out var recipe) &&
                           recipe != null &&
                           nodeState.CycleProgress >= recipe.CycleSeconds)
                    {
                        if (!TryExecuteRecipe(nodeState, recipe))
                        {
                            break;
                        }

                        nodeState.CycleProgress -= recipe.CycleSeconds;
                        dirty = true;
                    }
                }

                dirty |= TransferBufferedItems();
                dirty |= AutoCollectBufferedCards();

                if (dirty)
                {
                    RaiseStateChanged();
                }
            }
        }

        public WorkshopInventoryView BuildInventoryView()
        {
            var network = new Dictionary<WorkshopItemDefinition, int>(reserveItems);

            foreach (var nodeState in nodes.Values)
            {
                foreach (var pair in nodeState.EnumerateBufferUnsorted())
                {
                    if (pair.Key == null || pair.Key.Kind != WorkshopItemKind.Resource)
                    {
                        continue;
                    }

                    if (!network.TryAdd(pair.Key, pair.Value))
                    {
                        network[pair.Key] += pair.Value;
                    }
                }
            }

            return new WorkshopInventoryView(network, BuildPreparedCardSnapshot());
        }

        public WorkshopPlacedNodeSeed[] BuildCurrentLayout()
        {
            return nodes.Values
                .OrderBy(nodeState => nodeState.Position.y)
                .ThenBy(nodeState => nodeState.Position.x)
                .Select(nodeState => WorkshopPlacedNodeSeed.Create(nodeState.Definition, nodeState.Position, nodeState.RotationQuarterTurns))
                .ToArray();
        }

        public void CommitBattlePayload()
        {
            WorkshopBattlePayloadBridge.Commit(BuildPreparedCardSnapshot());
        }

        public WorkshopFlowStatsView BuildFlowStatsView()
        {
            var safeSeconds = Mathf.Max(0.01f, simulatedSeconds);
            return new WorkshopFlowStatsView(
                simulatedSeconds,
                totalElementProduced / safeSeconds,
                totalElementConsumed / safeSeconds,
                totalSpellsProduced / safeSeconds);
        }

        private Dictionary<WorkshopItemDefinition, int> BuildPreparedCardSnapshot()
        {
            var snapshot = new Dictionary<WorkshopItemDefinition, int>();

            foreach (var nodeState in nodes.Values)
            {
                if (!IsDeckCollector(nodeState))
                {
                    continue;
                }

                foreach (var pair in nodeState.EnumerateBufferUnsorted())
                {
                    if (pair.Key == null || pair.Key.Kind != WorkshopItemKind.Card || pair.Value <= 0)
                    {
                        continue;
                    }

                    if (HasConnectedTransferTarget(nodeState, pair.Key))
                    {
                        continue;
                    }

                    if (!snapshot.TryAdd(pair.Key, pair.Value))
                    {
                        snapshot[pair.Key] += pair.Value;
                    }
                }
            }

            return snapshot;
        }

        private static bool IsDeckCollector(WorkshopNodeState nodeState)
        {
            return nodeState != null &&
                   nodeState.Definition != null &&
                   nodeState.Definition.Id == "node.factory.deck_collector";
        }

        public WorkshopRunStateSnapshot CaptureRunState()
        {
            WorkshopRunStateSnapshot snapshot = new WorkshopRunStateSnapshot
            {
                SimulatedSeconds = simulatedSeconds,
                TotalElementProduced = totalElementProduced,
                TotalElementConsumed = totalElementConsumed,
                TotalSpellsProduced = totalSpellsProduced
            };

            foreach (string unlockedNodeId in unlockedNodeIds.OrderBy(id => id, StringComparer.Ordinal))
            {
                snapshot.UnlockedNodeIds.Add(unlockedNodeId);
            }

            foreach (KeyValuePair<WorkshopItemDefinition, int> pair in reserveItems.OrderBy(pair => pair.Key != null ? pair.Key.Id : string.Empty, StringComparer.Ordinal))
            {
                if (pair.Key == null || pair.Value <= 0)
                {
                    continue;
                }

                snapshot.ReserveItems.Add(new WorkshopRunItemStackSnapshot
                {
                    ItemId = pair.Key.Id,
                    Amount = pair.Value
                });
            }

            foreach (WorkshopNodeState nodeState in nodes.Values.OrderBy(nodeState => nodeState.Position.y).ThenBy(nodeState => nodeState.Position.x))
            {
                snapshot.Nodes.Add(nodeState.CaptureRunState());
            }

            return snapshot;
        }

        public void RestoreRunState(WorkshopRunStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            Dictionary<string, WorkshopNodeDefinition> nodeLookup = BuildNodeLookup();
            Dictionary<string, WorkshopItemDefinition> itemLookup = BuildItemLookup();

            BeginNotificationBatch();
            try
            {
                ResetRuntimeState();
                unlockedNodeIds.Clear();
                foreach (string unlockedNodeId in snapshot.UnlockedNodeIds.Where(id => !string.IsNullOrWhiteSpace(id)))
                {
                    unlockedNodeIds.Add(unlockedNodeId);
                }

                foreach (WorkshopRunNodeSnapshot nodeSnapshot in snapshot.Nodes)
                {
                    if (nodeSnapshot == null || string.IsNullOrWhiteSpace(nodeSnapshot.NodeId))
                    {
                        continue;
                    }

                    if (!nodeLookup.TryGetValue(nodeSnapshot.NodeId, out WorkshopNodeDefinition definition) || definition == null)
                    {
                        continue;
                    }

                    PlaceNodeInternal(nodeSnapshot.Position, definition, nodeSnapshot.RotationQuarterTurns);
                    if (nodes.TryGetValue(nodeSnapshot.Position, out WorkshopNodeState nodeState))
                    {
                        nodeState.RestoreRunState(nodeSnapshot, itemLookup);
                    }
                }

                foreach (WorkshopRunItemStackSnapshot itemSnapshot in snapshot.ReserveItems)
                {
                    if (itemSnapshot == null || string.IsNullOrWhiteSpace(itemSnapshot.ItemId) || itemSnapshot.Amount <= 0)
                    {
                        continue;
                    }

                    if (!itemLookup.TryGetValue(itemSnapshot.ItemId, out WorkshopItemDefinition itemDefinition) || itemDefinition == null)
                    {
                        continue;
                    }

                    reserveItems[itemDefinition] = itemSnapshot.Amount;
                }

                simulatedSeconds = Mathf.Max(0f, snapshot.SimulatedSeconds);
                totalElementProduced = Mathf.Max(0, snapshot.TotalElementProduced);
                totalElementConsumed = Mathf.Max(0, snapshot.TotalElementConsumed);
                totalSpellsProduced = Mathf.Max(0, snapshot.TotalSpellsProduced);
            }
            finally
            {
                EndNotificationBatch();
            }
        }

        private bool TryFindExecutableRecipe(WorkshopNodeState nodeState, out WorkshopProductionRecipe executableRecipe)
        {
            foreach (var recipe in nodeState.Definition.Recipes)
            {
                if (CanExecuteRecipe(nodeState, recipe))
                {
                    executableRecipe = recipe;
                    return true;
                }
            }

            executableRecipe = null;
            return false;
        }

        private bool CanExecuteRecipe(WorkshopNodeState nodeState, WorkshopProductionRecipe recipe)
        {
            if (nodeState == null || recipe == null || recipe.CycleSeconds <= 0f)
            {
                return false;
            }

            var bufferedInputAmount = 0;
            foreach (var input in recipe.Inputs ?? Array.Empty<WorkshopItemStack>())
            {
                if (input?.Item == null || input.Amount <= 0)
                {
                    return false;
                }

                var bufferedAmount = nodeState.CountItem(input.Item);
                var available = bufferedAmount;
                if (input.Item.Kind == WorkshopItemKind.Resource)
                {
                    available += GetReserveCount(input.Item);
                }

                if (available < input.Amount)
                {
                    return false;
                }

                bufferedInputAmount += Math.Min(input.Amount, bufferedAmount);
            }

            var bufferedOutputAmount = 0;
            foreach (var output in (recipe.Outputs ?? Array.Empty<WorkshopItemStack>()).Where(output => ShouldBufferOutput(nodeState, output)))
            {
                if (output?.Item == null || output.Amount <= 0)
                {
                    return false;
                }

                bufferedOutputAmount += output.Amount;
            }

            if (nodeState.BufferedItemCount - bufferedInputAmount + bufferedOutputAmount > nodeState.Definition.BufferCapacity)
            {
                return false;
            }

            return true;
        }

        private bool TryExecuteRecipe(WorkshopNodeState nodeState, WorkshopProductionRecipe recipe)
        {
            using (RecipeMarker.Auto())
            {
                if (!CanExecuteRecipe(nodeState, recipe))
                {
                    return false;
                }

                foreach (var input in recipe.Inputs ?? Array.Empty<WorkshopItemStack>())
                {
                    if (input?.Item == null || input.Amount <= 0)
                    {
                        return false;
                    }

                    var remaining = input.Amount;
                    remaining -= RemoveFromBuffer(nodeState, input.Item, remaining);
                    if (remaining > 0 && input.Item != null && input.Item.Kind == WorkshopItemKind.Resource)
                    {
                        ConsumeReserveItems(input.Item, remaining);
                    }

                    if (input.Item != null && input.Item.Kind == WorkshopItemKind.Resource)
                    {
                        totalElementConsumed += input.Amount;
                    }
                }

                foreach (var output in recipe.Outputs ?? Array.Empty<WorkshopItemStack>())
                {
                    if (output.Item == null || output.Amount <= 0)
                    {
                        continue;
                    }

                    if (output.Item.Kind == WorkshopItemKind.Card)
                    {
                        if (ShouldBufferCardOutput(nodeState))
                        {
                            nodeState.TryAddToBuffer(output.Item, output.Amount);
                        }
                        else
                        {
                            if (!preparedCards.TryAdd(output.Item, output.Amount))
                            {
                                preparedCards[output.Item] += output.Amount;
                            }
                        }

                        totalSpellsProduced += output.Amount;
                        continue;
                    }

                    nodeState.TryAddToBuffer(output.Item, output.Amount);
                    totalElementProduced += output.Amount;
                }

                nodeState.MarkActive();
                return true;
            }
        }

        private bool TransferBufferedItems()
        {
            using (TransferMarker.Auto())
            {
                var dirty = false;

                CacheNodesForTransfer();
                foreach (var nodeState in transferNodeIterationCache)
                {
                    if (nodeState.Definition.MaxTransferPerStep <= 0)
                    {
                        continue;
                    }

                    var transferredThisStep = 0;

                    foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
                    {
                        if (transferredThisStep >= nodeState.Definition.MaxTransferPerStep)
                        {
                            break;
                        }

                        if ((nodeState.RotatedOutputPorts & direction) == 0)
                        {
                            continue;
                        }

                        var targetCell = nodeState.Position + WorkshopDirectionUtility.ToOffset(direction);
                        if (!nodes.TryGetValue(targetCell, out var targetNode))
                        {
                            continue;
                        }

                        transferBufferCache.Clear();
                        transferBufferCache.AddRange(nodeState.EnumerateBufferUnsorted());

                        foreach (var pair in transferBufferCache)
                        {
                            if (transferredThisStep >= nodeState.Definition.MaxTransferPerStep)
                            {
                                break;
                            }

                            if (!CanTransferItemOut(nodeState, pair.Key))
                            {
                                continue;
                            }

                            if (!HasTransferLink(nodeState, targetNode, direction, pair.Key))
                            {
                                continue;
                            }

                            if (!targetNode.CanAccept(pair.Key))
                            {
                                continue;
                            }

                            var remainingBudget = nodeState.Definition.MaxTransferPerStep - transferredThisStep;
                            var transferCount = Math.Min(pair.Value, remainingBudget);
                            for (var i = 0; i < transferCount; i++)
                            {
                                if (nodeState.RemoveOne(pair.Key) == 0)
                                {
                                    break;
                                }

                                if (!targetNode.TryAddToBuffer(pair.Key, 1))
                                {
                                    nodeState.TryAddToBuffer(pair.Key, 1);
                                    break;
                                }

                                nodeState.MarkActive();
                                targetNode.MarkActive();
                                transferredThisStep++;
                                dirty = true;
                            }
                        }
                    }
                }

                return dirty;
            }
        }

        private bool AutoCollectBufferedCards()
        {
            return false;
        }

        private void CacheNodesForStep()
        {
            stepNodeIterationCache.Clear();
            stepNodeIterationCache.AddRange(nodes.Values);
            stepNodeIterationCache.Sort((left, right) =>
            {
                var yCompare = left.Position.y.CompareTo(right.Position.y);
                if (yCompare != 0)
                {
                    return yCompare;
                }

                return left.Position.x.CompareTo(right.Position.x);
            });
        }

        private void CacheNodesForTransfer()
        {
            transferNodeIterationCache.Clear();
            transferNodeIterationCache.AddRange(nodes.Values);
            transferNodeIterationCache.Sort((left, right) =>
            {
                var xCompare = left.Position.x.CompareTo(right.Position.x);
                if (xCompare != 0)
                {
                    return xCompare;
                }

                return left.Position.y.CompareTo(right.Position.y);
            });
        }

        private int RemoveFromBuffer(WorkshopNodeState nodeState, WorkshopItemDefinition item, int amount)
        {
            var removed = 0;
            while (removed < amount && nodeState.RemoveOne(item) > 0)
            {
                removed++;
            }

            return removed;
        }

        private int GetReserveCount(WorkshopItemDefinition item)
        {
            return item != null && reserveItems.TryGetValue(item, out var amount) ? amount : 0;
        }

        private Dictionary<string, WorkshopNodeDefinition> BuildNodeLookup()
        {
            return ContentDatabase.PlaceableNodes
                .Where(node => node != null && !string.IsNullOrWhiteSpace(node.Id))
                .GroupBy(node => node.Id, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        }

        private Dictionary<string, WorkshopItemDefinition> BuildItemLookup()
        {
            Dictionary<string, WorkshopItemDefinition> itemLookup = new Dictionary<string, WorkshopItemDefinition>(StringComparer.Ordinal);

            foreach (WorkshopNodeDefinition node in ContentDatabase.PlaceableNodes.Where(node => node != null))
            {
                foreach (WorkshopProductionRecipe recipe in node.Recipes.Where(recipe => recipe != null))
                {
                    foreach (WorkshopItemStack stack in (recipe.Inputs ?? Array.Empty<WorkshopItemStack>()).Concat(recipe.Outputs ?? Array.Empty<WorkshopItemStack>()))
                    {
                        RegisterItem(itemLookup, stack != null ? stack.Item : null);
                    }
                }
            }

            foreach (WorkshopRewardDefinition reward in ContentDatabase.DebugRewards.Where(reward => reward != null))
            {
                foreach (WorkshopItemStack stack in reward.GrantedItems ?? Array.Empty<WorkshopItemStack>())
                {
                    RegisterItem(itemLookup, stack != null ? stack.Item : null);
                }
            }

            return itemLookup;
        }

        private static void RegisterItem(IDictionary<string, WorkshopItemDefinition> itemLookup, WorkshopItemDefinition item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Id) || itemLookup.ContainsKey(item.Id))
            {
                return;
            }

            itemLookup.Add(item.Id, item);
        }

        private static bool CanTransferItemOut(WorkshopNodeState nodeState, WorkshopItemDefinition item)
        {
            if (nodeState == null || item == null)
            {
                return false;
            }

            if (nodeState.Definition.Category == WorkshopNodeCategory.Storage)
            {
                return WorkshopNodeState.StorageAcceptsItem(nodeState.Definition, item);
            }

            return nodeState.Definition.Recipes
                .Where(recipe => recipe != null)
                .SelectMany(recipe => recipe.Outputs ?? Array.Empty<WorkshopItemStack>())
                .Any(output => output != null && output.Item == item);
        }

        private static bool ShouldBufferOutput(WorkshopNodeState nodeState, WorkshopItemStack output)
        {
            return output?.Item != null && (output.Item.Kind == WorkshopItemKind.Resource || ShouldBufferCardOutput(nodeState));
        }

        private static bool ShouldBufferCardOutput(WorkshopNodeState nodeState)
        {
            return nodeState != null;
        }

        private static bool HasTransferLink(WorkshopNodeState sourceNode, WorkshopNodeState targetNode, NodePortMask direction, WorkshopItemDefinition item)
        {
            if (sourceNode == null || targetNode == null)
            {
                return false;
            }

            var targetDirection = WorkshopDirectionUtility.Opposite(direction);
            if ((targetNode.RotatedInputPorts & targetDirection) != 0)
            {
                return true;
            }

            return SupportsDirectFactoryCardDock(sourceNode, targetNode, item);
        }

        private static bool SupportsDirectFactoryCardDock(WorkshopNodeState sourceNode, WorkshopNodeState targetNode, WorkshopItemDefinition item)
        {
            if (sourceNode == null || targetNode == null || item == null || item.Kind != WorkshopItemKind.Card)
            {
                return false;
            }

            if (targetNode.Definition.Category == WorkshopNodeCategory.Storage)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(targetNode.Definition.Id) &&
                   targetNode.Definition.Id.StartsWith("node.factory.spell_fusion.", StringComparison.Ordinal);
        }

        private bool HasConnectedTransferTarget(WorkshopNodeState nodeState, WorkshopItemDefinition item)
        {
            foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
            {
                if ((nodeState.RotatedOutputPorts & direction) == 0)
                {
                    continue;
                }

                var targetCell = nodeState.Position + WorkshopDirectionUtility.ToOffset(direction);
                if (!nodes.TryGetValue(targetCell, out var targetNode))
                {
                    continue;
                }

                if (!HasTransferLink(nodeState, targetNode, direction, item))
                {
                    continue;
                }

                if (DefinitionAcceptsItem(targetNode.Definition, item))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool DefinitionAcceptsItem(WorkshopNodeDefinition definition, WorkshopItemDefinition item)
        {
            if (definition == null || item == null)
            {
                return false;
            }

            if (definition.Category == WorkshopNodeCategory.Storage)
            {
                return WorkshopNodeState.StorageAcceptsItem(definition, item);
            }

            if (definition.AcceptsAnyResource)
            {
                return item.Kind == WorkshopItemKind.Resource;
            }

            return definition.Recipes
                .Where(recipe => recipe != null)
                .SelectMany(recipe => recipe.Inputs ?? Array.Empty<WorkshopItemStack>())
                .Any(stack => stack != null && stack.Item == item);
        }

        private void ConsumeReserveItems(WorkshopItemDefinition item, int amount)
        {
            if (item == null || amount <= 0 || !reserveItems.TryGetValue(item, out var current))
            {
                return;
            }

            var next = Mathf.Max(0, current - amount);
            if (next == 0)
            {
                reserveItems.Remove(item);
            }
            else
            {
                reserveItems[item] = next;
            }
        }

        private void PlaceNodeInternal(Vector2Int cell, WorkshopNodeDefinition definition, int rotationQuarterTurns)
        {
            nodes[cell] = new WorkshopNodeState(definition, cell, rotationQuarterTurns);
        }

        private void BeginNotificationBatch()
        {
            notificationSuppressionDepth++;
        }

        private void EndNotificationBatch()
        {
            notificationSuppressionDepth = Math.Max(0, notificationSuppressionDepth - 1);
            if (notificationSuppressionDepth == 0 && notificationQueued)
            {
                notificationQueued = false;
                StateChanged?.Invoke();
            }
        }

        private void RaiseStateChanged()
        {
            if (notificationSuppressionDepth > 0)
            {
                notificationQueued = true;
                return;
            }

            StateChanged?.Invoke();
        }
    }
}
