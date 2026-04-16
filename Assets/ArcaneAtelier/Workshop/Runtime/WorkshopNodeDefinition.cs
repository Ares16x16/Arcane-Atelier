using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class WorkshopProductionRecipe
    {
        [SerializeField] private string id = "recipe.id";
        [SerializeField] private string displayName = "Recipe";
        [SerializeField] private float cycleSeconds = 1.5f;
        [SerializeField] private List<WorkshopItemStack> inputs = new();
        [SerializeField] private List<WorkshopItemStack> outputs = new();

        public string Id => id;
        public string DisplayName => displayName;
        public float CycleSeconds => Mathf.Max(0.1f, cycleSeconds);
        public IReadOnlyList<WorkshopItemStack> Inputs => inputs;
        public IReadOnlyList<WorkshopItemStack> Outputs => outputs;

        public static WorkshopProductionRecipe Create(
            string recipeId,
            string recipeDisplayName,
            float recipeCycleSeconds,
            IEnumerable<WorkshopItemStack> recipeInputs,
            IEnumerable<WorkshopItemStack> recipeOutputs)
        {
            return new WorkshopProductionRecipe
            {
                id = recipeId,
                displayName = recipeDisplayName,
                cycleSeconds = recipeCycleSeconds,
                inputs = recipeInputs?.ToList() ?? new List<WorkshopItemStack>(),
                outputs = recipeOutputs?.ToList() ?? new List<WorkshopItemStack>()
            };
        }
    }

    [CreateAssetMenu(menuName = "Arcane Atelier/Workshop/Node Definition", fileName = "NodeDefinition")]
    public sealed class WorkshopNodeDefinition : ScriptableObject
    {
        [SerializeField] private string id = "node.id";
        [SerializeField] private string displayName = "Node";
        [SerializeField, TextArea] private string description = "Placeholder workshop node.";
        [SerializeField] private WorkshopNodeCategory category = WorkshopNodeCategory.Source;
        [SerializeField] private bool unlockedByDefault = true;
        [SerializeField] private Color tint = new(0.32f, 0.36f, 0.42f);
        [SerializeField] private NodePortMask inputPorts = NodePortMask.None;
        [SerializeField] private NodePortMask outputPorts = NodePortMask.East;
        [SerializeField] private int bufferCapacity = 8;
        [SerializeField] private int maxTransferPerStep = 1;
        [SerializeField] private bool acceptsAnyResource;
        [SerializeField] private List<WorkshopProductionRecipe> recipes = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public WorkshopNodeCategory Category => category;
        public bool UnlockedByDefault => unlockedByDefault;
        public Color Tint => tint;
        public NodePortMask InputPorts => inputPorts;
        public NodePortMask OutputPorts => outputPorts;
        public int BufferCapacity => Mathf.Max(1, bufferCapacity);
        public int MaxTransferPerStep => Mathf.Max(0, maxTransferPerStep);
        public bool AcceptsAnyResource => acceptsAnyResource;
        public IReadOnlyList<WorkshopProductionRecipe> Recipes => recipes;

        public void Configure(
            string nodeId,
            string nodeDisplayName,
            string nodeDescription,
            WorkshopNodeCategory nodeCategory,
            bool isUnlockedByDefault,
            Color nodeTint,
            NodePortMask nodeInputPorts,
            NodePortMask nodeOutputPorts,
            int nodeBufferCapacity,
            int nodeMaxTransferPerStep,
            bool nodeAcceptsAnyResource,
            IEnumerable<WorkshopProductionRecipe> nodeRecipes)
        {
            id = nodeId;
            displayName = nodeDisplayName;
            description = nodeDescription;
            category = nodeCategory;
            unlockedByDefault = isUnlockedByDefault;
            tint = nodeTint;
            inputPorts = nodeInputPorts;
            outputPorts = nodeOutputPorts;
            bufferCapacity = Mathf.Max(1, nodeBufferCapacity);
            maxTransferPerStep = Mathf.Max(0, nodeMaxTransferPerStep);
            acceptsAnyResource = nodeAcceptsAnyResource;
            recipes = nodeRecipes?.ToList() ?? new List<WorkshopProductionRecipe>();
        }
    }
}
