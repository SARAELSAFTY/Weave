using System;
using System.Collections.Generic;
using Game.Scripts.Runtime.Narrative;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Stores all authored narrative content and related runtime configuration.</summary>
    [CreateAssetMenu(fileName = "NarrativeDatabase", menuName = "Weave/Narrative Database", order = 3)]
    public class NarrativeDatabase : ScriptableObject
    {
        /// <summary>Stores one card node position for the editor graph.</summary>
        [Serializable]
        public struct CardGraphPosition
        {
            public string cardId;
            public Vector2 position;
        }

        /// <summary>Stores one speaker node position for the editor graph.</summary>
        [Serializable]
        public struct SpeakerGraphPosition
        {
            public string memberId;
            public Vector2 position;
        }

        /// <summary>Stores one resource node position for the editor graph.</summary>
        [Serializable]
        public struct ResourceGraphPosition
        {
            public string resourceId;
            public Vector2 position;
        }

        [Header("Entry Point")]
        [Tooltip("First card ID shown when game starts.")]
        public string startingCardId;

        [Header("System Config")]
        [Tooltip("The catalog of all kingdom resources.")]
        public ResourceCatalog resourceCatalog;

        [Header("Authored Content")]
        [Tooltip("All card assets in the game.")]
        public List<CardData> cards = new List<CardData>();

        [Tooltip("All character speaker assets.")]
        public List<CouncilMemberData> speakers = new List<CouncilMemberData>();

        [HideInInspector]
        public List<CardGraphPosition> editorGraphPositions = new List<CardGraphPosition>();

        [HideInInspector]
        public List<SpeakerGraphPosition> editorSpeakerPositions = new List<SpeakerGraphPosition>();

        [HideInInspector]
        public List<ResourceGraphPosition> editorResourcePositions = new List<ResourceGraphPosition>();
    }
}