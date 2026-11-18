using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Pinta o HUD (vida/mana do jogador e vida do Arquivista) com o pacote
    /// ArenaDoVazio_HUD_Tecnico_Unity: moldura + fundo + fill + icone.
    /// Nao altera nenhum outro objeto da cena.
    /// </summary>
    public static class HudDresser
    {
        private const string HudFolder = "Assets/UI/ArenaDoVazio_HUD_Tecnico_Unity";
        private const string RootName = "HUD_Barras";

        private const float PlayerScale = 0.82f;
        private const float BossScale = 0.82f;
        private const float Gap = 8f;

        [MenuItem("Vazio/UI/Pintar HUD (assets)")]
        public static void Apply()
        {
            PrepareTextures();

            GameObject canvasGo = GameObject.Find("Canvas_HUD");
            if (canvasGo == null)
            {
                Debug.LogError("[HUD] Canvas_HUD nao encontrado.");
                return;
            }

            GameHud hud = canvasGo.GetComponent<GameHud>();
            if (hud == null)
            {
                Debug.LogError("[HUD] GameHud nao encontrado no Canvas_HUD.");
                return;
            }

            // Remove apenas o grupo criado por este script (idempotente).
            Transform previous = canvasGo.transform.Find(RootName);
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            // Esconde as barras antigas (nao apaga nada).
            foreach (string legacy in new[] { "Barra_Vida", "Barra_Mana", "Barra_Chefe" })
            {
                Transform old = canvasGo.transform.Find(legacy);
                if (old != null)
                {
                    old.gameObject.SetActive(false);
                }
            }

            GameObject root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            // ---------- Vida (canto superior esquerdo) ----------
            Image healthFill = BuildBar(root.transform, "Vida", "Health", PlayerScale,
                new Vector2(0f, 1f), new Vector2(16f + 256f * PlayerScale, -(16f + 48f * PlayerScale)), null);

            // ---------- Mana (abaixo da vida) ----------
            Image manaFill = BuildBar(root.transform, "Mana", "Mana", PlayerScale,
                new Vector2(0f, 1f), new Vector2(16f + 256f * PlayerScale, -(16f + 96f * PlayerScale + Gap + 48f * PlayerScale)), null);

            // ---------- Arquivista (topo central) ----------
            GameObject bossGroupGo = new GameObject("Chefe_Grupo", typeof(RectTransform));
            bossGroupGo.transform.SetParent(root.transform, false);
            // Empilhada abaixo da mana (o pacote sugere as tres barras alinhadas a esquerda).
            float barHeight = 96f * PlayerScale;
            float bossY = -(16f + (barHeight + Gap) * 2f + 48f * BossScale);

            RectTransform bossRect = bossGroupGo.GetComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0f, 1f);
            bossRect.anchorMax = new Vector2(0f, 1f);
            bossRect.pivot = new Vector2(0.5f, 0.5f);
            bossRect.sizeDelta = new Vector2(512f * BossScale, 96f * BossScale);
            bossRect.anchoredPosition = new Vector2(16f + 256f * BossScale, bossY);

            CanvasGroup bossGroup = bossGroupGo.AddComponent<CanvasGroup>();
            bossGroup.alpha = 0f;

            Image bossFill = BuildBar(bossGroupGo.transform, "Chefe", "Archivist", BossScale,
                new Vector2(0.5f, 0.5f), Vector2.zero, bossRect);

            // ---------- Liga no GameHud ----------
            PlayerStats stats = Object.FindAnyObjectByType<PlayerStats>(FindObjectsInactive.Include);
            ArchivistBoss boss = Object.FindAnyObjectByType<ArchivistBoss>(FindObjectsInactive.Include);
            hud.Configure(stats, boss, healthFill, manaFill, bossFill, bossGroup);

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[HUD] Barras pintadas com os assets do pacote UI.");
        }

        /// <summary>Cria uma barra completa (fundo, fill, moldura e icone).</summary>
        private static Image BuildBar(Transform parent, string name, string folder, float scale,
            Vector2 anchor, Vector2 anchoredPosition, RectTransform host)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform));
            bar.transform.SetParent(parent, false);

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(512f * scale, 96f * scale);
            rect.anchoredPosition = host != null ? Vector2.zero : anchoredPosition;

            if (host != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
            }

            string dir = HudFolder + "/" + folder + "/" + folder;

            // Fundo (trilha)
            AddImage(bar.transform, "Fundo", dir + "_Background.png", new Vector2(416f, 40f) * scale,
                new Vector2(34f, 0f) * scale, false);

            // Fill
            Image fill = AddImage(bar.transform, "Fill", dir + "_Fill.png", new Vector2(416f, 40f) * scale,
                new Vector2(34f, 0f) * scale, true);

            // Moldura
            AddImage(bar.transform, "Moldura", dir + "_Frame.png", new Vector2(512f, 96f) * scale, Vector2.zero, false);

            // Icone
            AddImage(bar.transform, "Icone", dir + "_Icon.png", new Vector2(80f, 80f) * scale,
                new Vector2(-216f, 0f) * scale, false);

            return fill;
        }

        private static Image AddImage(Transform parent, string name, string assetPath, Vector2 size,
            Vector2 position, bool filled)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            Image image = go.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (image.sprite == null)
            {
                Debug.LogWarning("[HUD] Sprite nao encontrado: " + assetPath);
            }
            image.raycastTarget = false;
            image.preserveAspect = false;

            if (filled)
            {
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.fillAmount = 1f;
            }
            else
            {
                image.type = Image.Type.Simple;
            }

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return image;
        }

        private static void PrepareTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { HudFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; dirty = true; }
                if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                if (!Mathf.Approximately(importer.spritePixelsPerUnit, 32f)) { importer.spritePixelsPerUnit = 32f; dirty = true; }
                if (importer.filterMode != FilterMode.Point) { importer.filterMode = FilterMode.Point; dirty = true; }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
