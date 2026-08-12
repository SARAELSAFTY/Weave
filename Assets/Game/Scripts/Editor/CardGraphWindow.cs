using System.Collections.Generic;
using System.IO;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>Card Graph editor window: toolbar + hosted <see cref="CardGraphView"/>.</summary>
    public class CardGraphWindow : EditorWindow
    {
        private const string NewCardFolder = "Assets/Game/Data/Cards";
        private const string NewSpeakerFolder = "Assets/Game/Data/Speakers";

        private NarrativeDatabase currentDatabase;
        private CardGraphView graphView;
        private Button newDatabaseButton;
        private Button newCardButton;
        private Button newSpeakerButton;
        private Button newCatalogButton;
        private Button newResourceButton;
        private ObjectField databaseField;
        private PopupField<string> startingCardDropdown;

        [MenuItem("Weave/Card Graph")]
        public static void OpenWindow()
        {
            CardGraphWindow window = GetWindow<CardGraphWindow>("Card Graph");
            window.minSize = new Vector2(900, 650);
            window.titleContent = new GUIContent("Card Graph", EditorGUIUtility.IconContent("d_Project").image);
        }

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
            databaseField.style.width = 240;
            databaseField.style.marginRight = 6;
            databaseField.RegisterValueChangedCallback(evt =>
            {
                currentDatabase = evt.newValue as NarrativeDatabase;
                UpdateToolbarState();
                PopulateGraph();
            });
            toolbar.Add(databaseField);

            newDatabaseButton = new Button(OnCreateNewDatabaseClicked) { text = "+ New DB" };
            newDatabaseButton.style.height = 24;
            newDatabaseButton.style.paddingLeft = 8;
            newDatabaseButton.style.paddingRight = 8;
            newDatabaseButton.style.marginRight = 12;
            newDatabaseButton.style.backgroundColor = new StyleColor(new Color(0.20f, 0.55f, 0.35f));
            newDatabaseButton.style.color = new StyleColor(Color.white);
            newDatabaseButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(newDatabaseButton);

            startingCardDropdown = new PopupField<string>("Starting Card:", new List<string> { "(None)" }, 0);
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

            newSpeakerButton = new Button(OnCreateNewSpeakerClicked) { text = "+ New Speaker" };
            newSpeakerButton.style.height = 24;
            newSpeakerButton.style.paddingLeft = 8;
            newSpeakerButton.style.paddingRight = 8;
            newSpeakerButton.style.marginRight = 6;
            newSpeakerButton.style.backgroundColor = new StyleColor(new Color(0.10f, 0.45f, 0.65f));
            newSpeakerButton.style.color = new StyleColor(Color.white);
            newSpeakerButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(newSpeakerButton);

            newCatalogButton = new Button(OnCreateNewCatalogClicked) { text = "+ New Catalog" };
            newCatalogButton.style.height = 24;
            newCatalogButton.style.paddingLeft = 8;
            newCatalogButton.style.paddingRight = 8;
            newCatalogButton.style.marginRight = 6;
            newCatalogButton.style.backgroundColor = new StyleColor(new Color(0.15f, 0.50f, 0.40f));
            newCatalogButton.style.color = new StyleColor(Color.white);
            newCatalogButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(newCatalogButton);

            newResourceButton = new Button(() => CreateResourceAt(null)) { text = "+ New Resource" };
            newResourceButton.style.height = 24;
            newResourceButton.style.paddingLeft = 8;
            newResourceButton.style.paddingRight = 8;
            newResourceButton.style.marginRight = 6;
            newResourceButton.style.backgroundColor = new StyleColor(new Color(0.15f, 0.50f, 0.40f));
            newResourceButton.style.color = new StyleColor(Color.white);
            newResourceButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            toolbar.Add(newResourceButton);

            return toolbar;
        }

        private void SetStartingCardFromDropdown(string selected)
        {
            if (currentDatabase == null) return;

            CardData newStartCard = null;
            if (selected != "(None)" && currentDatabase.cards != null)
            {
                newStartCard = currentDatabase.cards.Find(c => c != null && c.AssetName == selected);
            }

            if (currentDatabase.startingCard == newStartCard) return;

            Undo.RecordObject(currentDatabase, "Set Starting Card");
            currentDatabase.startingCard = newStartCard;
            EditorUtility.SetDirty(currentDatabase);
            PopulateGraph();
        }

        private void UpdateToolbarState()
        {
            bool hasDb = currentDatabase != null;
            newCardButton?.SetEnabled(hasDb);
            newSpeakerButton?.SetEnabled(hasDb);
            startingCardDropdown?.SetEnabled(hasDb);

            bool hasCatalog = hasDb && currentDatabase.resourceCatalog != null;
            newCatalogButton?.SetEnabled(hasDb && !hasCatalog);
            newResourceButton?.SetEnabled(hasCatalog);

            if (hasDb)
            {
                RefreshStartingCardDropdownOptions();
            }
        }

        public void RefreshStartingCardDropdownOptions()
        {
            if (currentDatabase == null || startingCardDropdown == null) return;

            List<string> options = new List<string> { "(None)" };
            if (currentDatabase.cards != null)
            {
                foreach (CardData card in currentDatabase.cards)
                {
                    if (card != null && !options.Contains(card.AssetName))
                    {
                        options.Add(card.AssetName);
                    }
                }
            }

            startingCardDropdown.choices = options;
            string currentStartName = currentDatabase.startingCard != null ? currentDatabase.startingCard.AssetName : "(None)";
            startingCardDropdown.SetValueWithoutNotify(options.Contains(currentStartName) ? currentStartName : "(None)");
        }

        public void PopulateGraph()
        {
            RefreshStartingCardDropdownOptions();
            graphView?.Populate(currentDatabase);
        }

        private void OnCreateNewCardClicked()
        {
            CreateCardAt(null);
        }

        public void CreateCardAt(Vector2? windowPosition)
        {
            if (currentDatabase == null) return;

            string cardName = FindNextUnusedCardName();
            CardData newCard = CardGraphEditor.CreateCard(NewCardFolder, cardName);
            if (newCard == null) return;

            Undo.RecordObject(currentDatabase, "Add New Card");
            currentDatabase.cards ??= new List<CardData>();
            currentDatabase.cards.Add(newCard);

            if (currentDatabase.startingCard == null)
            {
                currentDatabase.startingCard = newCard;
            }

            if (windowPosition.HasValue)
            {
                currentDatabase.editorGraphPositions ??= new List<NarrativeDatabase.CardGraphPosition>();
                currentDatabase.editorGraphPositions.Add(new NarrativeDatabase.CardGraphPosition
                {
                    card = newCard,
                    position = windowPosition.Value
                });
            }
            else
            {
                graphView?.SetPendingNewCard(newCard);
            }

            EditorUtility.SetDirty(currentDatabase);
            AssetDatabase.SaveAssets();

            PopulateGraph();

            Selection.activeObject = newCard;
            EditorGUIUtility.PingObject(newCard);
        }

        private string FindNextUnusedCardName()
        {
            return CardGraphEditor.FindNextUnusedName("Scene_Speaker_Slug", CardNameInUse);
        }

        private bool CardNameInUse(string cardName)
        {
            bool inDatabase = currentDatabase.cards != null &&
                currentDatabase.cards.Exists(c => c != null && c.name == cardName);
            bool onDisk = File.Exists(Path.Combine(NewCardFolder, cardName + ".asset"));
            return inDatabase || onDisk;
        }

        private void OnCreateNewSpeakerClicked()
        {
            CreateSpeakerAt(null);
        }

        public void CreateSpeakerAt(Vector2? windowPosition)
        {
            if (currentDatabase == null) return;

            string speakerName = FindNextUnusedSpeakerName();
            SpeakerData newSpeaker = CardGraphEditor.CreateSpeaker(NewSpeakerFolder, speakerName);
            if (newSpeaker == null) return;

            Undo.RecordObject(currentDatabase, "Add New Speaker");
            currentDatabase.speakers ??= new List<SpeakerData>();
            currentDatabase.speakers.Add(newSpeaker);

            if (windowPosition.HasValue)
            {
                currentDatabase.editorSpeakerPositions ??= new List<NarrativeDatabase.SpeakerGraphPosition>();
                currentDatabase.editorSpeakerPositions.Add(new NarrativeDatabase.SpeakerGraphPosition
                {
                    speaker = newSpeaker,
                    position = windowPosition.Value
                });
            }
            else
            {
                graphView?.SetPendingNewSpeaker(newSpeaker);
            }

            CardData selectedCard = Selection.activeObject as CardData;
            if (selectedCard != null)
            {
                Undo.RecordObject(selectedCard, "Assign New Speaker");
                selectedCard.speaker = newSpeaker;
                EditorUtility.SetDirty(selectedCard);
            }

            EditorUtility.SetDirty(currentDatabase);
            AssetDatabase.SaveAssets();

            PopulateGraph();

            Selection.activeObject = newSpeaker;
            EditorGUIUtility.PingObject(newSpeaker);
        }

        public void CreateResourceAt(Vector2? windowPosition)
        {
            if (currentDatabase == null || currentDatabase.resourceCatalog == null) return;

            ResourceCatalog catalog = currentDatabase.resourceCatalog;
            ResourceData newResource = CardGraphEditor.CreateResource(catalog);
            if (newResource == null) return;

            if (windowPosition.HasValue)
            {
                currentDatabase.editorResourcePositions ??= new List<NarrativeDatabase.ResourceGraphPosition>();
                currentDatabase.editorResourcePositions.Add(new NarrativeDatabase.ResourceGraphPosition
                {
                    resource = newResource,
                    position = windowPosition.Value
                });
                EditorUtility.SetDirty(currentDatabase);
            }
            else
            {
                graphView?.SetPendingNewResource(newResource);
            }

            AssetDatabase.SaveAssets();

            PopulateGraph();

            Selection.activeObject = catalog;
            EditorGUIUtility.PingObject(catalog);
        }

        public void OnCreateNewDatabaseClicked()
        {
            string databaseFolder = "Assets/Game/Data";
            string databaseName = FindNextUnusedDatabaseName(databaseFolder);
            NarrativeDatabase newDb = CardGraphEditor.CreateDatabase(databaseFolder, databaseName);
            if (newDb == null) return;
            AssetDatabase.SaveAssets();

            currentDatabase = newDb;
            if (databaseField != null)
            {
                databaseField.value = newDb;
            }

            UpdateToolbarState();
            PopulateGraph();

            Selection.activeObject = newDb;
            EditorGUIUtility.PingObject(newDb);
        }

        private static string FindNextUnusedDatabaseName(string folder)
        {
            return CardGraphEditor.FindNextUnusedName("NarrativeDatabase",
                name => File.Exists(Path.Combine(folder, name + ".asset")), startIndex: 1, digitFormat: "D3");
        }

        public void OnCreateNewCatalogClicked()
        {
            if (currentDatabase == null) return;

            string folder = "Assets/Game/Data";
            string catalogName = FindNextUnusedCatalogName(folder);
            ResourceCatalog newCatalog = CardGraphEditor.CreateCatalog(folder, catalogName);
            if (newCatalog == null) return;

            Undo.RecordObject(currentDatabase, "Assign Resource Catalog");
            currentDatabase.resourceCatalog = newCatalog;
            EditorUtility.SetDirty(currentDatabase);
            AssetDatabase.SaveAssets();

            UpdateToolbarState();
            PopulateGraph();

            Selection.activeObject = newCatalog;
            EditorGUIUtility.PingObject(newCatalog);
        }

        private static string FindNextUnusedCatalogName(string folder)
        {
            return CardGraphEditor.FindNextUnusedName("ResourceCatalog",
                name => File.Exists(Path.Combine(folder, name + ".asset")), startIndex: 1, digitFormat: "D3");
        }

        private string FindNextUnusedSpeakerName()
        {
            return CardGraphEditor.FindNextUnusedName("Spk_NewSpeaker", SpeakerNameInUse);
        }

        private bool SpeakerNameInUse(string speakerName)
        {
            bool inDatabase = currentDatabase.speakers != null &&
                currentDatabase.speakers.Exists(s => s != null && s.name == speakerName);
            bool onDisk = File.Exists(Path.Combine(NewSpeakerFolder, speakerName + ".asset"));
            return inDatabase || onDisk;
        }
    }
}
