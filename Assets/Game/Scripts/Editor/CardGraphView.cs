using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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

    // Set before Populate() so the new card node is placed at the viewport centre.
    private CardData pendingNewCard;

    public void SetPendingNewCard(CardData card)
    {
        pendingNewCard = card;
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

        emptyNoticeLabel = new Label
        {
            style =
            {
                fontSize = 15,
                color = new StyleColor(new Color(0.65f, 0.65f, 0.70f)),
                unityTextAlign = TextAnchor.MiddleCenter,
                marginTop = 220
            }
        };
        Add(emptyNoticeLabel);

        graphViewChanged = OnGraphViewChanged;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.Where(p => p.node != startPort.node && p.direction != startPort.direction).ToList();
    }

    public void Populate(NarrativeDatabase db)
    {
        SaveCurrentNodePositions();

        isPopulating = true;
        database = db;
        LoadPositionsFromDatabase();

        foreach (GraphElement element in graphElements.ToList())
        {
            RemoveElement(element);
        }

        if (database == null || database.cards == null || database.cards.Count == 0)
        {
            emptyNoticeLabel.style.display = DisplayStyle.Flex;
            emptyNoticeLabel.text = database == null
                ? "Select a NarrativeDatabase asset in the top toolbar to view and edit the card graph."
                : "Database has no cards. Click '+ New Card' in the toolbar to create one.";
            isPopulating = false;
            return;
        }

        emptyNoticeLabel.style.display = DisplayStyle.None;

        foreach (CardData c in database.cards)
        {
            CardGraph.SyncIdToAssetName(c);
        }

        Dictionary<string, CardData> cardsById = CardGraph.BuildLookup(database);
        Dictionary<string, CardNode> nodesByCardId = new Dictionary<string, CardNode>();
        List<CardNode> nodeList = CreateNodes(cardsById, nodesByCardId);
        DrawEdges(nodeList, nodesByCardId);

        isPopulating = false;
    }

    private void LoadPositionsFromDatabase()
    {
        positionsByCardId.Clear();
        if (database?.editorGraphPositions == null)
        {
            return;
        }

        foreach (NarrativeDatabase.CardGraphPosition entry in database.editorGraphPositions)
        {
            if (!string.IsNullOrEmpty(entry.cardId))
            {
                positionsByCardId[entry.cardId] = entry.position;
            }
        }
    }

    private List<CardNode> CreateNodes(Dictionary<string, CardData> cardsById, Dictionary<string, CardNode> nodesByCardId)
    {
        List<CardNode> nodeList = new List<CardNode>();

        int i = 0;
        foreach (CardData card in database.cards)
        {
            if (card == null)
            {
                continue;
            }

            bool isStartCard = !string.IsNullOrEmpty(database.startingCardId) && database.startingCardId == card.cardId;
            CardNode node = new CardNode(card, isStartCard, cardsById, this);
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

            TryConnect(sourceNode.LeftPort, card.leftNextCardId, nodesByCardId);
            TryConnect(sourceNode.RightPort, card.rightNextCardId, nodesByCardId);
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

        positionsByCardId[cardId] = position;

        database.editorGraphPositions ??= new List<NarrativeDatabase.CardGraphPosition>();
        int index = database.editorGraphPositions.FindIndex(e => e.cardId == cardId);
        NarrativeDatabase.CardGraphPosition entry = new NarrativeDatabase.CardGraphPosition
        {
            cardId = cardId,
            position = position
        };

        if (index >= 0)
        {
            database.editorGraphPositions[index] = entry;
        }
        else
        {
            database.editorGraphPositions.Add(entry);
        }

        EditorUtility.SetDirty(database);
    }

    private void RemovePositionFromDatabase(string cardId)
    {
        if (database?.editorGraphPositions == null || string.IsNullOrEmpty(cardId))
        {
            return;
        }

        positionsByCardId.Remove(cardId);
        int index = database.editorGraphPositions.FindIndex(e => e.cardId == cardId);
        if (index >= 0)
        {
            database.editorGraphPositions.RemoveAt(index);
            EditorUtility.SetDirty(database);
        }
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
        if (edge.output == sourceNode.LeftPort)
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
        if (edge.output == sourceNode.LeftPort)
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