using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The interface's look in one place: a warm honey, cream and wood palette after the concept
    /// art, the button colours by role, and the Material Symbols glyphs the icon font carries
    /// (codepoints from MaterialSymbolsRounded.codepoints).
    /// </summary>
    internal static class UiTheme
    {
        // Surfaces
        public static readonly Color Cream = Hex(0xFFF6E3);
        public static readonly Color CreamShade = Hex(0xF2E3C4);
        public static readonly Color Wood = Hex(0x8A5A2E);
        public static readonly Color WoodDark = Hex(0x6B4220);
        public static readonly Color Scrim = new Color(0.12f, 0.08f, 0.03f, 0.62f);
        public static readonly Color ShadowTint = new Color(0.1f, 0.06f, 0.02f, 0.3f);

        // Text
        public static readonly Color TextDark = Hex(0x4A2C0E);
        public static readonly Color TextSoft = Hex(0x76563A);
        public static readonly Color TextLight = Hex(0xFFF8E6);
        public static readonly Color Honey = Hex(0xFFC233);

        // Buttons by role (the sprite darkens its own lip)
        public static readonly Color Primary = Hex(0xFFB627);
        public static readonly Color Good = Hex(0x7CC45A);
        public static readonly Color Ad = Hex(0x36A6DB);
        public static readonly Color Royal = Hex(0xA77BE0);
        public static readonly Color Shop = Hex(0x4DBF9A);
        public static readonly Color Neutral = Hex(0xC9B9A0);
        public static readonly Color Disabled = Hex(0xD6CCBB);
        public static readonly Color Alert = Hex(0xF2545B);
        /// <summary>Dewdrops, the premium currency: clear of honey yellow and jelly purple.</summary>
        public static readonly Color Dew = Hex(0x2FB5C8);
        public static readonly Color DewPale = Hex(0xDDF7F9);

        // Bloom bar
        public static readonly Color BarTrack = new Color(0.25f, 0.15f, 0.06f, 0.72f);
        public static readonly Color BarFill = Hex(0x8BD35F);
        public static readonly Color FlowerPink = Hex(0xF266B3);

        // Icon glyphs (Material Symbols Rounded, filled)
        public const string IconShop = "\uEA12";
        public const string IconCrown = "\uECB3";
        public const string IconSpeed = "\uEA0B";
        public const string IconHoney = "\uE798";
        public const string IconClose = "\uE5CD";
        public const string IconAd = "\uF06A";
        public const string IconTouch = "\uE913";
        public const string IconCheck = "\uE668";
        public const string IconLock = "\uE899";
        public const string IconAdd = "\uE145";
        public const string IconEvolve = "\uEACF";
        public const string IconFlower = "\uE545";
        public const string IconHive = "\uEAA6";
        public const string IconBee = "\uEA1C";
        public const string IconSettings = "\uE8B8";
        public const string IconStar = "\uF09A";
        public const string IconTrophy = "\uEA23";
        public const string IconSound = "\uE050";
        public const string IconSoundOff = "\uE04F";
        public const string IconMusic = "\uE405";
        public const string IconMusicOff = "\uE440";
        public const string IconInfo = "\uE88E";
        public const string IconRestore = "\uE8B3";
        public const string IconGift = "\uE8F6";
        public const string IconTimer = "\uE425";
        public const string IconSparkle = "\uE65F";
        public const string IconUp = "\uE5D8";
        public const string IconJelly = "\uEB4C";
        public const string IconPrivacy = "\uF0DC";

        /// <summary>Every glyph above, for building the icon font atlas.</summary>
        public const string AllIcons = IconShop + IconCrown + IconSpeed + IconHoney + IconClose + IconAd + IconTouch
            + IconCheck + IconLock + IconAdd + IconEvolve + IconFlower + IconHive + IconBee + IconSettings + IconStar
            + IconTrophy + IconSound + IconSoundOff + IconMusic + IconMusicOff + IconInfo + IconRestore + IconGift
            + IconTimer + IconSparkle + IconUp + IconJelly + IconPrivacy;

        static Color Hex(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
    }
}
