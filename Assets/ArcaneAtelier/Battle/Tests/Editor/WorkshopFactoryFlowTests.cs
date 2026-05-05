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
        public void DefaultLayoutDemonstratesSpellAndElementFusion()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            var simulation = new WorkshopSimulation(database);

            Step(simulation, 220);

            WorkshopInventoryView inventory = simulation.BuildInventoryView();
            WorkshopNodeState spellConduitState = simulation.Nodes[new Vector2Int(4, 5)];

            Assert.That(spellConduitState.BufferedItemCount, Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Inferno Brand"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Frost Pin"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Tidal Mend"), Is.EqualTo(0));
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
    }
}
