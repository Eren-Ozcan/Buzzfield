using Buzzfield.Flowers;
using Buzzfield.Game;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Blooms flowers the way bees do, by taking nectar from them, without waiting for
    /// nectar to regrow: every visit refills the garden first and takes a full flower.
    /// </summary>
    internal static class TestBloom
    {
        const int MaxVisits = 1000;

        /// <summary>One visit: refills every active flower, then takes all of this one's nectar. Returns the units taken.</summary>
        public static double Visit(GameManager game, Flower flower)
        {
            game.Flowers.RegenerateFor(3600);
            return game.Flowers.Collect(flower, flower.Type.MaxNectar);
        }

        /// <summary>Visits until the flower blooms (or is not active); returns the number of visits.</summary>
        public static int UntilBloomed(GameManager game, Flower flower)
        {
            int visits = 0;
            while (flower.IsActive && !flower.IsBloomed && visits < MaxVisits)
            {
                Visit(game, flower);
                visits++;
            }
            return visits;
        }
    }
}
