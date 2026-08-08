using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;

namespace Game.Scripts.Editor
{
    public static class CardGraphEditor
    {
        // Adds a new resource to the catalog with an auto-generated unique id.
        public static string CreateResource(ResourceCatalog catalog)
        {
            if (catalog == null)
            {
                return null;
            }

            string id = FindNextUnusedResourceId(catalog);

            ResourceData data = ScriptableObject.CreateInstance<ResourceData>();
            data.id = id;
            data.displayName = id;
            data.name = id;

            string catalogPath = AssetDatabase.GetAssetPath(catalog);
            string directory = string.IsNullOrEmpty(catalogPath) ? "Assets" : Path.GetDirectoryName(catalogPath);
            string resourcesDirectory = Path.Combine(directory, "Resources");

            if (!AssetDatabase.IsValidFolder(resourcesDirectory))
            {
                AssetDatabase.CreateFolder(directory, "Resources");
            }

            string assetPath = Path.Combine(resourcesDirectory, $"{id}.asset");
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

            return id;
        }

        private static string FindNextUnusedResourceId(ResourceCatalog catalog)
        {
            int index = 1;
            string id;
            do
            {
                id = $"resource_{index:D3}";
                index++;
            }
            while (catalog.resources.Exists(r => r != null && r.id == id));

            return id;
        }

        public static List<string> GetSpeakerChoices(NarrativeDatabase db)
        {
            var list = new List<string> { "" };
            if (db != null && db.speakers != null)
            {
                foreach (var s in db.speakers)
                {
                    if (s != null && !string.IsNullOrEmpty(s.memberId)) list.Add(s.memberId);
                }
            }
            return list;
        }

        public static List<string> GetResourceCatalogChoices(NarrativeDatabase db)
        {
            var list = new List<string> { "" };
            if (db != null && db.resourceCatalog != null)
            {
                foreach (var r in db.resourceCatalog.resources)
                {
                    if (r != null && !string.IsNullOrEmpty(r.id)) list.Add(r.id);
                }
            }
            return list;
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

        public static void SyncIdToAssetName(CardData card, bool repoint = true)
        {
            if (card == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(card);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrEmpty(card.cardId))
            {
                card.cardId = fileName;
                EditorUtility.SetDirty(card);
                return;
            }

            if (card.cardId == fileName)
            {
                return;
            }

            if (repoint)
            {
                RepointReferences(card.cardId, fileName);
            }

            Undo.RecordObject(card, "Sync Card ID with Asset Name");
            card.cardId = fileName;
            card.name = fileName;
            EditorUtility.SetDirty(card);
        }

        public static void SyncIdToAssetName(CouncilMemberData speaker, bool repoint = true)
        {
            if (speaker == null) return;
            string assetPath = AssetDatabase.GetAssetPath(speaker);
            if (string.IsNullOrEmpty(assetPath)) return;
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrEmpty(speaker.memberId))
            {
                speaker.memberId = fileName;
                EditorUtility.SetDirty(speaker);
                return;
            }

            if (speaker.memberId == fileName) return;

            if (repoint)
            {
                RepointSpeakerReferences(speaker.memberId, fileName);
            }

            Undo.RecordObject(speaker, "Sync Speaker ID with Asset Name");
            speaker.memberId = fileName;
            speaker.name = fileName;
            EditorUtility.SetDirty(speaker);
        }

        public static void SyncIdToAssetName(ResourceData resource, bool repoint = true)
        {
            if (resource == null) return;
            string assetPath = AssetDatabase.GetAssetPath(resource);
            if (string.IsNullOrEmpty(assetPath)) return;
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            if (string.IsNullOrEmpty(resource.id))
            {
                resource.id = fileName;
                EditorUtility.SetDirty(resource);
                return;
            }

            if (resource.id == fileName) return;

            if (repoint)
            {
                RepointResourceReferences(resource.id, fileName);
            }

            Undo.RecordObject(resource, "Sync Resource ID with Asset Name");
            resource.id = fileName;
            resource.name = fileName;
            EditorUtility.SetDirty(resource);
        }

