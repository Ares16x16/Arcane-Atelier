using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArcaneAtelier
{
    public static class ArcaneArtCatalog
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

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
