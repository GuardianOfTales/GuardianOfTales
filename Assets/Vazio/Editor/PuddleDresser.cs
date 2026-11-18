using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Aplica o pacote Assets/Pocas: fatia o Acid VFX 01 (projetil), o Acid VFX 02
    /// Repeatable (poca) e o Acid VFX 02 Ending (sumico), atualiza o prefab da
    /// poca, cria o prefab do projetil e liga no Arquivista.
    /// </summary>
    public static class PuddleDresser
    {
        private const string Root = "Assets/Pocas/Acid VFX 01 - 02/Acid VFX 01 - 02";
        private const string AcidSheet = Root + "/Acid VFX 1/Acid VFX 01.png";
        private const string RepeatSheet = Root + "/Acid VFX 2/Acid VFX 02Repeatable.png";
        private const string EndingSheet = Root + "/Acid VFX 2/Acid VFX 02Ending.png";

        private const string PuddlePrefabPath = "Assets/Vazio/Prefabs/CorruptedPuddle.prefab";
        private const string AcidPrefabPath = "Assets/Vazio/Prefabs/AcidProjectile.prefab";
        private const string MaterialPath = "Assets/Vazio/Materials/SpriteUnlit.mat";

        // Cada quadro do Acid VFX 02 tem 56x32 (os 384/192 px sao a animacao
        // empilhada, nao duas pocas no mesmo quadro).
        private const float PuddleVisualScale = 2f;
        private const float PuddleVisualY = 0.375f;

        [MenuItem("Vazio/Pocas/Aplicar Acid VFX")]
        public static void Apply()
        {
            Slice();
            Build();
        }

        /// <summary>Fatiamento (reimporta texturas; chamar separado).</summary>
        public static void Slice()
        {
            SliceSheet(AcidSheet, 32, 32, 16, false, "Acid1_");
            SliceSheet(RepeatSheet, 56, 32, 12, true, "PuddleRepeat_");
            SliceSheet(EndingSheet, 56, 32, 6, true, "PuddleEnd_");
        }

        /// <summary>Prefabs + ligacao no chefe (nao mexe no resto da cena).</summary>
        public static void Build()
        {
            Sprite[] acid = LoadFrames(AcidSheet);
            Sprite[] repeat = LoadFrames(RepeatSheet);
            Sprite[] ending = LoadFrames(EndingSheet);

            if (acid.Length == 0 || repeat.Length == 0)
            {
                Debug.LogError("[Pocas] Frames nao encontrados.");
                return;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            // ---------- Prefab da poca ----------
            GameObject puddleRoot = PrefabUtility.LoadPrefabContents(PuddlePrefabPath);
            if (puddleRoot != null)
            {
                SpriteRenderer rootRenderer = puddleRoot.GetComponent<SpriteRenderer>();
                Transform visual = puddleRoot.transform.Find("Visual");
                if (visual == null)
                {
                    GameObject visualGo = new GameObject("Visual");
                    visual = visualGo.transform;
                    visual.SetParent(puddleRoot.transform, false);
                }

                SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    renderer = visual.gameObject.AddComponent<SpriteRenderer>();
                }

                if (rootRenderer != null)
                {
                    Object.DestroyImmediate(rootRenderer);
                }

                renderer.sprite = repeat[0];
                renderer.sortingOrder = -25;
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }

                visual.localPosition = new Vector3(0f, PuddleVisualY, 0f);
                visual.localScale = new Vector3(PuddleVisualScale, PuddleVisualScale, 1f);

                CorruptedPuddle puddle = puddleRoot.GetComponent<CorruptedPuddle>();
                if (puddle != null)
                {
                    SerializedObject so = new SerializedObject(puddle);
                    SetArray(so, "repeatFrames", repeat);
                    SetArray(so, "endingFrames", ending);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(puddleRoot, PuddlePrefabPath);
                PrefabUtility.UnloadPrefabContents(puddleRoot);
            }

            // ---------- Prefab do projetil ----------
            GameObject acidGo = new GameObject("AcidProjectile");
            SpriteRenderer acidRenderer = acidGo.AddComponent<SpriteRenderer>();
            acidRenderer.sprite = acid[0];
            acidRenderer.sortingOrder = -20;
            if (material != null)
            {
                acidRenderer.sharedMaterial = material;
            }

            acidGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            AcidProjectile projectile = acidGo.AddComponent<AcidProjectile>();
            GameObject puddleAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PuddlePrefabPath);
            projectile.ConfigureVisuals(acid, puddleAsset, 0f);

            PrefabUtility.SaveAsPrefabAsset(acidGo, AcidPrefabPath);
            Object.DestroyImmediate(acidGo);

            // ---------- Ligacao no chefe da cena ----------
            ArchivistBoss boss = Object.FindAnyObjectByType<ArchivistBoss>(FindObjectsInactive.Include);
            if (boss != null)
            {
                SerializedObject bossSo = new SerializedObject(boss);
                SerializedProperty acidProp = bossSo.FindProperty("acidPrefab");
                if (acidProp != null)
                {
                    acidProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(AcidPrefabPath);
                    bossSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                Debug.LogWarning("[Pocas] Arquivista nao encontrado na cena.");
            }

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Pocas] Acid VFX aplicado: projetil (" + acid.Length + " quadros) e poca (" + repeat.Length + " + " + ending.Length + " quadros).");
        }

        private static void SetArray(SerializedObject so, string field, Sprite[] frames)
        {
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogWarning("[Pocas] Campo nao encontrado: " + field);
                return;
            }

            property.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }
        }

        private static void SliceSheet(string path, int frameW, int frameH, int count, bool vertical, string prefix)
        {
#pragma warning disable 0618
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Pocas] Textura nao encontrada: " + path);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            SpriteMetaData[] metas = new SpriteMetaData[count];
            for (int i = 0; i < count; i++)
            {
                Rect rect = vertical
                    ? new Rect(0f, height - (i + 1) * frameH, frameW, frameH)
                    : new Rect(i * frameW, 0f, frameW, frameH);

                metas[i] = new SpriteMetaData
                {
                    name = prefix + i.ToString("D2"),
                    rect = rect,
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }

            importer.spritesheet = metas;
            importer.SaveAndReimport();
#pragma warning restore 0618
        }

        private static Sprite[] LoadFrames(string path)
        {
            System.Collections.Generic.List<Sprite> sprites =
                new System.Collections.Generic.List<Sprite>();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites.ToArray();
        }
    }
}
