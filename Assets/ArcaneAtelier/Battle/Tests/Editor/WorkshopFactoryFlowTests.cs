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
            Assert.That(CountPrepared(inventory, "Inferno Brand"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Frost Pin"), Is.GreaterThan(0));
            Assert.That(CountPrepared(inventory, "Tidal Mend"), Is.EqualTo(0));
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

        private static int CountPrepared(WorkshopInventoryView inventory, string displayName)
        {
            return inventory.PreparedCards
                .Where(pair => pair.Key != null && pair.Key.DisplayName == displayName)
                .Sum(pair => pair.Value);
        }
    }
}