        public static void RepointReferences(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
                if (db == null)
                {
                    continue;
                }

                if (db.startingCardId == oldId)
                {
                    Undo.RecordObject(db, "Update Starting Card ID");
                    db.startingCardId = newId;
                    EditorUtility.SetDirty(db);
                }

                if (db.editorGraphPositions != null)
                {
                    for (int i = 0; i < db.editorGraphPositions.Count; i++)
                    {
                        if (db.editorGraphPositions[i].cardId != oldId)
                        {
                            continue;
                        }

                        Undo.RecordObject(db, "Update Graph Node Position Id");
                        NarrativeDatabase.CardGraphPosition entry = db.editorGraphPositions[i];
                        entry.cardId = newId;
                        db.editorGraphPositions[i] = entry;
                        EditorUtility.SetDirty(db);
                    }
                }
            }

            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            foreach (string cGuid in cardGuids)
            {
                string cPath = AssetDatabase.GUIDToAssetPath(cGuid);
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(cPath);
                if (c == null) continue;

                bool changed = false;
                if (c.leftNextCardId == oldId)
                {
                    Undo.RecordObject(c, "Update Left Next Card ID");
                    c.leftNextCardId = newId;
                    changed = true;
                }

                if (c.rightNextCardId == oldId)
                {
                    Undo.RecordObject(c, "Update Right Next Card ID");
                    c.rightNextCardId = newId;
                    changed = true;
                }

                if (c.continueNextCardId == oldId)
                {
                    Undo.RecordObject(c, "Update Continue Next Card ID");
                    c.continueNextCardId = newId;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(c);
                }
            }
            
            AssetDatabase.SaveAssets();
        }

        public static void RepointSpeakerReferences(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId) return;

            string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
                if (db == null) continue;

                if (db.editorSpeakerPositions != null)
                {
                    for (int i = 0; i < db.editorSpeakerPositions.Count; i++)
                    {
                        if (db.editorSpeakerPositions[i].memberId == oldId)
                        {
                            Undo.RecordObject(db, "Update Speaker Position Id");
                            var entry = db.editorSpeakerPositions[i];
                            entry.memberId = newId;
                            db.editorSpeakerPositions[i] = entry;
                            EditorUtility.SetDirty(db);
                        }
                    }
                }
            }

            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            foreach (string cGuid in cardGuids)
            {
                string cPath = AssetDatabase.GUIDToAssetPath(cGuid);
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(cPath);
                if (c == null) continue;

                if (c.speakerId == oldId)
                {
                    Undo.RecordObject(c, "Update Speaker ID");
                    c.speakerId = newId;
                    EditorUtility.SetDirty(c);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static void RepointResourceReferences(string oldId, string newId)
        {
            if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId) return;

            string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
                if (db == null) continue;

                if (db.editorResourcePositions != null)
                {
                    for (int i = 0; i < db.editorResourcePositions.Count; i++)
                    {
                        if (db.editorResourcePositions[i].resourceId == oldId)
                        {
                            Undo.RecordObject(db, "Update Resource Position Id");
                            var entry = db.editorResourcePositions[i];
                            entry.resourceId = newId;
                            db.editorResourcePositions[i] = entry;
                            EditorUtility.SetDirty(db);
                        }
                    }
                }
            }

            string[] cardGuids = AssetDatabase.FindAssets("t:CardData");
            foreach (string cGuid in cardGuids)
            {
                string cPath = AssetDatabase.GUIDToAssetPath(cGuid);
                CardData c = AssetDatabase.LoadAssetAtPath<CardData>(cPath);
                if (c == null) continue;

                bool changed = false;
                changed |= UpdateResourceValueList(c.leftResourceChange.values, oldId, newId);
                changed |= UpdateResourceValueList(c.rightResourceChange.values, oldId, newId);

                if (changed)
                {
                    Undo.RecordObject(c, "Update Resource IDs");
                    EditorUtility.SetDirty(c);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static bool UpdateResourceValueList(ResourceValue[] values, string oldId, string newId)
        {
            if (values == null) return false;
            bool changed = false;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].id == oldId)
                {
                    values[i].id = newId;
                    changed = true;
                }
            }
            return changed;
        }
    }
}
