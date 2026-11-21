using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Monta o mundo do Velho Oeste: fatia os cowboys, cria os prefabs (inimigos,
    /// chefe, balas e portal) e adiciona jogador, spawner e chefe na cena.
    /// </summary>
    public static class WestWorldBuilder
    {
        private const string ScenePath = "Assets/VelhoOeste/Scenes/WildWestSampleScene.unity";
        private const string VazioScenePath = "Assets/Vazio/Scenes/Vazio.unity";
        private const string PrefabDir = "Assets/VelhoOeste/Prefabs";
        private const string CowboyDir = "Assets/VelhoOeste/Cowboy";
        private const string PlayerSheetFolder = "Assets/Player/Sprites/Player/";
        private const string MaterialPath = "Assets/Vazio/Materials/SpriteUnlit.mat";
        private const string WhitePath = "Assets/Vazio/Art/Effects/white.png";
        private const string MagicPath = "Assets/Vazio/Prefabs/MagicProjectile.prefab";
        private const string PortalSheet = "Assets/Vazio/Portal/Purple Portal Sprite Sheet.png";
        private const string PotionPath = "Assets/VelhoOeste/Cura/potion7HF_RD.png";

        private const float CowboyPpu = 40f;
        private const float BossScale = 1.8f;

        private static readonly string[] Clips =
        {
            "Idle", "Walk", "Run", "Jump", "Shot", "Special", "\u0421harging", "Hurt", "Dead", "Fighting stance",
        };

        [MenuItem("Vazio/Velho Oeste/Montar Mundo")]
        public static void Build()
        {
            SliceCowboys();
            AssetDatabase.Refresh();

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Sprite white = LoadSingleSprite(WhitePath);

            GameObject bullet = BuildBulletPrefab("Cowboy_Bullet", material, white, new Color(1f, 0.85f, 0.25f), new Vector2(0.32f, 0.12f), false);
            GameObject charged = BuildBulletPrefab("Cowboy_Bullet_Charged", material, white, new Color(1f, 0.45f, 0.15f), new Vector2(0.55f, 0.22f), true);
            GameObject portal = BuildPortalPrefab(material);
            GameObject potion = BuildPotionPrefab(material);
            GameObject spawnPortal = BuildSpawnPortalPrefab(material);
            GameObject boss = BuildCowboy("Cowboy_Boss", 3, material, bullet, charged, portal, true);
            GameObject c1 = BuildCowboy("Cowboy_1", 1, material, bullet, null, portal, false);
            GameObject c2 = BuildCowboy("Cowboy_2", 2, material, bullet, null, portal, false);

            AssembleScene(c1, c2, boss, portal, potion, spawnPortal);

            RegisterScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VelhoOeste] Mundo montado.");
        }

        // ------------------------------------------------------------- fatiamento
        private static void SliceCowboys()
        {
            for (int i = 1; i <= 3; i++)
            {
                foreach (string clip in Clips)
                {
                    string path = CowboyDir + "/Cowboy_" + i + "/" + clip + ".png";
                    SliceSheet(path, "c" + i + "_" + ShortName(clip) + "_", 128, 128, CowboyPpu);
                }
            }
        }

        private static string ShortName(string clip)
        {
            switch (clip)
            {
                case "Fighting stance": return "Stance";
                case "\u0421harging": return "Charging";
                default: return clip;
            }
        }

        private static void SliceSheet(string path, string prefix, int frameW, int frameH, float ppu)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("[VelhoOeste] Sheet ausente: " + path);
                return;
            }

#pragma warning disable 0618
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            int count = Mathf.Max(1, width / frameW);
            SpriteMetaData[] metas = new SpriteMetaData[count];
            for (int i = 0; i < count; i++)
            {
                metas[i] = new SpriteMetaData
                {
                    name = prefix + i.ToString("D2"),
                    rect = new Rect(i * frameW, 0f, frameW, frameH),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                };
            }

            _ = height;
            importer.spritesheet = metas;
            importer.SaveAndReimport();
