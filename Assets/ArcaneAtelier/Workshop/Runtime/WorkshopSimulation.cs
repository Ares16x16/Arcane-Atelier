using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopNodeState
    {
        private readonly Dictionary<WorkshopItemDefinition, int> buffer = new();

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
        public int BufferedItemCount => buffer.Values.Sum();

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
            if (item == null || item.Kind == WorkshopItemKind.Card || BufferedItemCount >= Definition.BufferCapacity)
            {
                return false;
            }

            if (Definition.AcceptsAnyResource || Definition.Category == WorkshopNodeCategory.Storage)
            {
                return true;
            }

            return Definition.Recipes.SelectMany(recipe => recipe.Inputs).Any(stack => stack.Item == item);
        }

        public bool TryAddToBuffer(WorkshopItemDefinition item, int amount)
        {
            if (item == null || amount <= 0 || BufferedItemCount + amount > Definition.BufferCapacity)
            {
                return false;
            }

            if (!buffer.TryAdd(item, amount))
            {
                buffer[item] += amount;
            }

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

            return 1;
        }

        public int CountItem(WorkshopItemDefinition item)
        {
            return item != null && buffer.TryGetValue(item, out var amount) ? amount : 0;
        }

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

    public sealed class WorkshopSimulation
    {
        private readonly Dictionary<Vector2Int, WorkshopNodeState> nodes = new();
        private readonly Dictionary<WorkshopItemDefinition, int> preparedCards = new();
        private readonly Dictionary<WorkshopItemDefinition, int> reserveItems = new();
        private readonly HashSet<string> unlockedNodeIds = new();
        private readonly Vector2Int gridSize;

        public WorkshopSimulation(WorkshopContentDatabase contentDatabase)
        {
            ContentDatabase = contentDatabase;
            gridSize = contentDatabase.GridSize;

            foreach (var node in contentDatabase.PlaceableNodes.Where(node => node != null && node.UnlockedByDefault))
            {
                unlockedNodeIds.Add(node.Id);
            }

            foreach (var seed in contentDatabase.DefaultLayout.Where(seed => seed != null && seed.NodeDefinition != null))
            {
                PlaceNode(seed.Position, seed.NodeDefinition, seed.RotationQuarterTurns, true);
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

            nodes[cell] = new WorkshopNodeState(definition, cell, rotationQuarterTurns);
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
            nodes.Clear();
            preparedCards.Clear();
            reserveItems.Clear();
            unlockedNodeIds.Clear();

            foreach (var node in ContentDatabase.PlaceableNodes.Where(node => node != null && node.UnlockedByDefault))
            {
                unlockedNodeIds.Add(node.Id);
            }

            foreach (var seed in ContentDatabase.DefaultLayout.Where(seed => seed != null && seed.NodeDefinition != null))
            {
                PlaceNode(seed.Position, seed.NodeDefinition, seed.RotationQuarterTurns, true);
            }

            RaiseStateChanged();
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            var dirty = false;

            foreach (var nodeState in nodes.Values.OrderBy(node => node.Position.y).ThenBy(node => node.Position.x))
            {
                foreach (var recipe in nodeState.Definition.Recipes)
                {
                    nodeState.CycleProgress += deltaTime * nodeState.SpeedMultiplier;
                    while (nodeState.CycleProgress >= recipe.CycleSeconds)
                    {
                        if (!TryExecuteRecipe(nodeState, recipe))
                        {
                            break;
                        }

                        nodeState.CycleProgress -= recipe.CycleSeconds;
                        dirty = true;
                    }

                    break;
                }
            }

            dirty |= TransferBufferedItems();

            if (dirty)
            {
                RaiseStateChanged();
            }
        }

        public WorkshopInventoryView BuildInventoryView()
        {
            var network = new Dictionary<WorkshopItemDefinition, int>(reserveItems);

            foreach (var nodeState in nodes.Values)
            {
                foreach (var pair in nodeState.EnumerateBuffer())
                {
                    if (!network.TryAdd(pair.Key, pair.Value))
                    {
                        network[pair.Key] += pair.Value;
                    }
                }
            }

            return new WorkshopInventoryView(network, new Dictionary<WorkshopItemDefinition, int>(preparedCards));
        }

        public void CommitBattlePayload()
        {
            WorkshopBattlePayloadBridge.Commit(preparedCards);
        }

        private bool TryExecuteRecipe(WorkshopNodeState nodeState, WorkshopProductionRecipe recipe)
        {
            if (recipe == null)
            {
                return false;
            }

            foreach (var input in recipe.Inputs)
            {
                var available = nodeState.CountItem(input.Item) + GetReserveCount(input.Item);
                if (available < input.Amount)
                {
                    return false;
                }
            }

            foreach (var output in recipe.Outputs.Where(output => output.Item != null && output.Item.Kind == WorkshopItemKind.Resource))
            {
                if (nodeState.BufferedItemCount + output.Amount > nodeState.Definition.BufferCapacity)
                {
                    return false;
                }
            }

            foreach (var input in recipe.Inputs)
            {
                var remaining = input.Amount;
                remaining -= RemoveFromBuffer(nodeState, input.Item, remaining);
                if (remaining > 0)
                {
                    ConsumeReserveItems(input.Item, remaining);
                }
            }

            foreach (var output in recipe.Outputs)
            {
                if (output.Item == null || output.Amount <= 0)
                {
                    continue;
                }

                if (output.Item.Kind == WorkshopItemKind.Card)
                {
                    if (!preparedCards.TryAdd(output.Item, output.Amount))
                    {
                        preparedCards[output.Item] += output.Amount;
                    }
                }
                else
                {
                    nodeState.TryAddToBuffer(output.Item, output.Amount);
                }
            }

            return true;
        }

        private bool TransferBufferedItems()
        {
            var dirty = false;

            foreach (var nodeState in nodes.Values.OrderBy(node => node.Position.x).ThenBy(node => node.Position.y))
            {
                if (nodeState.Definition.MaxTransferPerStep <= 0)
                {
                    continue;
                }

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

                    var transferredThisEdge = 0;
                    foreach (var pair in nodeState.EnumerateBuffer().ToArray())
                    {
                        if (transferredThisEdge >= nodeState.Definition.MaxTransferPerStep)
                        {
                            break;
                        }

                        if (!targetNode.CanAccept(pair.Key))
                        {
                            continue;
                        }

                        if (nodeState.RemoveOne(pair.Key) == 0)
                        {
                            continue;
                        }

                        if (!targetNode.TryAddToBuffer(pair.Key, 1))
                        {
                            nodeState.TryAddToBuffer(pair.Key, 1);
                            continue;
                        }

                        transferredThisEdge++;
                        dirty = true;
                    }
                }
            }

            return dirty;
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

        private void RaiseStateChanged()
        {
            StateChanged?.Invoke();
        }
    }
}
