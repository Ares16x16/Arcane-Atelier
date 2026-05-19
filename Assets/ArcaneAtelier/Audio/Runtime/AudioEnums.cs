namespace ArcaneAtelier.Audio
{
    public enum MusicTrack
    {
        None = 0,
        MainMenu = 1,
        Prologue = 2,
        Workshop = 3,
        Battle = 4,
        VictorySting = 5,
        DefeatSting = 6,
    }

    public enum SFXType
    {
        None = 0,

        // Workshop / Factory
        NodePlacement = 100,
        NodeRotation = 101,
        NodeRemoval = 102,
        ElementProductionTick = 103,
        SpellCardOutputBasic = 104,
        SpellCardOutputIntermediate = 105,
        SpellCardOutputAdvanced = 106,
        PayloadCommit = 107,

        // UI
        ButtonClick = 200,
        ButtonHover = 201,
        BoonDrawerOpen = 202,
        BoonDrawerClose = 203,
        UnlockNotification = 204,
        ErrorBuzz = 205,

        // Battle
        CardDraw = 300,
        CardPlayWhoosh = 301,
        AttackHitGeneric = 302,
        FireHit = 310,
        WaterHit = 311,
        WindHit = 312,
        EarthHit = 313,
        IceHit = 314,
        ThunderHit = 315,
        LightHit = 316,
        DarkHit = 317,
        HealRestore = 320,
        ShieldBlock = 321,
        PlayerHurt = 330,
        EnemyHurt = 331,
        EnemyDefeat = 332,
        PlayerDefeat = 333,
        EndTurnConfirm = 340,
    }
}
