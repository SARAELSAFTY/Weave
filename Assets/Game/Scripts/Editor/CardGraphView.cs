using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    // The node-graph canvas: builds CardNodes and edges from a NarrativeDatabase, and writes
    // edge/position/delete changes made in the graph back to the underlying CardData assets.
    public class CardGraphView : GraphView
    {
    private static readonly Vector2 DefaultNodeSize = new Vector2(260, 200);
    private static readonly Vector2 FallbackViewSize = new Vector2(1280, 720);
    private const float GridSpacingX = 310f;
    private const float GridSpacingY = 240f;
    private const int GridColumns = 4;

    private readonly CardGraphWindow window;
    private NarrativeDatabase database;
    private bool isPopulating;
    private readonly Label emptyNoticeLabel;

    // Node positions are persisted on the NarrativeDatabase asset (editorGraphPositions) so
    // layout survives closing/reopening the window and Unity domain reloads. This dictionary is
    // just an in-memory mirror, rebuilt from the asset every Populate(), for fast lookup.
    private readonly Dictionary<string, Vector2> positionsByCardId = new Dictionary<string, Vector2>();
    private readonly Dictionary<string, Vector2> positionsByMemberId = new Dictionary<string, Vector2>();
    private readonly Dictionary<string, Vector2> positionsByResourceId = new Dictionary<string, Vector2>();

    // Set before Populate() so the new card node is placed at the viewport centre.
    private CardData pendingNewCard;
    private string pendingNewSpeakerId;
    private string pendingNewResourceId;

    public NarrativeDatabase Database => database;

    public void SetPendingNewCard(CardData card)
    {
        pendingNewCard = card;
    }

    public void SetPendingNewSpeaker(string memberId)
    {
        pendingNewSpeakerId = memberId;
    }

    public void SetPendingNewResource(string resourceId)
    {
        pendingNewResourceId = resourceId;
    }

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

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.Where(p => p.node != startPort.node && p.direction != startPort.direction).ToList();
    }

    public void Populate(NarrativeDatabase db)
    {
        // Don't call SaveCurrentNodePositions() here. Rebuilding the graph view from scratch
        // shouldn't flush current node positions back to the database unless they were 
        // explicitly moved by the user (which is already handled by OnGraphViewChanged).
        // Calling it here risks overwriting valid stored positions with uninitialized 
        // (0,0) values if a refresh happens before the GraphView has fully laid out.

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

        Dictionary<string, CardData> cardsById = CardGraph.BuildLookup(database, issue => Debug.LogWarning($"[CardGraphView] {issue}"));
        Dictionary<string, CardNode> nodesByCardId = new Dictionary<string, CardNode>();
        List<CardNode> nodeList = CreateNodes(cardsById, nodesByCardId);
        DrawEdges(nodeList, nodesByCardId);
        CreateSpeakerNodes();
        CreateResourceNodes();

        isPopulating = false;
    }

    private void LoadPositionsFromDatabase()
    {
        positionsByCardId.Clear();
        positionsByMemberId.Clear();
        positionsByResourceId.Clear();

        if (database?.editorGraphPositions != null)
        {
            foreach (NarrativeDatabase.CardGraphPosition entry in database.editorGraphPositions)
            {
                if (!string.IsNullOrEmpty(entry.cardId))
                {
                    positionsByCardId[entry.cardId] = entry.position;
                }
            }
        }

        if (database?.editorSpeakerPositions != null)
        {
            foreach (NarrativeDatabase.SpeakerGraphPosition entry in database.editorSpeakerPositions)
            {
                if (!string.IsNullOrEmpty(entry.memberId))
                {
                    positionsByMemberId[entry.memberId] = entry.position;
                }
            }
        }

        if (database?.editorResourcePositions != null)
        {
            foreach (NarrativeDatabase.ResourceGraphPosition entry in database.editorResourcePositions)
            {
                if (!string.IsNullOrEmpty(entry.resourceId))
                {
                    positionsByResourceId[entry.resourceId] = entry.position;
                }
            }
        }
    }

    private List<CardNode> CreateNodes(Dictionary<string, CardData> cardsById, Dictionary<string, CardNode> nodesByCardId)
    {
        List<CardNode> nodeList = new List<CardNode>();

        Dictionary<string, int> cardIdCounts = new Dictionary<string, int>();
        foreach (CardData c in database.cards)
        {
            if (c != null && !string.IsNullOrEmpty(c.cardId))
            {
                cardIdCounts.TryGetValue(c.cardId, out int existingCount);
                cardIdCounts[c.cardId] = existingCount + 1;
            }
        }

        int i = 0;
        foreach (CardData card in database.cards)
        {
            if (card == null)
            {
                continue;
            }

            bool isStartCard = !string.IsNullOrEmpty(database.startingCardId) && database.startingCardId == card.cardId;
            bool isDuplicateId = !string.IsNullOrEmpty(card.cardId)
                && cardIdCounts.TryGetValue(card.cardId, out int count)
                && count > 1;

            CardNode node = new CardNode(card, isStartCard, cardsById, this, isDuplicateId);
            node.SetPosition(new Rect(ResolveNodePosition(card, i), DefaultNodeSize));

            AddElement(node);
            nodeList.Add(node);

            if (!string.IsNullOrEmpty(card.cardId))
            {
                nodesByCardId.TryAdd(card.cardId, node);
            }

            i++;
        }

        return nodeList;
    }

    private Vector2 ResolveNodePosition(CardData card, int indexInList)
    {
        if (pendingNewCard != null && card == pendingNewCard)
        {
            pendingNewCard = null;
            Vector2 centerPos = ViewportCenterInContentSpace();
            SavePositionToDatabase(card.cardId, centerPos);
            return centerPos;
        }

        if (!string.IsNullOrEmpty(card.cardId) && positionsByCardId.TryGetValue(card.cardId, out Vector2 savedPos))
        {
            return savedPos;
        }

        int col = indexInList % GridColumns;
        int row = indexInList / GridColumns;
        Vector2 gridPos = new Vector2(col * GridSpacingX + 60, row * GridSpacingY + 60);
        SavePositionToDatabase(card.cardId, gridPos);
        return gridPos;
    }

    private Vector2 ViewportCenterInContentSpace()
    {
        Vector2 viewSize = layout.size == Vector2.zero ? FallbackViewSize : layout.size;
        Vector2 viewCenter = viewSize * 0.5f;
        Vector2 contentPos = contentViewContainer.WorldToLocal(this.LocalToWorld(viewCenter));
        return contentPos - DefaultNodeSize * 0.5f;
    }

    private void DrawEdges(List<CardNode> nodeList, Dictionary<string, CardNode> nodesByCardId)
    {
        foreach (CardNode sourceNode in nodeList)
        {
            CardData card = sourceNode.Card;
            if (card == null)
            {
                continue;
            }

            if (card.isLlmReactionCard)
            {
                TryConnect(sourceNode.ContinuePort, card.continueNextCardId, nodesByCardId);
            }
            else
            {
                TryConnect(sourceNode.LeftPort, card.leftNextCardId, nodesByCardId);
                TryConnect(sourceNode.RightPort, card.rightNextCardId, nodesByCardId);
            }
        }
    }

    private void TryConnect(Port outputPort, string targetCardId, Dictionary<string, CardNode> nodesByCardId)
    {
        if (!string.IsNullOrEmpty(targetCardId) && nodesByCardId.TryGetValue(targetCardId, out CardNode targetNode))
        {
            AddElement(outputPort.ConnectTo(targetNode.InputPort));
        }
    }

    public void SetStartingCard(CardData card)
    {
        if (database == null || card == null)
        {
            return;
        }

        Undo.RecordObject(database, "Set Starting Card");
        database.startingCardId = card.cardId;
        EditorUtility.SetDirty(database);
        window.PopulateGraph();
    }

    private static readonly Vector2 DefaultSpeakerNodeSize = new Vector2(240, 170);

    private void CreateSpeakerNodes()
    {
        if (database?.speakers == null)
        {
            return;
        }

        int i = 0;
        foreach (CouncilMemberData speaker in database.speakers)
        {
            if (speaker == null)
            {
                continue;
            }

            SpeakerNode speakerNode = new SpeakerNode(speaker, this);
            Vector2 pos = ResolveSpeakerNodePosition(speaker, i);
            speakerNode.SetPosition(new Rect(pos, DefaultSpeakerNodeSize));
            AddElement(speakerNode);
            i++;
        }
    }

    private Vector2 ResolveSpeakerNodePosition(CouncilMemberData speaker, int indexInList)
    {
        string id = !string.IsNullOrEmpty(speaker.memberId) ? speaker.memberId : speaker.name;

        if (!string.IsNullOrEmpty(pendingNewSpeakerId) && id == pendingNewSpeakerId)
        {
            pendingNewSpeakerId = null;
            Vector2 centerPos = ViewportCenterInContentSpace();
            SaveSpeakerPositionToDatabase(id, centerPos);
            return centerPos;
        }

        if (!string.IsNullOrEmpty(id) && positionsByMemberId.TryGetValue(id, out Vector2 savedPos))
        {
            return savedPos;
        }

        Vector2 defaultPos = new Vector2(-260, indexInList * 210 + 60);
        SaveSpeakerPositionToDatabase(id, defaultPos);
        return defaultPos;
    }

    private static readonly Vector2 DefaultResourceNodeSize = new Vector2(200, 150);

    private void CreateResourceNodes()
    {
        if (database?.resourceCatalog?.resources == null)
        {
            return;
        }

        int i = 0;
        foreach (ResourceData resource in database.resourceCatalog.resources)
        {
            if (resource == null || string.IsNullOrEmpty(resource.id))
            {
                continue;
            }

            ResourceNode resourceNode = new ResourceNode(resource);
            Vector2 pos = ResolveResourceNodePosition(resource.id, i);
            resourceNode.SetPosition(new Rect(pos, DefaultResourceNodeSize));
            AddElement(resourceNode);
            i++;
        }
    }

    private Vector2 ResolveResourceNodePosition(string resourceId, int indexInList)
    {
        if (!string.IsNullOrEmpty(pendingNewResourceId) && resourceId == pendingNewResourceId)
        {
            pendingNewResourceId = null;
            Vector2 centerPos = ViewportCenterInContentSpace();
            SaveResourcePositionToDatabase(resourceId, centerPos);
            return centerPos;
        }

        if (positionsByResourceId.TryGetValue(resourceId, out Vector2 savedPos))
        {
            return savedPos;
        }

        Vector2 defaultPos = new Vector2(-520, indexInList * 190 + 60);
        SaveResourcePositionToDatabase(resourceId, defaultPos);
        return defaultPos;
    }

    private void SaveCurrentNodePositions()
    {
        if (database == null)
        {
            return;
        }

        foreach (CardNode node in graphElements.OfType<CardNode>())
        {
            if (node.Card != null && !string.IsNullOrEmpty(node.Card.cardId))
            {
                SavePositionToDatabase(node.Card.cardId, node.GetPosition().position);
            }
        }

        foreach (SpeakerNode node in graphElements.OfType<SpeakerNode>())
        {
            if (node.Speaker != null)
            {
                string id = !string.IsNullOrEmpty(node.Speaker.memberId) ? node.Speaker.memberId : node.Speaker.name;
                if (!string.IsNullOrEmpty(id))
                {
                    SaveSpeakerPositionToDatabase(id, node.GetPosition().position);
                }
            }
        }

        foreach (ResourceNode node in graphElements.OfType<ResourceNode>())
        {
            if (node.Data != null && !string.IsNullOrEmpty(node.Data.id))
            {
                SaveResourcePositionToDatabase(node.Data.id, node.GetPosition().position);
            }
        }
    }

   // Shared save logic for all three graph-position lists (card/speaker/resource). Writes only
   // when the position actually changed (avoids needless SetDirty on every repopulate), and
   // records Undo before mutating so node drags participate in Unity's undo/redo like every other
   // mutation in this tool does.
   private void SaveNodePosition<TEntry>(
       List<TEntry> list,
       Dictionary<string, Vector2> mirror,
       string id,
       Vector2 position,
       Func<TEntry, string> getId,
       Func<string, Vector2, TEntry> makeEntry,
       string undoLabel)
   {
       if (database == null || string.IsNullOrEmpty(id) || list == null)
       {
           return;
       }

       if (mirror.TryGetValue(id, out Vector2 existing) && existing == position)
       {
           return;
       }

       mirror[id] = position;

       int index = list.FindIndex(e => getId(e) == id);

       Undo.RecordObject(database, undoLabel);

       if (index >= 0)
       {
           list[index] = makeEntry(id, position);
       }
       else
       {
           list.Add(makeEntry(id, position));
       }

       EditorUtility.SetDirty(database);
   }

   // Shared remove logic for all three graph-position lists.
   private void RemoveNodePosition<TEntry>(
       List<TEntry> list,
       Dictionary<string, Vector2> mirror,
       string id,
       Func<TEntry, string> getId,
       string undoLabel)
   {
       if (list == null || string.IsNullOrEmpty(id))
       {
           return;
       }

       mirror.Remove(id);
       int index = list.FindIndex(e => getId(e) == id);
       if (index >= 0)
       {
           Undo.RecordObject(database, undoLabel);
           list.RemoveAt(index);
           EditorUtility.SetDirty(database);
       }
   }

    // Writes a single card's position into both the in-memory mirror and the database asset, so
    // a drag persists to disk (and survives closing/reopening the window) instead of living only
    // in this CardGraphView instance, which is destroyed when the EditorWindow closes.
    private void SavePositionToDatabase(string cardId, Vector2 position)
    {
        if (database == null || string.IsNullOrEmpty(cardId))
        {
            return;
        }

        database.editorGraphPositions ??= new List<NarrativeDatabase.CardGraphPosition>();
        SaveNodePosition(
            database.editorGraphPositions,
            positionsByCardId,
            cardId,
            position,
            e => e.cardId,
            (id, pos) => new NarrativeDatabase.CardGraphPosition { cardId = id, position = pos },
            "Move Card Node");
    }

    private void RemovePositionFromDatabase(string cardId)
    {
        if (database == null)
        {
            return;
        }

        RemoveNodePosition(
            database.editorGraphPositions,
            positionsByCardId,
            cardId,
            e => e.cardId,
            "Delete Card Node Position");
    }

    private void SaveSpeakerPositionToDatabase(string memberId, Vector2 position)
    {
        if (database == null || string.IsNullOrEmpty(memberId))
        {
            return;
        }

        database.editorSpeakerPositions ??= new List<NarrativeDatabase.SpeakerGraphPosition>();
        SaveNodePosition(
            database.editorSpeakerPositions,
            positionsByMemberId,
            memberId,
            position,
            e => e.memberId,
            (id, pos) => new NarrativeDatabase.SpeakerGraphPosition { memberId = id, position = pos },
            "Move Speaker Node");
    }

    private void RemoveSpeakerPositionFromDatabase(string memberId)
    {
        if (database == null)
        {
            return;
        }

        RemoveNodePosition(
            database.editorSpeakerPositions,
            positionsByMemberId,
            memberId,
            e => e.memberId,
            "Delete Speaker Node Position");
    }

    private void SaveResourcePositionToDatabase(string resourceId, Vector2 position)
    {
        if (database == null || string.IsNullOrEmpty(resourceId))
        {
            return;
        }

        database.editorResourcePositions ??= new List<NarrativeDatabase.ResourceGraphPosition>();
        SaveNodePosition(
            database.editorResourcePositions,
            positionsByResourceId,
            resourceId,
            position,
            e => e.resourceId,
            (id, pos) => new NarrativeDatabase.ResourceGraphPosition { resourceId = id, position = pos },
            "Move Resource Node");
    }

    private void RemoveResourcePositionFromDatabase(string resourceId)
    {
        if (database == null)
        {
            return;
        }

        RemoveNodePosition(
            database.editorResourcePositions,
            positionsByResourceId,
            resourceId,
            e => e.resourceId,
            "Delete Resource Node Position");
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if (database == null) return;

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
            SaveCurrentNodePositions();
        }

        // Edge/deletion handling below can mutate CardData referenced by other still-being-processed
        // elements in this same change, so we never repopulate mid-loop here — a single PopulateGraph()
        // runs once at the end, after every entry in the change has been applied.
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
            // PopulateGraph() below fully tears down and rebuilds every node/edge from CardData,
            // which recreates the correct edge from the data we just wrote. GraphView's own edge-
            // connector and delete logic still expect to add/remove whatever is left in
            // change.edgesToCreate / change.elementsToRemove *themselves*, right after this
            // callback returns. Against a graph we've already rebuilt, those are stale references
            // into torn-down nodes and ports — that's exactly what produced the dangling duplicate
            // edge after connecting two cards, and the EdgeManipulator NullReferenceException the
            // next time you dragged a node (it was still tracking the orphaned edge). Clearing
            // both lists here stops the framework from double-processing them.
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
        if (sourceCard.isLlmReactionCard && edge.output == sourceNode.ContinuePort)
        {
            sourceCard.continueNextCardId = targetCard.cardId;
        }
        else if (edge.output == sourceNode.LeftPort)
        {
            sourceCard.leftNextCardId = targetCard.cardId;
        }
        else if (edge.output == sourceNode.RightPort)
        {
            sourceCard.rightNextCardId = targetCard.cardId;
        }
        EditorUtility.SetDirty(sourceCard);
        return true;
    }

    // Returns true if the graph needs a repopulate afterward (a link was cleared or a card was deleted).
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
                        // User cancelled the confirmation dialog - keep the node in the graph.
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
        if (sourceCard.isLlmReactionCard && edge.output == sourceNode.ContinuePort)
        {
            sourceCard.continueNextCardId = string.Empty;
        }
        else if (edge.output == sourceNode.LeftPort)
        {
            sourceCard.leftNextCardId = string.Empty;
        }
        else if (edge.output == sourceNode.RightPort)
        {
            sourceCard.rightNextCardId = string.Empty;
        }
        EditorUtility.SetDirty(sourceCard);
        return true;
    }

    // Returns false (and leaves the card untouched) if the user cancelled the confirmation.
    private bool TryDeleteCard(CardNode cardNode)
    {
        CardData cardToDelete = cardNode.Card;
        if (cardToDelete == null)
        {
            return true;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Card",
            $"Delete '{cardToDelete.name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return false;
        }

        string deletedId = cardToDelete.cardId;
        RemovePositionFromDatabase(deletedId);
        ClearIncomingLinks(deletedId, cardToDelete);

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
        CouncilMemberData speakerToDelete = speakerNode.Speaker;
        if (speakerToDelete == null)
        {
            return true;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Speaker",
            $"Delete Speaker '{speakerToDelete.name}'? This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return false;
        }

        string memberId = speakerToDelete.memberId;
        RemoveSpeakerPositionFromDatabase(memberId);

        if (database?.cards != null && !string.IsNullOrEmpty(memberId))
        {
            foreach (CardData c in database.cards)
            {
                if (c != null && c.speakerId == memberId)
                {
                    Undo.RecordObject(c, "Clear Speaker Assignment");
                    c.speakerId = string.Empty;
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

        string resourceId = dataToDelete.id;
        bool confirm = EditorUtility.DisplayDialog(
            "Delete Resource",
            $"Delete resource asset '{resourceId}'? This will remove it from the catalog and delete the file. This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return false;
        }

        RemoveResourcePositionFromDatabase(resourceId);

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

    private void ClearIncomingLinks(string deletedId, CardData cardToDelete)
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

            if (other.leftNextCardId == deletedId)
            {
                Undo.RecordObject(other, "Clear Card Link");
                other.leftNextCardId = string.Empty;
                EditorUtility.SetDirty(other);
            }

            if (other.rightNextCardId == deletedId)
            {
                Undo.RecordObject(other, "Clear Card Link");
                other.rightNextCardId = string.Empty;
                EditorUtility.SetDirty(other);
            }

            if (other.continueNextCardId == deletedId)
            {
                Undo.RecordObject(other, "Clear Card Link");
                other.continueNextCardId = string.Empty;
                EditorUtility.SetDirty(other);
            }
        }

        if (database.startingCardId == deletedId)
        {
            Undo.RecordObject(database, "Clear Starting Card");
            database.startingCardId = string.Empty;
        }

        Undo.RecordObject(database, "Remove Card From Database");
        database.cards.Remove(cardToDelete);
        EditorUtility.SetDirty(database);
    }
    }
    }