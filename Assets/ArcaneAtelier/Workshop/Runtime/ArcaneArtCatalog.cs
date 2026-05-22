using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArcaneAtelier
{
    public static class ArcaneArtCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        private const string ConduitSpritePath = "Nodes/Factories/node_factory_conduit.png";
        private const string TurnConduitSpritePath = "Nodes/Factories/node_factory_turn_conduit.png";
        private const string TurnSpellConduitSpritePath = "Nodes/Factories/node_factory_turn_spell_conduit.png";

        public static Sprite GetElementIcon(ArcaneAtelier.Workshop.WorkshopElementAttribute element)
        {
            string iconName = element switch
            {
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Fire => "icon_fire",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Water => "icon_water",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Wind => "icon_wind",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Earth => "icon_earth",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Ice => "icon_ice",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Thunder => "icon_thunder",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Light => "icon_light",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Dark => "icon_dark",
                _ => string.Empty
            };

            return string.IsNullOrWhiteSpace(iconName)
                ? null
                : LoadSprite($"Elements/{iconName}.png");
        }

        public static Sprite GetPipesOverlay()
        {
            return LoadSprite("Nodes/Factories/Pipes.png");
        }

        public static Sprite GetWorkshopNodeSprite(string nodeId)
        {
            return nodeId switch
            {
                "node.spirit.fire" => LoadSprite("Nodes/Spirits/node_spirit_fire.png"),
                "node.spirit.water" => LoadSprite("Nodes/Spirits/node_spirit_water.png"),
                "node.spirit.wind" => LoadSprite("Nodes/Spirits/node_spirit_wind.png"),
                "node.spirit.earth" => LoadSprite("Nodes/Spirits/node_spirit_earth.png"),
                "node.spirit.ice" => LoadSprite("Nodes/Spirits/node_spirit_ice.png"),
                "node.spirit.thunder" => LoadSprite("Nodes/Spirits/node_spirit_thunder.png"),
                "node.spirit.light" => LoadSprite("Nodes/Spirits/node_spirit_light.png"),
                "node.spirit.dark" => LoadSprite("Nodes/Spirits/node_spirit_dark.png"),
                "node.factory.conduit" => LoadSprite(ConduitSpritePath),
                "node.factory.spell_conduit" => LoadSprite(ConduitSpritePath),
                "node.factory.deck_collector" => LoadSprite(ConduitSpritePath),
                Workshop.WorkshopNodeVariantUtility.TurningConduitId => LoadSprite(TurnConduitSpritePath),
                Workshop.WorkshopNodeVariantUtility.TurningConduitMirrorId => LoadSprite(TurnConduitSpritePath),
                Workshop.WorkshopNodeVariantUtility.TurningSpellConduitId => LoadSprite(TurnSpellConduitSpritePath),
                Workshop.WorkshopNodeVariantUtility.TurningSpellConduitMirrorId => LoadSprite(TurnSpellConduitSpritePath),
                _ => null
            };
        }

        public static Sprite GetSpiritIcon(ArcaneAtelier.Workshop.WorkshopElementAttribute element)
        {
            string nodeId = element switch
            {
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Fire => "node.spirit.fire",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Water => "node.spirit.water",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Wind => "node.spirit.wind",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Earth => "node.spirit.earth",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Ice => "node.spirit.ice",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Thunder => "node.spirit.thunder",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Light => "node.spirit.light",
                ArcaneAtelier.Workshop.WorkshopElementAttribute.Dark => "node.spirit.dark",
                _ => string.Empty
            };

            return string.IsNullOrWhiteSpace(nodeId) ? null : GetWorkshopNodeSprite(nodeId);
        }

        public static Color GetWorkshopNodeTint(string nodeId)
        {
            return nodeId switch
            {
                "node.factory.spell_conduit" => new Color(1f, 0.48f, 0.42f),
                "node.factory.deck_collector" => new Color(1f, 0.74f, 0.28f),
                _ => Color.white
            };
        }

        private static Sprite LoadSprite(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            if (SpriteCache.TryGetValue(relativePath, out Sprite cached))
            {
                return cached;
            }

            string fullPath = Path.Combine(Application.dataPath, "ArcaneAtelier", "Art", relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                SpriteCache[relativePath] = null;
                return null;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            if (bytes == null || bytes.Length == 0)
            {
                SpriteCache[relativePath] = null;
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = $"runtime_{Path.GetFileNameWithoutExtension(relativePath)}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (!texture.LoadImage(bytes))
            {
                Object.Destroy(texture);
                SpriteCache[relativePath] = null;
                return null;
            }

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SpriteCache[relativePath] = sprite;
            return sprite;
        }
    }
}
