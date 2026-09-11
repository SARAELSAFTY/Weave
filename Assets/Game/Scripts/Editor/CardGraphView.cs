using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Definitions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// GraphView implementation for editing a <see cref="NarrativeDatabase"/>: displays card, speaker, and resource nodes with edges representing narrative links, and persists node positions back to the database.
    /// </summary>
    /// <remarks>
    /// Node positions are serialized into the database's editor-only position lists so layout survives domain reloads.
    /// The <c>isPopulating</c> flag suppresses <see cref="graphViewChanged"/> callbacks during full rebuilds to avoid spurious undo records.
    /// </remarks>
    public class CardGraphView : GraphView
    {
        private static readonly Vector2 DefaultNodeSize = new Vector2(260, 200);
        private static readonly Vector2 FallbackViewSize = new Vector2(1280, 720);
        private const float GridSpacingX = 310f;
        private const float GridSpacingY = 240f;
        private const int GridColumns = 4;

        private readonly CardGraphWindow window;
        private NarrativeDatabase database;
        // Guard flag: true while Populate is rebuilding the graph, so OnGraphViewChanged does not record undo or trigger re-populate.
        private bool isPopulating;
        private readonly Label emptyNoticeLabel;

        private readonly Dictionary<CardData, Vector2> positionsByCard = new Dictionary<CardData, Vector2>();
        private readonly Dictionary<SpeakerData, Vector2> positionsBySpeaker = new Dictionary<SpeakerData, Vector2>();
        private readonly Dictionary<ResourceData, Vector2> positionsByResource = new Dictionary<ResourceData, Vector2>();

        // When set, the next Populate call places this newly-created asset at the viewport center instead of the grid fallback.
        private UnityEngine.Object pendingNewAsset;

        // Returns true when the cached position already matches, so no-op saves skip undo registration.
        private static bool HasUnchangedPosition<TAsset>(Dictionary<TAsset, Vector2> cache, TAsset asset, Vector2 position)
            where TAsset : UnityEngine.Object
        {
            return asset != null && cache.TryGetValue(asset, out Vector2 existing) && existing == position;
        }

        /// <summary>
        /// Updates or inserts a position entry in the given list and cache; returns false if the position is unchanged.
        /// </summary>
        private static bool TrySavePosition<TAsset, TEntry>(
            List<TEntry> list, Dictionary<TAsset, Vector2> cache, TAsset asset, Vector2 position,
            Func<TEntry, TAsset> getAsset, Func<TAsset, Vector2, TEntry> makeEntry)
            where TAsset : UnityEngine.Object
        {
            if (list == null || asset == null)
            {
                return false;
            }
            if (cache.TryGetValue(asset, out Vector2 existing) && existing == position)
            {
                return false;
            }

            cache[asset] = position;
            int index = list.FindIndex(e => getAsset(e) == asset);
            if (index >= 0)
            {
                list[index] = makeEntry(asset, position);
            }
            else
            {
                list.Add(makeEntry(asset, position));
            }
            return true;
        }

        /// <summary>Removes a position entry from the given list and cache for the specified asset; returns false if no entry existed.</summary>
        private static bool TryRemovePosition<TAsset, TEntry>(
            List<TEntry> list, Dictionary<TAsset, Vector2> cache, TAsset asset, Func<TEntry, TAsset> getAsset)
            where TAsset : UnityEngine.Object
        {
            if (list == null || asset == null)
            {
                return false;
            }
            cache.Remove(asset);
            int index = list.FindIndex(e => getAsset(e) == asset);
            if (index < 0)
            {
                return false;
            }
            list.RemoveAt(index);
            return true;
        }

        /// <summary>The narrative database currently displayed in the graph, or null if none is loaded.</summary>
        public NarrativeDatabase Database => database;

        /// <summary>Marks a newly created card so that the next <see cref="Populate"/> places it at the viewport center.</summary>
        public void SetPendingNewCard(CardData card)
        {
            pendingNewAsset = card;
        }

        /// <summary>Marks a newly created speaker so that the next <see cref="Populate"/> places it at the viewport center.</summary>
        public void SetPendingNewSpeaker(SpeakerData speaker)
        {
            pendingNewAsset = speaker;
        }

        /// <summary>Marks a newly created resource so that the next <see cref="Populate"/> places it at the viewport center.</summary>
        public void SetPendingNewResource(ResourceData resource)
        {
            pendingNewAsset = resource;
        }

        /// <summary>
        /// Initializes the graph view with zoom, drag, selection manipulators, a grid background, and an empty-state notice label.
        /// </summary>
        /// <param name="window">The owning editor window used for delegating create/repopulate actions.</param>
        public CardGraphView(CardGraphWindow window)
        {
            this.window = window;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            emptyNoticeLabel = new Label();
            emptyNoticeLabel.style.fontSize = 15;
            emptyNoticeLabel.style.color = new StyleColor(new Color(0.65f, 0.65f, 0.70f));
            emptyNoticeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            emptyNoticeLabel.style.marginTop = 220;
            Add(emptyNoticeLabel);

            graphViewChanged = OnGraphViewChanged;
        }

        /// <summary>Returns all ports that belong to a different node and have the opposite direction, enabling cross-node connections only.</summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            foreach (Port port in ports)
            {
                if (port.node == startPort.node || port.direction == startPort.direction)
                {
                    continue;
                }

                compatiblePorts.Add(port);
            }

            return compatiblePorts;
        }

        /// <summary>
        /// Rebuilds the entire graph from the given database: clears existing elements, creates card/speaker/resource nodes at saved or default positions, draws edges, and shows an empty-state message when appropriate.
        /// </summary>
        /// <param name="db">The narrative database to visualize.</param>
        public void Populate(NarrativeDatabase db)
        {
            isPopulating = true;
            database = db;
            LoadPositionsFromDatabase();

            foreach (GraphElement element in graphElements.ToList())
            {
                RemoveElement(element);
            }

            bool hasCards = database?.cards != null && database.cards.Count > 0;
            bool hasSpeakers = database?.speakers != null && database.speakers.Count > 0;
            bool hasResources = database?.resourceCatalog != null && database.resourceCatalog.resources != null && database.resourceCatalog.resources.Count > 0;

            if (database == null || (!hasCards && !hasSpeakers && !hasResources))
            {
                emptyNoticeLabel.style.display = DisplayStyle.Flex;
                emptyNoticeLabel.text = database == null
                    ? "Select a NarrativeDatabase asset in the top toolbar to view and edit the graph."
                    : "Database has no cards, speakers, or resources. Click the buttons in the toolbar to create content.";
                isPopulating = false;
                return;
            }

            emptyNoticeLabel.style.display = DisplayStyle.None;

            Dictionary<CardData, CardNode> nodesByCard = new Dictionary<CardData, CardNode>();
            List<CardNode> nodeList = CreateNodes(nodesByCard);
            DrawEdges(nodeList, nodesByCard);
            CreateSpeakerNodes();
            CreateResourceNodes();

            // Defer clearing the guard so that any layout callbacks fired during element addition are also suppressed.
            EditorApplication.delayCall += () => isPopulating = false;
        }

        /// <summary>Clears in-memory position caches and reloads them from the database's serialized editor position lists.</summary>
        private void LoadPositionsFromDatabase()
        {
            positionsByCard.Clear();
            positionsBySpeaker.Clear();
            positionsByResource.Clear();

            if (database?.editorGraphPositions != null)
            {
                foreach (NarrativeDatabase.CardGraphPosition entry in database.editorGraphPositions)
                {
                    if (entry.card != null)
                    {
                        positionsByCard[entry.card] = entry.position;
                    }
                }
            }

            if (database?.editorSpeakerPositions != null)
            {
                foreach (NarrativeDatabase.SpeakerGraphPosition entry in database.editorSpeakerPositions)
                {
                    if (entry.speaker != null)
                    {
                        positionsBySpeaker[entry.speaker] = entry.position;
                    }
                }
            }

            if (database?.editorResourcePositions != null)
            {
                foreach (NarrativeDatabase.ResourceGraphPosition entry in database.editorResourcePositions)
                {
                    if (entry.resource != null)
                    {
                        positionsByResource[entry.resource] = entry.position;
                    }
                }
            }
        }

        /// <summary>Creates and adds a <see cref="CardNode"/> for each card in the database, returning the ordered node list.</summary>
        private List<CardNode> CreateNodes(Dictionary<CardData, CardNode> nodesByCard)
        {
            List<CardNode> nodeList = new List<CardNode>();
            if (database?.cards == null)
            {
                return nodeList;
            }

            int i = 0;
            foreach (CardData card in database.cards)
            {
                if (card == null)
                {
                    continue;
                }

                bool isStartCard = database.startingCard == card;
                CardNode node = new CardNode(card, isStartCard, this);
                node.SetPosition(new Rect(ResolveNodePosition(card, i), DefaultNodeSize));

                AddElement(node);
                nodeList.Add(node);
                nodesByCard[card] = node;
                i++;
            }

            return nodeList;
        }

        /// <summary>Returns the saved position for a card, or computes a grid-based fallback from its list index.</summary>
        private Vector2 ResolveNodePosition(CardData card, int indexInList)
        {
            int column = indexInList % GridColumns;
            int row = indexInList / GridColumns;
            Vector2 gridPosition = new Vector2(column * GridSpacingX + 60, row * GridSpacingY + 60);
            return ResolveNewOrSavedPosition(card, positionsByCard, SavePositionToDatabase, gridPosition);
        }

        /// <summary>Computes the center of the current viewport in content-space coordinates, used for placing newly created nodes.</summary>
        private Vector2 ViewportCenterInContentSpace()
        {
            Vector2 viewSize = layout.size == Vector2.zero ? FallbackViewSize : layout.size;
            Vector2 viewCenter = viewSize * 0.5f;
            Vector2 contentPos = contentViewContainer.WorldToLocal(this.LocalToWorld(viewCenter));
            return contentPos - DefaultNodeSize * 0.5f;
        }

        /// <summary>
        /// Resolves a node's position: if the asset is the pending new asset, places it at viewport center; otherwise uses saved position or falls back to the provided default.
        /// </summary>
        private Vector2 ResolveNewOrSavedPosition<TAsset>(
            TAsset asset,
            Dictionary<TAsset, Vector2> savedPositions,
            Action<TAsset, Vector2> savePosition,
            Vector2 fallbackPosition)
            where TAsset : UnityEngine.Object
        {
            if (asset != null && pendingNewAsset == asset)
            {
                pendingNewAsset = null;
                Vector2 centerPosition = ViewportCenterInContentSpace();
                savePosition(asset, centerPosition);
                return centerPosition;
            }

            if (savedPositions.TryGetValue(asset, out Vector2 savedPosition))
            {
                return savedPosition;
            }

            savePosition(asset, fallbackPosition);
            return fallbackPosition;
        }

        /// <summary>Creates edges between card nodes based on each card's left/right/continue next-card references.</summary>
        private void DrawEdges(List<CardNode> nodeList, Dictionary<CardData, CardNode> nodesByCard)
        {
            foreach (CardNode sourceNode in nodeList)
            {
                CardData card = sourceNode.Card;
                if (card == null)
                {
                    continue;
                }

                if (card.UsesContinueExit)
                {
                    TryConnect(sourceNode.ContinuePort, card.continueNextCard, nodesByCard);
                }
                else
                {
                    TryConnect(sourceNode.LeftPort, card.leftNextCard, nodesByCard);
                    TryConnect(sourceNode.RightPort, card.rightNextCard, nodesByCard);
                    if (card.isThreeWayVerdict)
                    {
                        CardData middleTarget = card.middleNextCard != null ? card.middleNextCard : card.continueNextCard;
                        TryConnect(sourceNode.MiddlePort, middleTarget, nodesByCard);
                    }

                    if (card.isEndingEvaluator)
                    {
                        TryConnect(sourceNode.ContinuePort, card.continueNextCard, nodesByCard);
                        TryConnect(sourceNode.MiddlePort, card.middleNextCard, nodesByCard);
                    }
                }
            }
        }

        /// <summary>Connects an output port to the input port of the target card's node if both exist in the graph.</summary>
        private void TryConnect(Port outputPort, CardData targetCard, Dictionary<CardData, CardNode> nodesByCard)
        {
            if (targetCard != null && nodesByCard.TryGetValue(targetCard, out CardNode targetNode))
            {
                AddElement(outputPort.ConnectTo(targetNode.InputPort));
            }
        }

        /// <summary>Sets the database's starting card with undo support and triggers a full graph repopulate to update visual indicators.</summary>
        /// <param name="card">The card to designate as the starting card.</param>
        public void SetStartingCard(CardData card)
        {
            if (database == null || card == null)
            {
                return;
            }

            Undo.RecordObject(database, "Set Starting Card");
            database.startingCard = card;
            EditorUtility.SetDirty(database);
            window.PopulateGraph();
        }

        private static readonly Vector2 DefaultSpeakerNodeSize = new Vector2(240, 170);

        /// <summary>Creates and adds a <see cref="SpeakerNode"/> for each speaker in the database at saved or default positions.</summary>
        private void CreateSpeakerNodes()
        {
            if (database?.speakers == null)
            {
                return;
            }

            int i = 0;
            foreach (SpeakerData speaker in database.speakers)
            {
                if (speaker == null)
                {
                    continue;
                }

                SpeakerNode speakerNode = new SpeakerNode(speaker);
                Vector2 pos = ResolveSpeakerNodePosition(speaker, i);
                speakerNode.SetPosition(new Rect(pos, DefaultSpeakerNodeSize));
                AddElement(speakerNode);
                i++;
            }
        }

        /// <summary>Returns the saved position for a speaker, or a default left-column position based on list index.</summary>
        private Vector2 ResolveSpeakerNodePosition(SpeakerData speaker, int indexInList)
        {
            Vector2 defaultPosition = new Vector2(-260, indexInList * 210 + 60);
            return ResolveNewOrSavedPosition(speaker, positionsBySpeaker, SaveSpeakerPositionToDatabase, defaultPosition);
        }

        private static readonly Vector2 DefaultResourceNodeSize = new Vector2(240, 320);

        /// <summary>Creates and adds a <see cref="ResourceNode"/> for each resource in the catalog.</summary>
        private void CreateResourceNodes()
        {
            if (database?.resourceCatalog?.resources == null)
            {
                return;
            }

            int i = 0;
            foreach (ResourceData resource in database.resourceCatalog.resources)
            {
                if (resource == null)
                {
                    continue;
                }

                ResourceNode resourceNode = new ResourceNode(resource);
                Vector2 pos = ResolveResourceNodePosition(resource, i);
                resourceNode.SetPosition(new Rect(pos, DefaultResourceNodeSize));
                AddElement(resourceNode);
                i++;
            }
        }

        /// <summary>Returns the saved position for a resource, or a default far-left-column position based on list index.</summary>
        private Vector2 ResolveResourceNodePosition(ResourceData resource, int indexInList)
        {
            Vector2 defaultPosition = new Vector2(-540, indexInList * 340 + 60);
            return ResolveNewOrSavedPosition(resource, positionsByResource, SaveResourcePositionToDatabase, defaultPosition);
        }

        /// <summary>Persists the positions of all moved graph elements (cards, speakers, resources) back to the database with undo support.</summary>
        private void SaveMovedNodePositions(List<GraphElement> movedElements)
        {
            if (database == null || movedElements == null)
            {
                return;
            }

            foreach (GraphElement element in movedElements)
            {
                switch (element)
                {
                    case CardNode cardNode when cardNode.Card != null:
                        SavePositionToDatabase(cardNode.Card, cardNode.GetPosition().position);
                        break;
                    case SpeakerNode speakerNode when speakerNode.Speaker != null:
                        SaveSpeakerPositionToDatabase(speakerNode.Speaker, speakerNode.GetPosition().position);
                        break;
                    case ResourceNode resourceNode when resourceNode.Data != null:
                        SaveResourcePositionToDatabase(resourceNode.Data, resourceNode.GetPosition().position);
                        break;
                }
            }
        }

        private void SavePositionToDatabase(CardData card, Vector2 position)
        {
            if (database == null || card == null)
            {
                return;
            }

            if (HasUnchangedPosition(positionsByCard, card, position))
            {
                return;
            }

            database.editorGraphPositions ??= new List<NarrativeDatabase.CardGraphPosition>();
            Undo.RecordObject(database, "Move Card Node");
            bool changed = TrySavePosition(database.editorGraphPositions, positionsByCard, card, position,
                e => e.card, (c, p) => new NarrativeDatabase.CardGraphPosition { card = c, position = p });
            if (changed)
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void RemovePositionFromDatabase(CardData card)
        {
            if (database?.editorGraphPositions == null || card == null)
            {
                return;
            }

            Undo.RecordObject(database, "Delete Card Node Position");
            if (TryRemovePosition(database.editorGraphPositions, positionsByCard, card, e => e.card))
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void SaveSpeakerPositionToDatabase(SpeakerData speaker, Vector2 position)
        {
            if (database == null || speaker == null)
            {
                return;
            }

            if (HasUnchangedPosition(positionsBySpeaker, speaker, position))
            {
                return;
            }

            database.editorSpeakerPositions ??= new List<NarrativeDatabase.SpeakerGraphPosition>();
            Undo.RecordObject(database, "Move Speaker Node");
            bool changed = TrySavePosition(database.editorSpeakerPositions, positionsBySpeaker, speaker, position,
                e => e.speaker, (s, p) => new NarrativeDatabase.SpeakerGraphPosition { speaker = s, position = p });
            if (changed)
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void RemoveSpeakerPositionFromDatabase(SpeakerData speaker)
        {
            if (database?.editorSpeakerPositions == null || speaker == null)
            {
                return;
            }

            Undo.RecordObject(database, "Delete Speaker Node Position");
            if (TryRemovePosition(database.editorSpeakerPositions, positionsBySpeaker, speaker, e => e.speaker))
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void SaveResourcePositionToDatabase(ResourceData resource, Vector2 position)
        {
            if (database == null || resource == null)
            {
                return;
            }

            if (HasUnchangedPosition(positionsByResource, resource, position))
            {
                return;
            }

            database.editorResourcePositions ??= new List<NarrativeDatabase.ResourceGraphPosition>();
            Undo.RecordObject(database, "Move Resource Node");
            bool changed = TrySavePosition(database.editorResourcePositions, positionsByResource, resource, position,
                e => e.resource, (r, p) => new NarrativeDatabase.ResourceGraphPosition { resource = r, position = p });
            if (changed)
            {
                EditorUtility.SetDirty(database);
            }
        }

        private void RemoveResourcePositionFromDatabase(ResourceData resource)
        {
            if (database?.editorResourcePositions == null || resource == null)
            {
                return;
            }

            Undo.RecordObject(database, "Delete Resource Node Position");
            if (TryRemovePosition(database.editorResourcePositions, positionsByResource, resource, e => e.resource))
            {
                EditorUtility.SetDirty(database);
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (database == null)
            {
                return;
            }

            Vector2 mousePos = evt.mousePosition;
            Vector2 graphPos = contentViewContainer.WorldToLocal(mousePos);

            evt.menu.AppendAction("Create Card", _ => window.CreateCardAt(graphPos));
            evt.menu.AppendAction("Create Speaker", _ => window.CreateSpeakerAt(graphPos));
            if (database.resourceCatalog != null)
            {
                evt.menu.AppendAction("Create Resource", _ => window.CreateResourceAt(graphPos));
            }
            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (isPopulating)
            {
                return change;
            }

            if (change.movedElements != null)
            {
                SaveMovedNodePositions(change.movedElements);
            }

            bool needsRepopulate = false;

            if (change.edgesToCreate != null)
            {
                foreach (Edge edge in change.edgesToCreate)
                {
                    needsRepopulate |= ApplyEdgeCreated(edge);
                }
            }

            if (change.elementsToRemove != null)
            {
                needsRepopulate |= ApplyRemovals(change.elementsToRemove);
            }

            if (needsRepopulate)
            {
                change.edgesToCreate?.Clear();
                change.elementsToRemove?.Clear();
                window.PopulateGraph();
            }

            return change;
        }

        private bool ApplyEdgeCreated(Edge edge)
        {
            if (edge.output?.node is not CardNode sourceNode || edge.input?.node is not CardNode targetNode)
            {
                return false;
            }

            CardData sourceCard = sourceNode.Card;
            CardData targetCard = targetNode.Card;
            if (sourceCard == null || targetCard == null)
            {
                return false;
            }

            Undo.RecordObject(sourceCard, "Connect Card Link");
            if (edge.output == sourceNode.ContinuePort)
            {
                sourceCard.continueNextCard = targetCard;
            }
            else if (edge.output == sourceNode.LeftPort)
            {
                sourceCard.leftNextCard = targetCard;
            }
            else if (edge.output == sourceNode.RightPort)
            {
                sourceCard.rightNextCard = targetCard;
            }
            else if (edge.output == sourceNode.MiddlePort)
            {
                sourceCard.middleNextCard = targetCard;
            }
            EditorUtility.SetDirty(sourceCard);
            return true;
        }

        private bool ApplyRemovals(List<GraphElement> elementsToRemove)
        {
            bool anyChange = false;
            List<GraphElement> snapshot = new List<GraphElement>(elementsToRemove);

            foreach (GraphElement element in snapshot)
            {
                switch (element)
                {
                    case Edge edge:
                        anyChange |= ClearEdgeLink(edge);
                        break;
                    case CardNode cardNode:
                        if (TryDeleteCard(cardNode))
                        {
                            anyChange = true;
                        }
                        else
                        {
                            elementsToRemove.Remove(cardNode);
                        }
                        break;
                    case SpeakerNode speakerNode:
                        if (TryDeleteSpeaker(speakerNode))
                        {
                            anyChange = true;
                        }
                        else
                        {
                            elementsToRemove.Remove(speakerNode);
                        }
                        break;
                    case ResourceNode resourceNode:
                        if (TryDeleteResource(resourceNode))
                        {
                            anyChange = true;
                        }
                        else
                        {
                            elementsToRemove.Remove(resourceNode);
                        }
                        break;
                }
            }

            return anyChange;
        }

        private bool ClearEdgeLink(Edge edge)
        {
            if (edge.output?.node is not CardNode sourceNode || sourceNode.Card == null)
            {
                return false;
            }

            CardData sourceCard = sourceNode.Card;
            Undo.RecordObject(sourceCard, "Clear Card Link");
            if (edge.output == sourceNode.ContinuePort)
            {
                sourceCard.continueNextCard = null;
            }
            else if (edge.output == sourceNode.LeftPort)
            {
                sourceCard.leftNextCard = null;
            }
            else if (edge.output == sourceNode.RightPort)
            {
                sourceCard.rightNextCard = null;
            }
            else if (edge.output == sourceNode.MiddlePort)
            {
                sourceCard.middleNextCard = null;
            }
            EditorUtility.SetDirty(sourceCard);
            return true;
        }

        private bool TryDeleteCard(CardNode cardNode)
        {
            CardData cardToDelete = cardNode.Card;
            if (cardToDelete == null)
            {
                return true;
            }

            bool confirm = EditorUtility.DisplayDialog("Delete Card", $"Delete '{cardToDelete.AssetName}'? This cannot be undone.", "Delete", "Cancel");
            if (!confirm)
            {
                return false;
            }

            RemovePositionFromDatabase(cardToDelete);
            ClearIncomingLinks(cardToDelete);

            string path = AssetDatabase.GetAssetPath(cardToDelete);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            window.RefreshStartingCardDropdownOptions();
            return true;
        }

        private bool TryDeleteSpeaker(SpeakerNode speakerNode)
        {
            SpeakerData speakerToDelete = speakerNode.Speaker;
            if (speakerToDelete == null)
            {
                return true;
            }

            bool confirm = EditorUtility.DisplayDialog("Delete Speaker", $"Delete '{speakerToDelete.AssetName}'? This cannot be undone.", "Delete", "Cancel");
            if (!confirm)
            {
                return false;
            }

            RemoveSpeakerPositionFromDatabase(speakerToDelete);

            if (database?.cards != null)
            {
                foreach (CardData c in database.cards)
                {
                    if (c != null && c.speaker == speakerToDelete)
                    {
                        Undo.RecordObject(c, "Clear Speaker Assignment");
                        c.speaker = null;
                        EditorUtility.SetDirty(c);
                    }
                }
            }

            if (database?.speakers != null)
            {
                Undo.RecordObject(database, "Remove Speaker From Database");
                database.speakers.Remove(speakerToDelete);
                EditorUtility.SetDirty(database);
            }

            string path = AssetDatabase.GetAssetPath(speakerToDelete);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            return true;
        }

        private bool TryDeleteResource(ResourceNode resourceNode)
        {
            ResourceData dataToDelete = resourceNode.Data;
            if (dataToDelete == null)
            {
                return true;
            }

            bool confirm = EditorUtility.DisplayDialog("Delete Resource", $"Delete resource '{dataToDelete.AssetName}'? This cannot be undone.", "Delete", "Cancel");
            if (!confirm)
            {
                return false;
            }

            RemoveResourcePositionFromDatabase(dataToDelete);

            if (database?.resourceCatalog != null)
            {
                Undo.RecordObject(database.resourceCatalog, "Remove Resource From Catalog");
                database.resourceCatalog.resources.Remove(dataToDelete);
                EditorUtility.SetDirty(database.resourceCatalog);
            }

            string path = AssetDatabase.GetAssetPath(dataToDelete);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.DeleteAsset(path);
            }

            return true;
        }

        private void ClearIncomingLinks(CardData cardToDelete)
        {
            if (database == null || database.cards == null)
            {
                return;
            }

            foreach (CardData other in database.cards)
            {
                if (other == null || other == cardToDelete)
                {
                    continue;
                }

                if (other.leftNextCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.leftNextCard = null;
                    EditorUtility.SetDirty(other);
                }

                if (other.rightNextCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.rightNextCard = null;
                    EditorUtility.SetDirty(other);
                }

                if (other.continueNextCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.continueNextCard = null;
                    EditorUtility.SetDirty(other);
                }

                if (other.middleNextCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.middleNextCard = null;
                    EditorUtility.SetDirty(other);
                }

                if (other.skipToCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.skipToCard = null;
                    EditorUtility.SetDirty(other);
                }

                if (other.skipToAltCard == cardToDelete)
                {
                    Undo.RecordObject(other, "Clear Card Link");
                    other.skipToAltCard = null;
                    EditorUtility.SetDirty(other);
                }
            }

            if (database.startingCard == cardToDelete)
            {
                Undo.RecordObject(database, "Clear Starting Card");
                database.startingCard = null;
            }

            Undo.RecordObject(database, "Remove Card From Database");
            database.cards.Remove(cardToDelete);
            EditorUtility.SetDirty(database);
        }
    }
}
