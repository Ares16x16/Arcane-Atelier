namespace ArcaneAtelier.Workshop
{
    public static class WorkshopNodeVariantUtility
    {
        public const string TurningConduitId = "node.factory.turn_conduit";
        public const string TurningConduitMirrorId = "node.factory.turn_conduit.mirror";
        public const string TurningSpellConduitId = "node.factory.turn_spell_conduit";
        public const string TurningSpellConduitMirrorId = "node.factory.turn_spell_conduit.mirror";

        public static bool IsMirrorVariant(string nodeId)
        {
            return nodeId == TurningConduitMirrorId || nodeId == TurningSpellConduitMirrorId;
        }

        public static bool IsCornerConduitFamily(string nodeId)
        {
            return nodeId == TurningConduitId ||
                   nodeId == TurningConduitMirrorId ||
                   nodeId == TurningSpellConduitId ||
                   nodeId == TurningSpellConduitMirrorId;
        }

        public static bool ShouldMirrorSprite(string nodeId)
        {
            return IsMirrorVariant(nodeId);
        }

        public static string GetPaletteGroupId(string nodeId)
        {
            return nodeId switch
            {
                TurningConduitMirrorId => TurningConduitId,
                TurningSpellConduitMirrorId => TurningSpellConduitId,
                _ => nodeId
            };
        }

        public static bool TryGetMirrorVariantId(string nodeId, out string mirrorNodeId)
        {
            switch (nodeId)
            {
                case TurningConduitId:
                case TurningConduitMirrorId:
                    mirrorNodeId = TurningConduitMirrorId;
                    return true;
                case TurningSpellConduitId:
                case TurningSpellConduitMirrorId:
                    mirrorNodeId = TurningSpellConduitMirrorId;
                    return true;
                default:
                    mirrorNodeId = string.Empty;
                    return false;
            }
        }
    }
}
