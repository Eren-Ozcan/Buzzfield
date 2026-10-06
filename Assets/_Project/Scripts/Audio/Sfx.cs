namespace Buzzfield.Audio
{
    /// <summary>Every one-shot sound the game plays. New members go at the end: the library stores them by value.</summary>
    public enum Sfx
    {
        UiTap = 0,
        UiRefused = 1,
        UiOpen = 2,
        UiClose = 3,
        UpgradeBuy = 4,
        BeeSpawn = 5,
        BeeEvolve = 6,
        HoneyDeposit = 7,
        PollenShake = 8,
        FlowerBloom = 9,
        SproutPop = 10,
        JellyGain = 11,
        AbilityBuy = 12,
        HoneyCollect = 13,
        Reward = 14,
        PurchaseSuccess = 15,
        GardenComplete = 16,
        QueenMove = 17,
        GoalClaim = 18,
        ButterflyAppear = 19,
        ButterflyCatch = 20,
        DewGain = 21,
    }

    /// <summary>The looping layers under the effects.</summary>
    public enum SoundLoop
    {
        Music = 0,
        AmbienceDry = 1,
        AmbienceBloom = 2,
        SwarmSmall = 3,
        SwarmMedium = 4,
        SwarmLarge = 5,
    }

    /// <summary>Which volume and switch a sound follows.</summary>
    public enum SoundBus
    {
        Effects = 0,
        Ui = 1,
        /// <summary>A short jingle: plays on its own voice and ducks the music while it lasts.</summary>
        Stinger = 2,
    }

    /// <summary>File names of the sounds (docs/audio.md); variants add _1, _2... to the name.</summary>
    public static class SoundFiles
    {
        public static string Name(Sfx id)
        {
            switch (id)
            {
                case Sfx.UiTap: return "sfx_ui_tap";
                case Sfx.UiRefused: return "sfx_ui_refused";
                case Sfx.UiOpen: return "sfx_ui_open";
                case Sfx.UiClose: return "sfx_ui_close";
                case Sfx.UpgradeBuy: return "sfx_upgrade_buy";
                case Sfx.BeeSpawn: return "sfx_bee_spawn";
                case Sfx.BeeEvolve: return "sfx_bee_evolve";
                case Sfx.HoneyDeposit: return "sfx_honey_deposit";
                case Sfx.PollenShake: return "sfx_pollen_shake";
                case Sfx.FlowerBloom: return "sfx_flower_bloom";
                case Sfx.SproutPop: return "sfx_sprout_pop";
                case Sfx.JellyGain: return "sfx_jelly_gain";
                case Sfx.AbilityBuy: return "sfx_ability_buy";
                case Sfx.HoneyCollect: return "sfx_honey_collect";
                case Sfx.Reward: return "sfx_reward";
                case Sfx.PurchaseSuccess: return "sfx_purchase_success";
                case Sfx.GardenComplete: return "stinger_garden_complete";
                case Sfx.QueenMove: return "stinger_queen_move";
                case Sfx.GoalClaim: return "sfx_goal_claim";
                case Sfx.ButterflyAppear: return "sfx_butterfly_appear";
                case Sfx.ButterflyCatch: return "sfx_butterfly_catch";
                case Sfx.DewGain: return "sfx_dew_gain";
                default: return id.ToString();
            }
        }

        public static string Name(SoundLoop loop)
        {
            switch (loop)
            {
                case SoundLoop.Music: return "mus_garden_loop";
                case SoundLoop.AmbienceDry: return "amb_dry_loop";
                case SoundLoop.AmbienceBloom: return "amb_bloom_loop";
                case SoundLoop.SwarmSmall: return "bee_swarm_loop_small";
                case SoundLoop.SwarmMedium: return "bee_swarm_loop_medium";
                case SoundLoop.SwarmLarge: return "bee_swarm_loop_large";
                default: return loop.ToString();
            }
        }
    }
}
