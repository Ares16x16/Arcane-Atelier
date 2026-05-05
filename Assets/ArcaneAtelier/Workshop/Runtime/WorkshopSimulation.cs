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

        private readonly Dictionary<WorkshopItemDefinition, int> buffer = new Dictionary<WorkshopItemDefinition, int>();
        private int bufferedItemCount;

        public WorkshopNodeState(WorkshopNodeDefinition definition, Vector2Int position, int rotationQuarterTurns)
        {
            Definition = definition;
            Position = position;
            RotationQuarterTurns = ((rotationQuarterTurns % 4) + 4) % 4;
            SpeedMultiplier = 1f;
        }

        public WorkshopNodeDefinition Definition { get; }
        public Vector2Int Position { get; }
        public int RotationQuarterTurns { get; private set; }
        public float SpeedMultiplier { get; private set; }
        public float CycleProgress { get; set; }
        public IReadOnlyDictionary<WorkshopItemDefinition, int> Buffer => buffer;

        public NodePortMask RotatedInputPorts => WorkshopDirectionUtility.Rotate(Definition.InputPorts, RotationQuarterTurns);
        public NodePortMask RotatedOutputPorts => WorkshopDirectionUtility.Rotate(Definition.OutputPorts, RotationQuarterTurns);
        public int BufferedItemCount => bufferedItemCount;

        public void RotateClockwise()
        {
            RotationQuarterTurns = (RotationQuarterTurns + 1) % 4;
        }

        public void ApplyEfficiencyBonus(float bonus)
        {
            SpeedMultiplier = Mathf.Max(0.1f, SpeedMultiplier + bonus);
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

            if (definition.Id == SpellConduitId || definition.Id == TurningSpellConduitId || definition.Id == TurningSpellConduitMirrorId)
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
            BeginNotificationBatch();
            try
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

                foreach (var seed in (ContentDatabase.DefaultLayout ?? Array.Empty<WorkshopPlacedNodeSeed>()).Where(seed => seed != null && seed.NodeDefinition != null))
                {
                    PlaceNodeInternal(seed.Position, seed.NodeDefinition, seed.RotationQuarterTurns);
                }
            }
            finally
            {
                EndNotificationBatch();
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
            var snapshot = new Dictionary<WorkshopItemDefinition, int>(preparedCards);

            foreach (var nodeState in nodes.Values)
            {
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

                        var targetDirection = WorkshopDirectionUtility.Opposite(direction);
                        if ((targetNode.RotatedInputPorts & targetDirection) == 0)
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
            var dirty = false;

            foreach (var nodeState in nodes.Values)
            {
                if (nodeState.Definition.Category == WorkshopNodeCategory.Storage)
                {
                    continue;
                }

                transferBufferCache.Clear();
                transferBufferCache.AddRange(nodeState.EnumerateBufferUnsorted().Where(pair => pair.Key != null && pair.Key.Kind == WorkshopItemKind.Card));
                foreach (var pair in transferBufferCache)
                {
                    if (pair.Value <= 0 || HasConnectedTransferTarget(nodeState, pair.Key))
                    {
                        continue;
                    }

                    var moved = RemoveFromBuffer(nodeState, pair.Key, pair.Value);
                    if (moved <= 0)
                    {
                        continue;
                    }

                    if (!preparedCards.TryAdd(pair.Key, moved))
                    {
                        preparedCards[pair.Key] += moved;
                    }

                    dirty = true;
                }
            }

            return dirty;
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
            return nodeState != null && nodeState.RotatedOutputPorts != NodePortMask.None && nodeState.Definition.MaxTransferPerStep > 0;
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

                var targetDirection = WorkshopDirectionUtility.Opposite(direction);
                if ((targetNode.RotatedInputPorts & targetDirection) == 0)
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
