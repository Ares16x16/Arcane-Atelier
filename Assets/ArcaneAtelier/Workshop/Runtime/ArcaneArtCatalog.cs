using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ArcaneAtelier
{
    public static class ArcaneArtCatalog
    {
        private sealed class SpriteCacheEntry
        {
            public Sprite Sprite;
            public long Length;
            public long LastWriteUtcTicks;
        }

        private static readonly Dictionary<string, SpriteCacheEntry> SpriteCache = new Dictionary<string, SpriteCacheEntry>();
        private const string ConduitSpritePath = "Nodes/Factories/node_factory_conduit.png";
        private const string TurnConduitSpritePath = "Nodes/Factories/node_factory_turn_conduit.png";
        private const string TurnSpellConduitSpritePath = "Nodes/Factories/node_factory_turn_spell_conduit.png";
        private const string UiArtRoot = "Workshop/";

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

        public static Sprite GetUiBackground()
        {
            return LoadSprite($"{UiArtRoot}WS_BG_Far.png");
        }

        public static Sprite GetUiStatusBar()
        {
            return LoadSprite($"{UiArtRoot}WS_Status_Bar.png");
        }

        public static Sprite GetUiTopLeftPanel()
        {
            return LoadSprite($"{UiArtRoot}WS_TopLeft_Panel.png");
        }

        public static Sprite GetUiRightRailPanel()
        {
            return LoadSprite($"{UiArtRoot}WS_RightRail_Panel.png");
        }

        public static Sprite GetUiPaletteDock()
        {
            return LoadSprite($"{UiArtRoot}WS_PaletteDock.png");
        }

        public static Sprite GetUiSubPanelColumn()
        {
            return LoadSprite($"{UiArtRoot}WS_SubPanel_Column.png");
        }

        public static Sprite GetUiOrnateFrame()
        {
            return LoadSprite($"{UiArtRoot}WS_Frame_temp.png");
        }

        public static Sprite GetUiBoardUnderlay()
        {
            return LoadSprite($"{UiArtRoot}WS_Board_Underlay.png");
        }

        public static Sprite GetUiTileA()
        {
            return LoadSprite($"{UiArtRoot}WS_Tile_A.png");
        }

        public static Sprite GetUiTileB()
        {
            return LoadSprite($"{UiArtRoot}WS_Tile_B.png");
        }

        public static Sprite GetUiTileHover()
        {
            return LoadSprite($"{UiArtRoot}WS_Tile_Hover.png", true);
        }

        public static Sprite GetUiTileSelected()
        {
            return LoadSprite($"{UiArtRoot}WS_Tile_Selected.png", true);
        }

        public static Sprite GetUiLeylineHorizontal()
        {
            return LoadSprite($"{UiArtRoot}WS_Leyline_H.png", true);
        }

        public static Sprite GetUiLeylineVertical()
        {
            return LoadSprite($"{UiArtRoot}WS_Leyline_V.png", true);
        }

        public static Sprite GetUiPanelMain()
        {
            return LoadSprite($"{UiArtRoot}WS_Panel_Main_9slice.png");
        }

        public static Sprite GetUiPanelSub()
        {
            return LoadSprite($"{UiArtRoot}WS_Panel_Sub_9slice.png");
        }

        public static Sprite GetUiButton()
        {
            return LoadSprite($"{UiArtRoot}WS_Button.png", true, 28);
        }

        public static Sprite GetUiButtonSmall()
        {
            return LoadSprite($"{UiArtRoot}WS_Button_Small.png", true);
        }

        public static Sprite GetUiTabActive()
        {
            return LoadSprite($"{UiArtRoot}WS_Tab_Active.png", true);
        }

        public static Sprite GetUiTabInactive()
        {
            return LoadSprite($"{UiArtRoot}WS_Tab_Inactive.png", true);
        }

        public static Sprite GetUiBlueprintCard()
        {
            return LoadSprite($"{UiArtRoot}WS_Blueprint_Card.png", true, 20);
        }

        public static Sprite GetUiSlotFrame()
        {
            return LoadSprite($"{UiArtRoot}WS_Slot_Frame.png", true);
        }

        public static Sprite GetUiTooltipFrame()
        {
            return LoadSprite($"{UiArtRoot}WS_Tooltip_Frame.png", true);
        }

        public static Sprite GetWorkshopBackground()
        {
            return GetUiBackground();
        }

        public static Sprite GetWorkshopStatusBar()
        {
            return GetUiStatusBar();
        }

        public static Sprite GetWorkshopTopLeftPanel()
        {
            return GetUiTopLeftPanel();
        }

        public static Sprite GetWorkshopRightRailPanel()
        {
            return GetUiRightRailPanel();
        }

        public static Sprite GetWorkshopPaletteDock()
        {
            return GetUiPaletteDock();
        }

        public static Sprite GetWorkshopSubPanelColumn()
        {
            return GetUiSubPanelColumn();
        }

        public static Sprite GetWorkshopOrnateFrame()
        {
            return GetUiOrnateFrame();
        }

        public static Sprite GetWorkshopBoardUnderlay()
        {
            return GetUiBoardUnderlay();
        }

        public static Sprite GetWorkshopTileA()
        {
            return GetUiTileA();
        }

        public static Sprite GetWorkshopTileB()
        {
            return GetUiTileB();
        }

        public static Sprite GetWorkshopTileHover()
        {
            return GetUiTileHover();
        }

        public static Sprite GetWorkshopTileSelected()
        {
            return GetUiTileSelected();
        }

        public static Sprite GetWorkshopLeylineHorizontal()
        {
            return GetUiLeylineHorizontal();
        }

        public static Sprite GetWorkshopLeylineVertical()
        {
            return GetUiLeylineVertical();
        }

        public static Sprite GetWorkshopPanelMain()
        {
            return GetUiPanelMain();
        }

        public static Sprite GetWorkshopPanelSub()
        {
            return GetUiPanelSub();
        }

        public static Sprite GetWorkshopButton()
        {
            return GetUiButton();
        }

        public static Sprite GetWorkshopButtonSmall()
        {
            return GetUiButtonSmall();
        }

        public static Sprite GetWorkshopTabActive()
        {
            return GetUiTabActive();
        }

        public static Sprite GetWorkshopTabInactive()
        {
            return GetUiTabInactive();
        }

        public static Sprite GetWorkshopBlueprintCard()
        {
            return GetUiBlueprintCard();
        }

        public static Sprite GetWorkshopSlotFrame()
        {
            return GetUiSlotFrame();
        }

        public static Sprite GetWorkshopTooltipFrame()
        {
            return GetUiTooltipFrame();
        }

        private static Sprite LoadSprite(string relativePath, bool trimTransparentBounds = false, byte alphaThreshold = 8)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return null;
            }

            string cacheKey = trimTransparentBounds ? $"{relativePath}|trim|{alphaThreshold}" : relativePath;
            string fullPath = Path.Combine(Application.dataPath, "ArcaneAtelier", "Art", relativePath.Replace('/', Path.DirectorySeparatorChar));
            FileInfo fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                SpriteCache[cacheKey] = null;
                return null;
            }

            long fileLength = fileInfo.Length;
            long lastWriteUtcTicks = fileInfo.LastWriteTimeUtc.Ticks;
            if (SpriteCache.TryGetValue(cacheKey, out SpriteCacheEntry cached) &&
                cached != null &&
                cached.Sprite != null &&
                cached.Length == fileLength &&
                cached.LastWriteUtcTicks == lastWriteUtcTicks)
            {
                return cached.Sprite;
            }

            if (cached != null)
            {
                DestroyCachedSprite(cached.Sprite);
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            if (bytes == null || bytes.Length == 0)
            {
                SpriteCache[cacheKey] = null;
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
                SpriteCache[cacheKey] = null;
                return null;
            }

            Rect spriteRect = trimTransparentBounds
                ? FindVisibleRect(texture, alphaThreshold)
                : new Rect(0f, 0f, texture.width, texture.height);

            Sprite sprite = Sprite.Create(
                texture,
                spriteRect,
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            SpriteCache[cacheKey] = new SpriteCacheEntry
            {
                Sprite = sprite,
                Length = fileLength,
                LastWriteUtcTicks = lastWriteUtcTicks
            };
            return sprite;
        }

        private static void DestroyCachedSprite(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            if (sprite.texture != null)
            {
                Object.Destroy(sprite.texture);
            }

            Object.Destroy(sprite);
        }

        private static Rect FindVisibleRect(Texture2D texture, byte alphaThreshold)
        {
            if (texture == null)
            {
                return new Rect(0f, 0f, 2f, 2f);
            }

            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;
            int height = texture.height;
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int y = 0; y < height; y++)
            {
                int rowStart = y * width;
                for (int x = 0; x < width; x++)
                {
                    if (pixels[rowStart + x].a <= alphaThreshold)
                    {
                        continue;
                    }

                    if (x < minX)
                    {
                        minX = x;
                    }

                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (x > maxX)
                    {
                        maxX = x;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new Rect(0f, 0f, texture.width, texture.height);
            }

            return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
