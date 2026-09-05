using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Scripts.Definitions;
using Game.Scripts.Narrative;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Static utility class for creating narrative assets (cards, speakers, resources, databases, catalogs) and generating unique asset names within the card graph editor.
    /// </summary>
    public static class CardGraphEditor
    {
        /// <summary>
        /// Finds the next unused name by appending an incrementing numeric suffix (e.g., "Base_02", "Base_03") until the predicate returns false.
        /// </summary>
        /// <param name="baseName">The base name without suffix.</param>
        /// <param name="nameInUse">Predicate that returns true if a candidate name is already taken.</param>
        /// <param name="startIndex">First numeric suffix to try.</param>
        /// <param name="digitFormat">Format string for the numeric suffix (e.g., "D2" for zero-padded two digits).</param>
        /// <returns>The first candidate name not reported as in use.</returns>
        public static string FindNextUnusedName(string baseName, Func<string, bool> nameInUse, int startIndex = 2, string digitFormat = "D2")
        {
            if (!nameInUse(baseName)) return baseName;

            int index = startIndex;
            string candidate;
            do
            {
                candidate = $"{baseName}_{index.ToString(digitFormat)}";
                index++;
            }
            while (nameInUse(candidate));

            return candidate;
        }

        /// <summary>
        /// Creates a new <see cref="ResourceData"/> asset inside a "Resources" subfolder next to the catalog, adds it to the catalog's resource list with undo support, and returns it.
        /// </summary>
        /// <param name="catalog">The resource catalog to add the new resource to.</param>
        /// <returns>The newly created resource, or null if creation failed.</returns>
        public static ResourceData CreateResource(ResourceCatalog catalog)
        {
            if (catalog == null)
            {
                return null;
            }

            string name = FindNextUnusedResourceName(catalog);

            ResourceData data = ScriptableObject.CreateInstance<ResourceData>();
            data.assetName = name;
            data.name = name;

            string catalogPath = AssetDatabase.GetAssetPath(catalog);
            string directory = string.IsNullOrEmpty(catalogPath) ? "Assets" : Path.GetDirectoryName(catalogPath);
            string resourcesDirectory = Path.Combine(directory, "Resources");
            EnsureFolderExists(resourcesDirectory);

            string assetPath = Path.Combine(resourcesDirectory, $"{name}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(data)))
            {
                Debug.LogError($"[CardGraphEditor] Failed to create resource asset at '{assetPath}'.");
                return null;
            }

            Undo.RecordObject(catalog, "Add New Resource to Catalog");
            catalog.resources.Add(data);
            EditorUtility.SetDirty(catalog);

            return data;
        }

        /// <summary>
        /// Creates a new <see cref="CardData"/> ScriptableObject asset at the specified folder path and returns it.
        /// </summary>
        /// <param name="folder">Asset folder path (e.g., "Assets/Game/Data/Cards").</param>
        /// <param name="cardName">File and asset name for the new card.</param>
        /// <returns>The newly created card, or null if creation failed.</returns>
        public static CardData CreateCard(string folder, string cardName)
        {
            EnsureFolderExists(folder);
            string assetPath = Path.Combine(folder, cardName + ".asset");

            CardData newCard = ScriptableObject.CreateInstance<CardData>();
            newCard.name = cardName;
            newCard.assetName = cardName;
            AssetDatabase.CreateAsset(newCard, assetPath);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newCard)))
            {
                Debug.LogError($"[CardGraphEditor] Failed to create card asset at '{assetPath}'.");
                return null;
            }

            return newCard;
        }

        /// <summary>
        /// Creates a new <see cref="SpeakerData"/> ScriptableObject asset at the specified folder path and returns it.
        /// </summary>
        /// <param name="folder">Asset folder path (e.g., "Assets/Game/Data/Speakers").</param>
        /// <param name="speakerName">File and asset name for the new speaker.</param>
        /// <returns>The newly created speaker, or null if creation failed.</returns>
        public static SpeakerData CreateSpeaker(string folder, string speakerName)
        {
            EnsureFolderExists(folder);
            string assetPath = Path.Combine(folder, speakerName + ".asset");

            SpeakerData newSpeaker = ScriptableObject.CreateInstance<SpeakerData>();
            newSpeaker.name = speakerName;
            newSpeaker.assetName = speakerName;
            AssetDatabase.CreateAsset(newSpeaker, assetPath);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newSpeaker)))
            {
                Debug.LogError($"[CardGraphEditor] Failed to create speaker asset at '{assetPath}'.");
                return null;
            }

            return newSpeaker;
        }

        /// <summary>
        /// Creates a new <see cref="NarrativeDatabase"/> ScriptableObject asset at the specified folder path and returns it.
        /// </summary>
        /// <param name="folder">Asset folder path.</param>
        /// <param name="databaseName">File and asset name for the new database.</param>
        /// <returns>The newly created database, or null if creation failed.</returns>
        public static NarrativeDatabase CreateDatabase(string folder, string databaseName)
        {
            EnsureFolderExists(folder);
            string assetPath = Path.Combine(folder, databaseName + ".asset");

            NarrativeDatabase newDatabase = ScriptableObject.CreateInstance<NarrativeDatabase>();
            newDatabase.name = databaseName;
            AssetDatabase.CreateAsset(newDatabase, assetPath);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newDatabase)))
            {
                Debug.LogError($"[CardGraphEditor] Failed to create database asset at '{assetPath}'.");
                return null;
            }

            return newDatabase;
        }

        /// <summary>
        /// Creates a new <see cref="ResourceCatalog"/> ScriptableObject asset at the specified folder path and returns it.
        /// </summary>
        /// <param name="folder">Asset folder path.</param>
        /// <param name="catalogName">File and asset name for the new catalog.</param>
        /// <returns>The newly created catalog, or null if creation failed.</returns>
        public static ResourceCatalog CreateCatalog(string folder, string catalogName)
        {
            EnsureFolderExists(folder);
            string assetPath = Path.Combine(folder, catalogName + ".asset");

            ResourceCatalog newCatalog = ScriptableObject.CreateInstance<ResourceCatalog>();
            newCatalog.name = catalogName;
            AssetDatabase.CreateAsset(newCatalog, assetPath);

            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(newCatalog)))
            {
                Debug.LogError($"[CardGraphEditor] Failed to create resource catalog asset at '{assetPath}'.");
                return null;
            }

            return newCatalog;
        }

        /// <summary>Recursively creates the specified asset folder path if any segment does not already exist.</summary>
        private static void EnsureFolderExists(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder)) return;

            int separatorIndex = folder.LastIndexOf('/');
            if (separatorIndex <= 0) return;

            string parent = folder.Substring(0, separatorIndex);
            string leaf = folder.Substring(separatorIndex + 1);
            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Finds the next unused resource name within the given catalog using the default "Res_NewResource" base name.</summary>
        private static string FindNextUnusedResourceName(ResourceCatalog catalog)
        {
            return FindNextUnusedName("Res_NewResource", name => ResourceNameInUse(catalog, name));
        }

        /// <summary>Returns true if any resource in the catalog matches the given name by either Unity object name or assetName field.</summary>
        private static bool ResourceNameInUse(ResourceCatalog catalog, string name)
        {
            return catalog.resources.Exists(r => r != null && (r.name == name || r.assetName == name));
        }
    }
}
