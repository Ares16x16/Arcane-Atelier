using System.Linq;
using ArcaneAtelier.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace ArcaneAtelier.Battle.Tests
{
    public sealed class WorkshopFactoryFlowTests
    {
        [Test]
        public void ElementFusionKeepsInvalidInputsInsteadOfLeakingThemForward()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition windSpirit = FindNode(database, "node.spirit.wind");
            WorkshopNodeDefinition earthSpirit = FindNode(database, "node.spirit.earth");
            WorkshopNodeDefinition elementFusion = FindNode(database, "node.factory.element_fusion");
            WorkshopNodeDefinition conduit = FindNode(database, "node.factory.conduit");

            simulation.PlaceNode(new Vector2Int(0, 1), windSpirit, 0);
            simulation.PlaceNode(new Vector2Int(1, 0), earthSpirit, 3);
            simulation.PlaceNode(new Vector2Int(1, 1), elementFusion, 0);
            simulation.PlaceNode(new Vector2Int(2, 1), conduit, 0);

            Step(simulation, 80);

            WorkshopNodeState fusionState = simulation.Nodes[new Vector2Int(1, 1)];
            WorkshopNodeState conduitState = simulation.Nodes[new Vector2Int(2, 1)];

            Assert.That(fusionState.BufferedItemCount, Is.GreaterThan(0));
            Assert.That(conduitState.BufferedItemCount, Is.EqualTo(0));
        }

        [Test]
        public void DefaultLayoutStartsWithSingleCenteredCollector()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);

            Assert.That(simulation.Nodes.Count, Is.EqualTo(1));
            Assert.That(simulation.Nodes.ContainsKey(new Vector2Int(24, 24)), Is.True);
            Assert.That(simulation.Nodes[new Vector2Int(24, 24)].Definition.Id, Is.EqualTo("node.factory.deck_collector"));
        }

        [Test]
        public void SpellFusionAcceptsDirectAdjacentCardFeedsWithoutSpellConduit()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition fireSpirit = FindNode(database, "node.spirit.fire");
            WorkshopNodeDefinition elementShaper = FindNode(database, "node.factory.element_shaping");
            WorkshopNodeDefinition spellFusionBasic = FindNode(database, "node.factory.spell_fusion.basic");

            simulation.PlaceNode(new Vector2Int(2, 2), spellFusionBasic, 0);

            simulation.PlaceNode(new Vector2Int(4, 2), fireSpirit, 2);
            simulation.PlaceNode(new Vector2Int(3, 2), elementShaper, 2);

            simulation.PlaceNode(new Vector2Int(2, 0), fireSpirit, 3);
            simulation.PlaceNode(new Vector2Int(2, 1), elementShaper, 3);

            Step(simulation, 160);

            WorkshopItemDefinition infernoBrand = FindItem(database, "spell.intermediate.fire");
            Assert.That(simulation.Nodes[new Vector2Int(2, 2)].CountItem(infernoBrand), Is.GreaterThan(0));
        }

        [Test]
        public void SpellFusionTwoConsumesAdjacentFusionOneOutputs()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition fireSpirit = FindNode(database, "node.spirit.fire");
            WorkshopNodeDefinition waterSpirit = FindNode(database, "node.spirit.water");
            WorkshopNodeDefinition elementShaper = FindNode(database, "node.factory.element_shaping");
            WorkshopNodeDefinition spellFusionBasic = FindNode(database, "node.factory.spell_fusion.basic");
            WorkshopNodeDefinition spellFusionIntermediate = FindNode(database, "node.factory.spell_fusion.intermediate");
            WorkshopNodeDefinition spellConduit = FindNode(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition deckCollector = FindNode(database, "node.factory.deck_collector");

            AddSpellFusionBasicWestFeed(simulation, fireSpirit, elementShaper, spellFusionBasic, new Vector2Int(2, 3));
            AddSpellFusionBasicNorthFeed(simulation, waterSpirit, elementShaper, spellFusionBasic, new Vector2Int(3, 2));
            simulation.PlaceNode(new Vector2Int(3, 3), spellFusionIntermediate, 0, true);
            simulation.PlaceNode(new Vector2Int(4, 3), spellConduit, 0);
            simulation.PlaceNode(new Vector2Int(5, 3), deckCollector, 0);

            Step(simulation, 360);

            WorkshopInventoryView inventory = simulation.BuildInventoryView();
            Assert.That(simulation.Nodes[new Vector2Int(5, 3)].BufferedItemCount, Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Steam Requiem"), Is.GreaterThan(0));
        }

        [Test]
        public void SpellFusionThreeConsumesAdjacentFusionTwoOutputs()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition fireSpirit = FindNode(database, "node.spirit.fire");
            WorkshopNodeDefinition waterSpirit = FindNode(database, "node.spirit.water");
            WorkshopNodeDefinition elementShaper = FindNode(database, "node.factory.element_shaping");
            WorkshopNodeDefinition spellFusionBasic = FindNode(database, "node.factory.spell_fusion.basic");
            WorkshopNodeDefinition spellFusionIntermediate = FindNode(database, "node.factory.spell_fusion.intermediate");
            WorkshopNodeDefinition spellFusionAdvanced = FindNode(database, "node.factory.spell_fusion.advanced");
            WorkshopNodeDefinition spellConduit = FindNode(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition deckCollector = FindNode(database, "node.factory.deck_collector");

            AddSpellFusionBasicWestFeed(simulation, fireSpirit, elementShaper, spellFusionBasic, new Vector2Int(2, 3));
            AddSpellFusionBasicNorthFeed(simulation, waterSpirit, elementShaper, spellFusionBasic, new Vector2Int(3, 2));
            simulation.PlaceNode(new Vector2Int(3, 3), spellFusionIntermediate, 0, true);
            simulation.PlaceNode(new Vector2Int(4, 3), spellFusionAdvanced, 0, true);
            simulation.PlaceNode(new Vector2Int(5, 3), spellConduit, 0);
            simulation.PlaceNode(new Vector2Int(6, 3), deckCollector, 0);

            Step(simulation, 700);

            WorkshopInventoryView inventory = simulation.BuildInventoryView();
            Assert.That(simulation.Nodes[new Vector2Int(6, 3)].BufferedItemCount, Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Boiling Star Requiem"), Is.GreaterThan(0));
        }

        [Test]
        public void HackFactoryLayoutProducesFinalSpellCards()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            WorkshopPlacedNodeSeed[] hackLayout = WorkshopSceneController.BuildHackFactoryLayout(database);
            var duplicateCells = hackLayout
                .GroupBy(seed => seed.Position)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateCells, Is.Empty);

            var simulation = new WorkshopSimulation(database);
            simulation.ResetToLayout(hackLayout);

            Step(simulation, 260);

            WorkshopInventoryView inventory = simulation.BuildInventoryView();
            Assert.That(CountPrepared(inventory, "Boiling Star Requiem"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Heavenbreaker Tempest"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Eclipse Apotheosis"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Zero Point Citadel"), Is.GreaterThan(0));
        }

        [Test]
        public void SpellConduitCanDockIntoFusionFromAnyOutputSide()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopItemDefinition steam = FindItem(database, "spell.advanced.steam");
            WorkshopNodeDefinition spellConduit = FindNode(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition spellFusionAdvanced = FindNode(database, "node.factory.spell_fusion.advanced");
            WorkshopNodeDefinition deckCollector = FindNode(database, "node.factory.deck_collector");

            simulation.PlaceNode(new Vector2Int(2, 2), spellConduit, 1);
            simulation.PlaceNode(new Vector2Int(2, 1), spellFusionAdvanced, 0, true);
            simulation.PlaceNode(new Vector2Int(3, 1), deckCollector, 0);

            WorkshopNodeState conduitState = simulation.Nodes[new Vector2Int(2, 2)];
            conduitState.TryAddToBuffer(steam, 2);

            Step(simulation, 40);

            WorkshopInventoryView inventory = simulation.BuildInventoryView();
            Assert.That(CountPrepared(inventory, "Boiling Star Requiem"), Is.GreaterThan(0));
        }

        [Test]
        public void SpellFusionPortsCanBeEditedPerPlacedNode()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition spellFusionBasic = FindNode(database, "node.factory.spell_fusion.basic");
            simulation.PlaceNode(new Vector2Int(1, 1), spellFusionBasic, 0);

            WorkshopNodeState node = simulation.Nodes[new Vector2Int(1, 1)];
            Assert.That(node.RotatedInputPorts, Is.EqualTo(NodePortMask.West | NodePortMask.South));
            Assert.That(node.RotatedOutputPorts, Is.EqualTo(NodePortMask.East));

            simulation.CycleNodePort(new Vector2Int(1, 1), NodePortMask.West);
            Assert.That(node.RotatedInputPorts, Is.EqualTo(NodePortMask.South));
            Assert.That(node.RotatedOutputPorts, Is.EqualTo(NodePortMask.West));

            simulation.CycleNodePort(new Vector2Int(1, 1), NodePortMask.West);
            Assert.That(node.RotatedInputPorts, Is.EqualTo(NodePortMask.South));
            Assert.That(node.RotatedOutputPorts, Is.EqualTo(NodePortMask.None));

            simulation.CycleNodePort(new Vector2Int(1, 1), NodePortMask.North);
            Assert.That(node.RotatedInputPorts, Is.EqualTo(NodePortMask.North | NodePortMask.South));

            simulation.CycleNodePort(new Vector2Int(1, 1), NodePortMask.East);
            Assert.That(node.RotatedInputPorts, Is.EqualTo(NodePortMask.North | NodePortMask.South));
            Assert.That(node.RotatedOutputPorts, Is.EqualTo(NodePortMask.East));
        }

        [Test]
        public void RunStateSnapshotRestoresPlacedNodesBuffersAndEditedPorts()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopNodeDefinition fireSpirit = FindNode(database, "node.spirit.fire");
            WorkshopNodeDefinition elementShaper = FindNode(database, "node.factory.element_shaping");
            WorkshopNodeDefinition spellFusionBasic = FindNode(database, "node.factory.spell_fusion.basic");
            WorkshopNodeDefinition spellConduit = FindNode(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition deckCollector = FindNode(database, "node.factory.deck_collector");

            simulation.PlaceNode(new Vector2Int(0, 0), fireSpirit, 0);
            simulation.PlaceNode(new Vector2Int(1, 0), elementShaper, 0);
            simulation.PlaceNode(new Vector2Int(2, 0), spellConduit, 0);
            simulation.PlaceNode(new Vector2Int(3, 0), deckCollector, 0);
            simulation.PlaceNode(new Vector2Int(6, 6), spellFusionBasic, 0);
            simulation.CycleNodePort(new Vector2Int(6, 6), NodePortMask.West);

            Step(simulation, 80);

            WorkshopRunStateSnapshot snapshot = simulation.CaptureRunState();
            var restoredSimulation = new WorkshopSimulation(database);
            restoredSimulation.RestoreRunState(snapshot);

            Assert.That(restoredSimulation.Nodes.Count, Is.EqualTo(5));
            Assert.That(restoredSimulation.Nodes[new Vector2Int(6, 6)].RotatedOutputPorts, Is.EqualTo(NodePortMask.West));
            Assert.That(restoredSimulation.Nodes[new Vector2Int(3, 0)].BufferedItemCount, Is.GreaterThan(0));
            Assert.That(CountPrepared(restoredSimulation.BuildInventoryView(), "Cinder Dart"), Is.GreaterThan(0));
        }

        [Test]
        public void ElementFusionProcessesEveryPossiblePairInRecipeOrder()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);
            ClearDefaultLayout(simulation);

            WorkshopItemDefinition wind = FindItem(database, "element.wind");
            WorkshopItemDefinition water = FindItem(database, "element.water");
            WorkshopItemDefinition fire = FindItem(database, "element.fire");
            WorkshopItemDefinition ice = FindItem(database, "element.ice");
            WorkshopItemDefinition thunder = FindItem(database, "element.thunder");
            WorkshopNodeDefinition elementFusion = FindNode(database, "node.factory.element_fusion");

            simulation.PlaceNode(new Vector2Int(1, 1), elementFusion, 0);
            WorkshopNodeState fusionState = simulation.Nodes[new Vector2Int(1, 1)];
            fusionState.TryAddToBuffer(wind, 2);
            fusionState.TryAddToBuffer(water, 1);
            fusionState.TryAddToBuffer(fire, 1);

            Step(simulation, 40);

            Assert.That(fusionState.CountItem(ice), Is.GreaterThan(0));
            Assert.That(fusionState.CountItem(thunder), Is.GreaterThan(0));
        }

        [Test]
        public void ConduitsAcceptOnlyTheirLaneItemTypes()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            WorkshopItemDefinition fire = FindItem(database, "element.fire");
            WorkshopItemDefinition fireSpell = FindItem(database, "spell.basic.fire");
            WorkshopNodeDefinition elementConduit = FindNode(database, "node.factory.conduit");
            WorkshopNodeDefinition turnConduit = FindNode(database, "node.factory.turn_conduit");
            WorkshopNodeDefinition turnConduitMirror = FindNode(database, "node.factory.turn_conduit.mirror");
            WorkshopNodeDefinition spellConduit = FindNode(database, "node.factory.spell_conduit");
            WorkshopNodeDefinition turnSpellConduit = FindNode(database, "node.factory.turn_spell_conduit");
            WorkshopNodeDefinition turnSpellConduitMirror = FindNode(database, "node.factory.turn_spell_conduit.mirror");
            WorkshopNodeDefinition deckCollector = FindNode(database, "node.factory.deck_collector");

            Assert.That(new WorkshopNodeState(elementConduit, Vector2Int.zero, 0).CanAccept(fire), Is.True);
            Assert.That(new WorkshopNodeState(elementConduit, Vector2Int.zero, 0).CanAccept(fireSpell), Is.False);
            Assert.That(new WorkshopNodeState(turnConduit, Vector2Int.zero, 0).CanAccept(fire), Is.True);
            Assert.That(new WorkshopNodeState(turnConduit, Vector2Int.zero, 0).CanAccept(fireSpell), Is.False);
            Assert.That(new WorkshopNodeState(turnConduitMirror, Vector2Int.zero, 0).CanAccept(fire), Is.True);
            Assert.That(new WorkshopNodeState(turnConduitMirror, Vector2Int.zero, 0).CanAccept(fireSpell), Is.False);
            Assert.That(new WorkshopNodeState(spellConduit, Vector2Int.zero, 0).CanAccept(fireSpell), Is.True);
            Assert.That(new WorkshopNodeState(spellConduit, Vector2Int.zero, 0).CanAccept(fire), Is.False);
            Assert.That(new WorkshopNodeState(turnSpellConduit, Vector2Int.zero, 0).CanAccept(fireSpell), Is.True);
            Assert.That(new WorkshopNodeState(turnSpellConduit, Vector2Int.zero, 0).CanAccept(fire), Is.False);
            Assert.That(new WorkshopNodeState(turnSpellConduitMirror, Vector2Int.zero, 0).CanAccept(fireSpell), Is.True);
            Assert.That(new WorkshopNodeState(turnSpellConduitMirror, Vector2Int.zero, 0).CanAccept(fire), Is.False);
            Assert.That(new WorkshopNodeState(deckCollector, Vector2Int.zero, 0).CanAccept(fireSpell), Is.True);
            Assert.That(new WorkshopNodeState(deckCollector, Vector2Int.zero, 0).CanAccept(fire), Is.False);
        }

        private static void Step(WorkshopSimulation simulation, int count)
        {
            for (var i = 0; i < count; i++)
            {
                simulation.Step(0.25f);
            }
        }

        private static void ClearDefaultLayout(WorkshopSimulation simulation)
        {
            foreach (Vector2Int cell in simulation.Nodes.Keys.ToArray())
            {
                simulation.RemoveNode(cell);
            }
        }

        private static WorkshopNodeDefinition FindNode(WorkshopContentDatabase database, string nodeId)
        {
            return database.PlaceableNodes.First(node => node != null && node.Id == nodeId);
        }

        private static WorkshopItemDefinition FindItem(WorkshopContentDatabase database, string itemId)
        {
            return database.PlaceableNodes
                .Where(node => node != null)
                .SelectMany(node => node.Recipes)
                .Where(recipe => recipe != null)
                .SelectMany(recipe => (recipe.Inputs ?? System.Array.Empty<WorkshopItemStack>())
                    .Concat(recipe.Outputs ?? System.Array.Empty<WorkshopItemStack>()))
                .Select(stack => stack?.Item)
                .First(item => item != null && item.Id == itemId);
        }

        private static int CountPrepared(WorkshopInventoryView inventory, string displayName)
        {
            return inventory.PreparedCards
                .Where(pair => pair.Key != null && pair.Key.DisplayName == displayName)
                .Sum(pair => pair.Value);
        }

        private static void AddSpellFusionBasicWestFeed(WorkshopSimulation simulation, WorkshopNodeDefinition spirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, Vector2Int fusionCell)
        {
            simulation.PlaceNode(fusionCell + new Vector2Int(-2, 0), spirit, 0);
            simulation.PlaceNode(fusionCell + new Vector2Int(-1, 0), elementShaper, 0);
            simulation.PlaceNode(fusionCell + new Vector2Int(0, -2), spirit, 3);
            simulation.PlaceNode(fusionCell + new Vector2Int(0, -1), elementShaper, 3);
            simulation.PlaceNode(fusionCell, spellFusionBasic, 0);
        }

        private static void AddSpellFusionBasicNorthFeed(WorkshopSimulation simulation, WorkshopNodeDefinition spirit, WorkshopNodeDefinition elementShaper, WorkshopNodeDefinition spellFusionBasic, Vector2Int fusionCell)
        {
            simulation.PlaceNode(fusionCell + new Vector2Int(0, -2), spirit, 3);
            simulation.PlaceNode(fusionCell + new Vector2Int(0, -1), elementShaper, 3);
            simulation.PlaceNode(fusionCell + new Vector2Int(2, 0), spirit, 2);
            simulation.PlaceNode(fusionCell + new Vector2Int(1, 0), elementShaper, 2);
            simulation.PlaceNode(fusionCell, spellFusionBasic, 3);
        }

    }
}