#pragma warning restore 0618
        }

        private static Sprite[] LoadFrames(string path)
        {
            List<Sprite> sprites = new List<Sprite>();
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

        private static Sprite LoadSingleSprite(string path)
        {
            Sprite[] sprites = LoadFrames(path);
            return sprites.Length > 0 ? sprites[0] : null;
        }

        // ---------------------------------------------------------------- prefabs
        private static GameObject BuildBulletPrefab(string name, Material material, Sprite white, Color color,
            Vector2 size, bool glow)
        {
            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = white;
            renderer.color = color;
            renderer.sortingOrder = 60;
            if (material != null) renderer.sharedMaterial = material;

            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.AddComponent<CowboyBullet>();

            if (glow)
            {
                GameObject glowGo = new GameObject("Glow");
                glowGo.transform.SetParent(go.transform, false);
                SpriteRenderer glowRenderer = glowGo.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = white;
                glowRenderer.color = new Color(1f, 0.55f, 0.15f, 0.45f);
                glowRenderer.sortingOrder = 59;
                if (material != null) glowRenderer.sharedMaterial = material;
                glowGo.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
            }

            Directory.CreateDirectory(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return asset;
        }

        private static GameObject BuildSpawnPortalPrefab(Material material)
        {
            Sprite[] frames = LoadFrames(PortalSheet);

            GameObject root = new GameObject("Portal_Spawn");

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            visual.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = frames.Length > 0 ? frames[0] : null;
            renderer.sortingOrder = 45;
            if (material != null) renderer.sharedMaterial = material;

            if (frames.Length > 1)
            {
                SpriteLoopAnimator animator = visual.AddComponent<SpriteLoopAnimator>();
                animator.Configure(renderer, frames, 12f);
            }

            Directory.CreateDirectory(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Portal_Spawn.prefab");
            Object.DestroyImmediate(root);
            return asset;
        }

        private static GameObject BuildPotionPrefab(Material material)
        {
            SlicePotion(PotionPath);
            Sprite sprite = LoadFrames(PotionPath).Length > 0 ? LoadFrames(PotionPath)[0] : null;

            GameObject go = new GameObject("Cura");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 55;
            if (material != null) renderer.sharedMaterial = material;

            go.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            HealthPotion potion = go.AddComponent<HealthPotion>();
            potion.Configure(0.25f);

            Directory.CreateDirectory(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Cura.prefab");
            Object.DestroyImmediate(go);
            return asset;
        }

        private static void SlicePotion(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static GameObject BuildPortalPrefab(Material material)
        {
            Sprite[] frames = LoadFrames(PortalSheet);
            GameObject root = new GameObject("Portal_VelhoOeste");

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            // O sprite do portal tem pivo nos pes: sobe a arte ate encostar no chao.
            visual.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            visual.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = frames.Length > 0 ? frames[0] : null;
            renderer.sortingOrder = 40;
            if (material != null) renderer.sharedMaterial = material;

            if (frames.Length > 1)
            {
                SpriteLoopAnimator animator = visual.AddComponent<SpriteLoopAnimator>();
                animator.Configure(renderer, frames, 8f);
            }

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.4f, 5f);
            trigger.offset = new Vector2(0f, 2.5f);

            WestPortal portal = root.AddComponent<WestPortal>();
            portal.Configure("Vazio");

            Directory.CreateDirectory(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabDir + "/Portal_VelhoOeste.prefab");
            Object.DestroyImmediate(root);
            return asset;
        }

        private static GameObject BuildCowboy(string name, int index, Material material, GameObject bullet,
            GameObject chargedBullet, GameObject portal, bool isBoss)
        {
            string dir = CowboyDir + "/Cowboy_" + index + "/";
            Sprite[][] clips = new Sprite[10][];
            for (int i = 0; i < Clips.Length; i++)
            {
                clips[i] = LoadFrames(dir + Clips[i] + ".png");
            }

            GameObject go = new GameObject(name);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = clips[0].Length > 0 ? clips[0][0] : null;
            renderer.sortingOrder = 50;
            if (material != null) renderer.sharedMaterial = material;

            CowboyVisualAnimator visual = go.AddComponent<CowboyVisualAnimator>();
            visual.Configure(renderer, clips[0], clips[1], clips[2], clips[3], clips[4], clips[5], clips[6], clips[7], clips[8], clips[9]);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.7f, 1.7f);
            collider.offset = new Vector2(0f, 0.85f);

            GameObject fire = new GameObject("FirePoint");
            fire.transform.SetParent(go.transform, false);
            // Cano da arma: na altura do peito/mao do cowboy (a escala do chefe
            // ja multiplica esse offset).
            fire.transform.localPosition = new Vector3(0.5f, 0.95f, 0f);

            if (isBoss)
            {
                go.transform.localScale = new Vector3(BossScale, BossScale, 1f);
                CowboyBoss boss = go.AddComponent<CowboyBoss>();
                boss.Configure(48f, bullet, chargedBullet, portal, 0f, visual, fire.transform);
            }
            else
            {
                CowboyEnemy enemy = go.AddComponent<CowboyEnemy>();
                enemy.Configure(6f, bullet, 1f, 0f, visual, fire.transform, renderer);
            }

            Directory.CreateDirectory(PrefabDir);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return asset;
        }

        // ------------------------------------------------------------------ cena
        private static void AssembleScene(GameObject cowboy1, GameObject cowboy2, GameObject bossPrefab, GameObject portal, GameObject potion, GameObject spawnPortal)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // remove montagens anteriores
            foreach (string legacy in new[] { "JogoVelhoOeste" })
            {
                GameObject old = GameObject.Find(legacy);
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                }
            }

            GameObject root = new GameObject("JogoVelhoOeste");

            // Audio: clipes + musica do mundo (the mountain).
            GameObject audioGo = AudioInstaller.Create();
            audioGo.transform.SetParent(root.transform, false);
            audioGo.GetComponent<GameAudio>().SetAutoWestMusic(true);

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                camera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            float playerX = -6f;
            float groundY = FindGround(playerX);

            Transform player = BuildPlayer(new Vector3(playerX, groundY + 0.92f, 0f));
            player.SetParent(root.transform, true);

            WestCameraFollow follow = camera.GetComponent<WestCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<WestCameraFollow>();
            }

            follow.Configure(player, -12f, 322f);

            // A camera do sample e' muito aberta (ortho 17); ajusta para o
            // enquadramento de gameplay usado no mundo do Vazio.
            camera.orthographic = true;
            camera.orthographicSize = 7.36f;
            camera.transform.position = new Vector3(playerX, groundY + 3.6f, -10f);

            // ---------------- Arenas de combate ----------------
            // 4 instancias do mesmo componente; o chefe nasce da arena final.
            BuildArena(root.transform, "Arena_1", 25f, cowboy1, cowboy2, null,
                10, 1, 2f, 5f, 0f, follow, 0f, potion, false, 15f, spawnPortal);

            BuildArena(root.transform, "Arena_2", 95f, cowboy1, cowboy2, null,
                15, 1, 2f, 5f, 0f, follow, 0f, potion, false, 15f, spawnPortal);

            BuildArena(root.transform, "Arena_3", 175f, cowboy1, cowboy2, null,
                20, 1, 2f, 5f, 0f, follow, 0f, potion, false, 15f, spawnPortal);

            BuildArena(root.transform, "Arena_Final", 268f, cowboy1, cowboy2, bossPrefab,
                20, 2, 2f, 5f, 5f, follow, 0f, potion, true, 15f, spawnPortal);

            BuildHud(root.transform, player.GetComponent<PlayerStats>(), null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static CombatArena BuildArena(Transform root, string name, float triggerX,
            GameObject cowboy1, GameObject cowboy2, GameObject bossPrefab,
            int perWave, int waves, float interval, float wavePause, float bossDelay,
            WestCameraFollow follow, float bossX, GameObject potion, bool potionDuringCombat, float potionInterval,
            GameObject spawnPortal)
        {
            float groundY = FindGround(triggerX);

            // A camera trava no X do trigger: a arena tem que caber no
            // enquadramento. Meia largura visivel com ortho 5.2 e 16:9 ~ 9,2 u;
            // usamos 8 u para as paredes e 5,5 u para os spawns.
            const float halfWidth = 11f;
            const float spawnHalfWidth = 9f;
            float leftX = triggerX - halfWidth;
            float rightX = triggerX + halfWidth;

            // Trigger de entrada (o proprio objeto da arena)
            GameObject triggerGo = new GameObject(name);
            triggerGo.transform.SetParent(root, false);
            triggerGo.transform.position = new Vector3(triggerX, groundY + 3f, 0f);

            BoxCollider2D trigger = triggerGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.6f, 8f);

            // Paredes invisiveis
            GameObject leftWall = BuildWall(root, name + "_ParedeEsq", leftX);
            GameObject rightWall = BuildWall(root, name + "_ParedeDir", rightX);

            // Pontos de spawn distribuidos dentro da arena
            List<Transform> points = new List<Transform>();
            const int pointCount = 5;
            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0.5f : i / (float)(pointCount - 1);
                float x = Mathf.Lerp(triggerX - spawnHalfWidth, triggerX + spawnHalfWidth, t);
                GameObject point = new GameObject("Spawn_" + (i + 1));
                point.transform.SetParent(root, false);
                point.transform.position = new Vector3(x, FindGround(x) + 0.05f, 0f);
                points.Add(point.transform);
            }

            Transform bossPoint = null;
            if (bossPrefab != null)
            {
                GameObject bossGo = new GameObject("Boss_Spawn");
                bossGo.transform.SetParent(root, false);
                float bx = bossX > 0f ? bossX : triggerX + 2f;
                bossGo.transform.position = new Vector3(bx, FindGround(bx) + 0.05f, 0f);
                bossPoint = bossGo.transform;
            }

            CombatArena arena = triggerGo.AddComponent<CombatArena>();
            arena.Configure(points, leftWall, rightWall, trigger, cowboy1, new[] { cowboy1, cowboy2 },
                bossPrefab, bossPoint, perWave, interval, waves, wavePause, bossDelay, follow,
                potion, potionDuringCombat, potionInterval, spawnPortal, 0.5f);

            return arena;
        }

        private static GameObject BuildWall(Transform root, string name, float x)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(root, false);
            wall.transform.position = new Vector3(x, FindGround(x) + 7f, 0f);

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.6f, 14f);

            wall.SetActive(false);
            return wall;
        }

        private static void BuildHud(Transform root, PlayerStats stats, CowboyBoss boss)
        {
            const string hudFolder = "Assets/UI/ArenaDoVazio_HUD_Tecnico_Unity";

            GameObject canvasGo = new GameObject("Canvas_HUD_Oeste", typeof(RectTransform));
            canvasGo.transform.SetParent(root, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            WestHud hud = canvasGo.AddComponent<WestHud>();

            const float scale = 0.82f;
            float barHeight = 96f * scale;
            float gap = 8f;

            Image health = BuildBar(canvasGo.transform, "Vida", hudFolder + "/Health/Health", scale,
                new Vector2(0f, 1f), new Vector2(16f + 256f * scale, -(16f + 48f * scale)));

            Image mana = BuildBar(canvasGo.transform, "Mana", hudFolder + "/Mana/Mana", scale,
                new Vector2(0f, 1f), new Vector2(16f + 256f * scale, -(16f + barHeight + gap + 48f * scale)));

            // barra do cowboy (usa os assets do Arquivista), no rodape central
            GameObject bossGroupGo = new GameObject("Chefe_Grupo", typeof(RectTransform));
            bossGroupGo.transform.SetParent(canvasGo.transform, false);

            const float bossScale = 1.1f;
            RectTransform bossRect = bossGroupGo.GetComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0.5f, 0f);
            bossRect.anchorMax = new Vector2(0.5f, 0f);
            bossRect.pivot = new Vector2(0.5f, 0f);
            bossRect.sizeDelta = new Vector2(512f * bossScale, 96f * bossScale);
            bossRect.anchoredPosition = new Vector2(0f, 18f);

            CanvasGroup bossGroup = bossGroupGo.AddComponent<CanvasGroup>();
            bossGroup.alpha = 0f;

            Image bossFill = BuildBar(bossGroupGo.transform, "Chefe", hudFolder + "/Archivist/Archivist", bossScale,
                new Vector2(0.5f, 0.5f), Vector2.zero, true);

            hud.Configure(stats, boss, health, mana, bossFill, bossGroup);
        }

        private static Image BuildBar(Transform parent, string name, string spriteBase, float scale,
            Vector2 anchor, Vector2 position, bool anchoredToCentre = false)
        {
            GameObject bar = new GameObject(name, typeof(RectTransform));
            bar.transform.SetParent(parent, false);

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(512f * scale, 96f * scale);
            rect.anchoredPosition = anchoredToCentre
                ? new Vector2(0f, 0f)
                : position;

            AddImage(bar.transform, "Fundo", spriteBase + "_Background.png", new Vector2(416f, 40f) * scale,
                new Vector2(34f, 0f) * scale, false);
            Image fill = AddImage(bar.transform, "Fill", spriteBase + "_Fill.png", new Vector2(416f, 40f) * scale,
                new Vector2(34f, 0f) * scale, true);
            AddImage(bar.transform, "Moldura", spriteBase + "_Frame.png", new Vector2(512f, 96f) * scale, Vector2.zero, false);
            AddImage(bar.transform, "Icone", spriteBase + "_Icon.png", new Vector2(80f, 80f) * scale,
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
            image.raycastTarget = false;

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

        private static float FindGround(float x)
        {
            // Pega o colisor MAIS BAIXO (o chao): adereços altos como cercas e
            // carroças tambem tem collider e apareceriam antes no raycast simples.
            RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(x, 40f), Vector2.down, 80f);
            if (hits.Length == 0)
            {
                return -8f;
            }

            float lowest = hits[0].point.y;
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.point.y < lowest)
                {
                    lowest = hit.point.y;
                }
            }

            return lowest;
        }

        private static Transform BuildPlayer(Vector3 position)
        {
            Sprite[] idle = LoadFrames(PlayerSheetFolder + "Idle.png");
            Sprite[] run = LoadFrames(PlayerSheetFolder + "Run.png");
            Sprite[] walk = LoadFrames(PlayerSheetFolder + "Walk.png");
            Sprite[] jump = LoadFrames(PlayerSheetFolder + "Jump.png");
            Sprite[] attack1 = LoadFrames(PlayerSheetFolder + "Attack_1.png");
            Sprite[] attack2 = LoadFrames(PlayerSheetFolder + "Attack_2.png");
            Sprite[] hurt = LoadFrames(PlayerSheetFolder + "Hurt.png");
            Sprite[] dead = LoadFrames(PlayerSheetFolder + "Dead.png");

            GameObject player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = position;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);

            GameObject spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(visual.transform, false);
            spriteGo.transform.localPosition = new Vector3(0f, -0.9f, 0f);

            SpriteRenderer renderer = spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            renderer.sortingLayerName = "Player";
            renderer.sortingOrder = 500;

            Material unlit = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            PlayerVisualAnimator animator = spriteGo.AddComponent<PlayerVisualAnimator>();
            animator.Configure(renderer, idle, run, walk, jump, attack1, attack2, hurt, dead);

            PlayerController controller = player.AddComponent<PlayerController>();
            controller.ConfigurePresentation(visual.transform, renderer);
            controller.ConfigureVisual(animator);
            controller.ConfigureGravity(3f);

            PlayerStats stats = player.AddComponent<PlayerStats>();
            stats.Configure(controller, renderer, position + Vector3.up * 0.5f, animator);

            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            GameObject magic = AssetDatabase.LoadAssetAtPath<GameObject>(MagicPath);
            combat.Configure(magic, stats, controller, animator);

            return player.transform;
        }

        private static void RegisterScenes()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in new[] { VazioScenePath, ScenePath })
            {
                if (!scenes.Exists(s => s.path == path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
