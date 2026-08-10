using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;

namespace Game.Scripts.Editor
{
    public static class CardGraphEditor
    {
        // Adds a new resource to the catalog with an auto-generated asset name.
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
            // displayName intentionally left empty — author sets the player-facing label in the Inspector

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

        private static string FindNextUnusedResourceName(ResourceCatalog catalog)
        {
            const string baseTemplate = "Res_NewResource";
            if (!ResourceNameInUse(catalog, baseTemplate)) return baseTemplate;

            int index = 2;
            string name;
            do
            {
                name = $"Res_NewResource_{index:D2}";
                index++;
            }
            while (ResourceNameInUse(catalog, name));

            return name;
        }

        private static bool ResourceNameInUse(ResourceCatalog catalog, string name)
        {
            return catalog.resources.Exists(r => r != null && (r.name == name || r.assetName == name));
        }

        public static NarrativeDatabase FindOwningDatabase(CardData cardData)
        {
            string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
            NarrativeDatabase fallbackDb = null;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
                if (db == null)
                {
                    continue;
                }

                if (fallbackDb == null)
                {
                    fallbackDb = db;
                }

                if (cardData != null && db.cards != null && db.cards.Contains(cardData))
                {
                    return db;
                }
            }

            return fallbackDb;
        }
    }
}
