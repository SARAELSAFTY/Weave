using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Editor window shell: owns the toolbar (database picker, starting-card picker, new-card
// and validate buttons) and hosts a CardGraphView for the actual node graph.
public class CardGraphWindow : EditorWindow
{
    private const string NewCardFolder = "Assets/Game/Data/Cards";

    private NarrativeDatabase currentDatabase;
    private CardGraphView graphView;
    private Button newCardButton;
    private Button validateButton;
    private ObjectField databaseField;
    private DropdownField startingCardDropdown;

    [MenuItem("Weave/Card Graph")]
    public static void OpenWindow()
    {
        CardGraphWindow window = GetWindow<CardGraphWindow>("Card Graph");
        window.minSize = new Vector2(900, 650);
        window.titleContent = new GUIContent("Card Graph", EditorGUIUtility.IconContent("d_Project").image);
    }

    // Called by CardDataEditor / CardAssetPostprocessor whenever a card changes outside this window.
    public static void RefreshOpenWindows()
    {
        foreach (CardGraphWindow window in Resources.FindObjectsOfTypeAll<CardGraphWindow>())
        {
            window.PopulateGraph();
        }
    }

    private void OnEnable()
    {
        ConstructUI();
    }

    private void ConstructUI()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(BuildToolbar());

        graphView = new CardGraphView(this);
        graphView.StretchToParentSize();
        graphView.style.top = 36;
        rootVisualElement.Add(graphView);

        UpdateToolbarState();
        PopulateGraph();
    }

    private Toolbar BuildToolbar()
    {
        Toolbar toolbar = new Toolbar();
        toolbar.style.height = 36;
        toolbar.style.paddingLeft = 8;
        toolbar.style.paddingRight = 8;
        toolbar.style.paddingTop = 4;
        toolbar.style.paddingBottom = 4;
        toolbar.style.backgroundColor = new StyleColor(new Color(0.13f, 0.14f, 0.16f));
        toolbar.style.borderBottomWidth = 1;
        toolbar.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.23f, 0.26f));

        databaseField = new ObjectField("Database:")
        {
            objectType = typeof(NarrativeDatabase),
            allowSceneObjects = false,
            value = currentDatabase
        };
        databaseField.style.width = 260;
        databaseField.style.marginRight = 12;
        databaseField.RegisterValueChangedCallback(evt =>
        {
            currentDatabase = evt.newValue as NarrativeDatabase;
            UpdateToolbarState();
            PopulateGraph();
        });
        toolbar.Add(databaseField);

        startingCardDropdown = new DropdownField("Starting Card:", new List<string> { "(None)" }, 0);
        startingCardDropdown.style.width = 240;
        startingCardDropdown.style.marginRight = 12;
        startingCardDropdown.RegisterValueChangedCallback(evt => SetStartingCardFromDropdown(evt.newValue));
        toolbar.Add(startingCardDropdown);

        VisualElement spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        toolbar.Add(spacer);

        newCardButton = new Button(OnCreateNewCardClicked) { text = "+ New Card" };
        newCardButton.style.height = 24;
        newCardButton.style.paddingLeft = 12;
        newCardButton.style.paddingRight = 12;
        newCardButton.style.marginRight = 6;
        newCardButton.style.backgroundColor = new StyleColor(new Color(0.10f, 0.45f, 0.65f));
        newCardButton.style.color = new StyleColor(Color.white);
        newCardButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        toolbar.Add(newCardButton);

        validateButton = new Button(OnValidateClicked) { text = "Validate" };
        validateButton.style.height = 24;
        validateButton.style.paddingLeft = 10;
        validateButton.style.paddingRight = 10;
        toolbar.Add(validateButton);

        return toolbar;
    }

    private void SetStartingCardFromDropdown(string selected)
    {
        if (currentDatabase == null)
        {
            return;
        }

        string newStartId = (selected == "(None)" || string.IsNullOrEmpty(selected)) ? string.Empty : selected;
        if (currentDatabase.startingCardId == newStartId)
        {
            return;
        }

        Undo.RecordObject(currentDatabase, "Set Starting Card");
        currentDatabase.startingCardId = newStartId;
        EditorUtility.SetDirty(currentDatabase);
        PopulateGraph();
    }

    private void UpdateToolbarState()
    {
        bool hasDb = currentDatabase != null;
        newCardButton?.SetEnabled(hasDb);
        validateButton?.SetEnabled(hasDb);
        startingCardDropdown?.SetEnabled(hasDb);

        if (hasDb)
        {
            RefreshStartingCardDropdownOptions();
        }
    }

    public void RefreshStartingCardDropdownOptions()
    {
        if (currentDatabase == null || startingCardDropdown == null)
        {
            return;
        }

        List<string> options = new List<string> { "(None)" };
        if (currentDatabase.cards != null)
        {
            foreach (CardData card in currentDatabase.cards)
            {
                string id = card != null ? (!string.IsNullOrEmpty(card.cardId) ? card.cardId : card.name) : null;
                if (!string.IsNullOrEmpty(id) && !options.Contains(id))
                {
                    options.Add(id);
                }
            }
        }

        startingCardDropdown.choices = options;
        string currentStartId = currentDatabase.startingCardId;
        startingCardDropdown.SetValueWithoutNotify(
            !string.IsNullOrEmpty(currentStartId) && options.Contains(currentStartId) ? currentStartId : "(None)");
    }

    public void PopulateGraph()
    {
        RefreshStartingCardDropdownOptions();
        graphView?.Populate(currentDatabase);
    }

    private void OnCreateNewCardClicked()
    {
        if (currentDatabase == null)
        {
            return;
        }

        if (!Directory.Exists(NewCardFolder))
        {
            Directory.CreateDirectory(NewCardFolder);
            AssetDatabase.Refresh();
        }

        string cardName = FindNextUnusedCardName();
        string assetPath = Path.Combine(NewCardFolder, cardName + ".asset");

        CardData newCard = ScriptableObject.CreateInstance<CardData>();
        newCard.name = cardName;
        newCard.cardId = cardName;
        AssetDatabase.CreateAsset(newCard, assetPath);

        Undo.RecordObject(currentDatabase, "Add New Card");
        currentDatabase.cards ??= new List<CardData>();
        currentDatabase.cards.Add(newCard);

        if (string.IsNullOrEmpty(currentDatabase.startingCardId))
        {
            currentDatabase.startingCardId = cardName;
        }

        EditorUtility.SetDirty(currentDatabase);
        AssetDatabase.SaveAssets();

        graphView?.SetPendingNewCard(newCard);
        PopulateGraph();

        Selection.activeObject = newCard;
        EditorGUIUtility.PingObject(newCard);
    }

    private string FindNextUnusedCardName()
    {
        int index = 1;
        string cardName;
        do
        {
            cardName = $"Card_{index:D3}";
            index++;
        }
        while (CardNameInUse(cardName));

        return cardName;
    }

    private bool CardNameInUse(string cardName)
    {
        bool inDatabase = currentDatabase.cards != null &&
            currentDatabase.cards.Exists(c => c != null && (c.cardId == cardName || c.name == cardName));
        bool onDisk = File.Exists(Path.Combine(NewCardFolder, cardName + ".asset"));
        return inDatabase || onDisk;
    }

    private void OnValidateClicked()
    {
        if (currentDatabase == null)
        {
            return;
        }

        EditorApplication.ExecuteMenuItem("CONTEXT/NarrativeDatabase/Validate Card Links");
        Debug.Log($"[Card Graph] Validated '{currentDatabase.name}'. Check Console for detailed diagnostic log.");
    }
}
