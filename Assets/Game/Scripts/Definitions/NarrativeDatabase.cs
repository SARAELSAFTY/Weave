using System;
using System.Collections.Generic;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Central database asset linking all cards, speakers, resources, and prompt templates for a narrative.</summary>
    [CreateAssetMenu(fileName = "NarrativeDatabase", menuName = "Weave/Narrative Database", order = 3)]
    public class NarrativeDatabase : ScriptableObject
    {
        /// <summary>Stores a card reference paired with its editor graph-node position.</summary>
        [Serializable]
        public struct CardGraphPosition
        {
            /// <summary>The card this position entry refers to.</summary>
            public CardData card;
            /// <summary>Node position in the editor graph view.</summary>
            public Vector2 position;
        }

        /// <summary>Stores a speaker reference paired with its editor graph-node position.</summary>
        [Serializable]
        public struct SpeakerGraphPosition
        {
            /// <summary>The speaker this position entry refers to.</summary>
            public SpeakerData speaker;
            /// <summary>Node position in the editor graph view.</summary>
            public Vector2 position;
        }

        /// <summary>Stores a resource reference paired with its editor graph-node position.</summary>
        [Serializable]
        public struct ResourceGraphPosition
        {
            /// <summary>The resource this position entry refers to.</summary>
            public ResourceData resource;
            /// <summary>Node position in the editor graph view.</summary>
            public Vector2 position;
        }

        [Header("Core References")]
        [Tooltip("The first card shown when the narrative begins.")]
        public CardData startingCard;

        [Tooltip("Catalog of all resources tracked by this narrative.")]
        public ResourceCatalog resourceCatalog;

        [Tooltip("Shared LLM prompt templates used by reaction and petition cards.")]
        public LlmPromptTemplates promptTemplates;

        [Header("Collections")]
        [Tooltip("All cards belonging to this narrative.")]
        public List<CardData> cards = new List<CardData>();

        [Tooltip("All speakers referenced by cards in this narrative.")]
        public List<SpeakerData> speakers = new List<SpeakerData>();

        // Editor-only graph layout data; hidden from the Inspector but serialized for the custom editor.
        [HideInInspector]
        public List<CardGraphPosition> editorGraphPositions = new List<CardGraphPosition>();

        [HideInInspector]
        public List<SpeakerGraphPosition> editorSpeakerPositions = new List<SpeakerGraphPosition>();

        [HideInInspector]
        public List<ResourceGraphPosition> editorResourcePositions = new List<ResourceGraphPosition>();
    }
}
