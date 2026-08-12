using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;

namespace Game.Scripts.Editor
{
    public static class CardGraphEditor
    {
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
            // Leave displayName empty; author sets the player-facing label in the Inspector.

            string catalogPath = AssetDatabase.GetAssetPath(catalog);
            string directory = string.IsNullOrEmpty(catalogPath) ? "Assets" : Path.GetDirectoryName(catalogPath);
            string resourcesDirectory = Path.Combine(directory, "Resources");

            if (!AssetDatabase.IsValidFolder(resourcesDirectory))
            {
                AssetDatabase.CreateFolder(directory, "Resources");
            }

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

        private static void EnsureFolderExists(string folder)
        {
            if (Directory.Exists(folder)) return;

            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        private static string FindNextUnusedResourceName(ResourceCatalog catalog)
        {
            return FindNextUnusedName("Res_NewResource", name => ResourceNameInUse(catalog, name));
        }

        private static bool ResourceNameInUse(ResourceCatalog catalog, string name)
        {
            return catalog.resources.Exists(r => r != null && (r.name == name || r.assetName == name));
        }

    }
}
