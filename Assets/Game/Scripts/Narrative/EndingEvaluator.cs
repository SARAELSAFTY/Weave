using Game.Scripts.Definitions;

namespace Game.Scripts.Narrative
{
    /// <summary>Picks an authored ending from the evaluator card's three wired targets using resources and flags.</summary>
    /// <remarks>
    /// Wiring on the evaluator card: left = Tyrant, right = Shadow King,
    /// continue = Warlords' Peace.
    /// </remarks>
    public static class EndingEvaluator
    {
        public const int HegemonyThreshold = 70;
        public const int IronCrownThreshold = 50;

        /// <summary>Returns the ending card that matches the current reign, or null if none are wired.</summary>
        public static CardData Select(
            CardData evaluatorCard,
            ResourceState resourceState,
            ResourceCatalog catalog,
            StoryFlagState flags)
        {
            if (evaluatorCard == null)
            {
                return null;
            }

            CardData tyrant = evaluatorCard.leftNextCard;
            CardData shadowKing = evaluatorCard.rightNextCard;
            CardData warlordsPeace = evaluatorCard.continueNextCard;

            int crown = Get(resourceState, catalog, "Crown");
            int gold = Get(resourceState, catalog, "Gold");
            int army = Get(resourceState, catalog, "Army");
            bool chancellorFallen = flags != null && flags.Has(StoryFlags.ChancellorFallen);

            if (army >= HegemonyThreshold && army >= gold && army >= crown && warlordsPeace != null)
            {
                return warlordsPeace;
            }

            if (chancellorFallen && crown >= IronCrownThreshold && tyrant != null)
            {
                return tyrant;
            }

            if (!chancellorFallen && shadowKing != null)
            {
                return shadowKing;
            }

            if (crown >= gold && crown >= army && tyrant != null)
            {
                return tyrant;
            }

            return shadowKing ?? tyrant ?? warlordsPeace;
        }

        private static int Get(ResourceState resourceState, ResourceCatalog catalog, string assetName)
        {
            if (resourceState == null || catalog == null)
            {
                return 0;
            }

            ResourceData resource = catalog.FindByAssetName(assetName);
            return resource != null ? resourceState.Get(resource) : 0;
        }
    }
}
