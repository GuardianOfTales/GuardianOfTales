using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Constroi a cena do mundo Vazio a partir dos assets pre-moldados da pasta
    /// "base" (scenesMVP/Vazio), corrigindo as inconsistencias do material original:
    /// a arena estava espalhada atras do inicio do corredor, os elementos tinham
    /// escalas divergentes e nao existiam colisores 2D, camera de jogo nem jogador.
    /// </summary>
    public static class VazioLevelBuilder
    {
        private const string Root = "Assets/Vazio";
        private const string Art = Root + "/Art";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/Vazio.unity";
        private const string MaterialPath = Root + "/Materials/SpriteUnlit.mat";
        private const string PlayerSpritePath = Art + "/Characters/librarian.png";
        private const string PlayerSheetFolder = "Assets/Player/Sprites/Player/";
        private const float PlayerSpritePPU = 36f;
        private const float PlayerFeetOffsetY = -0.9f;

        // ---- Geometria do mundo -------------------------------------------------
        private const float CorridorStartX = -8f;
        private const float ArenaStartX = 80f;
        private const float ArenaEndX = 106f;
        private const float ArenaCenterX = 93f;
        private const float ArenaWidth = 26f;
        private const float CeilingY = 12f;
        private const float CorridorCenterY = 4.3f;
        // A arte do corredor tem uma faixa de piso na base (~17,9% da altura).
        // A camada e dimensionada e deslocada para que essa faixa fique abaixo
        // do piso fisico (y = 0), alinhando arte e colisao.
        private const float LayerHeight = 14.5f;
        private const float LayerFloorFraction = 0.179f;
        private const float CorridorOrtho = 5.2f;
        private const float ArenaTriggerX = 78f;
        private const float PlayerSpawnX = 2f;
        private const float PlayerGravityScale = 3.2f;

        // Portal corredor -> arena.
        private const float PortalX = 77f;
        private const float ArenaSpawnX = 84f;

        // Pilares que fecham a arena nas duas bordas do enquadramento da camera.
        private const float ArenaPillarLeftX = 80.6f;
        private const float ArenaPillarRightX = 105.4f;

        // ---- Ordem de desenho ---------------------------------------------------
        private const int OrderBack = -100;
        private const int OrderFar = -90;
        private const int OrderMiddle = -80;
        private const int OrderNear = -70;
        private const int OrderPortalWall = -26;
        private const int OrderPortalDarkness = -24;
        private const int OrderPortalArch = -23;
        private const int OrderArenaPillarFrame = -20;
        private const int OrderPlatform = -30;
        private const int OrderArenaBackdrop = -68;
        private const int OrderArenaSky = -60;
        private const int OrderArenaArc = -55;
        private const int OrderArenaPillar = -50;
        private const int OrderArenaTorchPillar = -42;
        private const int OrderArenaFloor = -35;
        // O piso do corredor fica atras do fundo solido da arena, para nao
        // "vazar" um bloco roxo na borda esquerda do enquadramento da arena.
        private const int OrderCorridorFloor = -69;
        // O jogador sempre na frente de qualquer elemento de cenario.
        private const int OrderPlayer = 500;
        private const string PlayerSortingLayer = "Player";

        private static readonly Color BackgroundColor = new Color32(0x0D, 0x07, 0x16, 0xFF);

        [MenuItem("Vazio/Construir Cena do Vazio (2D)")]
        public static void BuildFromMenu()
        {
            Build();
        }

        /// <summary>Ponto de entrada para: Unity.exe -executeMethod Vazio.EditorTools.VazioLevelBuilder.Build</summary>
        public static void Build()
        {
            Debug.Log("[Vazio] Iniciando construcao da cena...");

            ConfigureTextures();
            AssetDatabase.Refresh();

            Material unlit = GetOrCreateUnlitMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            new GameObject("Vazio");

            EnsurePlayerSortingLayer();

            GameObject magicPrefab = BuildMagicPrefab(unlit);
            Sprite[] bookPool = LoadBookSprites();
            GameObject bookPrefab = BuildBookPrefab(unlit, bookPool);
            GameObject laserPrefab = BuildLaserPrefab(unlit);
            GameObject puddlePrefab = BuildPuddlePrefab(unlit);

            Transform player = BuildPlayer(unlit, magicPrefab);
            Transform cameraTransform = BuildCamera(player);
            BuildCorridorBackground(unlit, cameraTransform);
            BuildCorridorFloor(unlit);
            BuildArena(unlit);
            BuildArenaBoundaryPillars(unlit);
            BuildPlatforms(unlit);

            Transform arenaSpawn = BuildArenaSpawn();
            CanvasGroup fade = BuildFadeCanvas();

            ArchivistBoss boss = BuildBoss(unlit, player, bookPrefab, laserPrefab, puddlePrefab);
            BuildPortal(unlit, arenaSpawn, fade, cameraTransform.GetComponent<CameraFollow2D>(), boss.gameObject);

            BuildColliders();

            BuildHud(unlit, player.GetComponent<PlayerStats>(), boss);

            ApplyPhysicsSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            Directory.CreateDirectory(SceneDir);
            EditorSceneManager.SaveScene(scene, ScenePath);

            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Vazio] Cena construida em " + ScenePath);
        }

        // ------------------------------------------------------------------ setup
        private static void ConfigureTextures()
        {
            // Arte pixel art do corredor (6 pixels por unidade, filtro pontual).
            foreach (string file in new[] { "back", "far", "middle", "near", "foreground" })
            {
                ConfigureTexture(Art + "/Background/" + file + ".png", 6f, true, 2048, true);
            }

            ConfigureTexture(Art + "/Environment/tileset.png", 6f, true, 2048, true);
            ConfigureTexture(PlayerSpritePath, 13.3333f, true, 2048, true);
            ConfigurePlayerSprites();
            ConfigureTexture(Art + "/Effects/torch-sheet.png", 6f, true, 2048, true);
            ConfigureTexture(Art + "/Effects/white.png", 4f, false, 64, true);
            ConfigureTexture(Art + "/Environment/platform.png", 16f, true, 256, true);
            ConfigureTexture(Art + "/Environment/pillar.png", 8f, true, 256, true);
            ConfigureTexture(Art + "/Environment/portal_arch.png", 16f, true, 512, true);
            ConfigureTexture(Art + "/Combat/archivist.png", 16f, true, 512, true);
            ConfigureTexture(Art + "/Combat/magic_orb.png", 16f, true, 256, true);
            ConfigureTexture(Art + "/Combat/book.png", 16f, true, 256, true);
            ConfigureTexture(Art + "/Combat/laser_beam.png", 16f, true, 512, true);
            ConfigureTexture(Art + "/Combat/puddle.png", 16f, true, 512, true);
            ConfigureBossSpriteSheets();

            // Arte pintada da arena: mantem o PPU original, mas garante resolucao cheia.
            ConfigureTexture(Art + "/Background/arena/arena-boss-sky.png", -1f, false, 4096, false);
            ConfigureTexture(Art + "/Background/arena/arena-boss-floor.png", -1f, false, 4096, false);
            ConfigureTexture(Art + "/Background/arena/arena-boss-rocks-arc.png", -1f, false, 4096, false);
            ConfigureTexture(Art + "/Background/arena/arena-boss-rocks-pillar.png", -1f, false, 4096, false);
            ConfigureTexture(Art + "/Background/arena/arena-boss-rocks-torch-pillar.png", -1f, false, 4096, false);
        }

        private static void ConfigureTexture(string path, float pixelsPerUnit, bool pointFilter,
            int maxSize, bool uncompressed)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Vazio] Textura nao encontrada: " + path);
                return;
            }

            bool dirty = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (pixelsPerUnit > 0f && !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                dirty = true;
            }

            FilterMode filter = pointFilter ? FilterMode.Point : FilterMode.Bilinear;
            if (importer.filterMode != filter)
            {
                importer.filterMode = filter;
                dirty = true;
            }

            if (uncompressed && importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.maxTextureSize < maxSize)
            {
                importer.maxTextureSize = maxSize;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        private static Material GetOrCreateUnlitMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                Debug.LogWarning("[Vazio] Shader unlit nao encontrado; usando o material padrao.");
                return null;
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Vazio/Materials"));
            Material material = new Material(shader) { name = "SpriteUnlit" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        // --------------------------------------------------------------- jogador
        private static Transform BuildPlayer(Material unlit, GameObject magicPrefab)
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
            player.transform.position = new Vector3(PlayerSpawnX, 0.92f, 0f);

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = PlayerGravityScale;
            body.freezeRotation = true;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D collider = player.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 1.8f);
            collider.offset = Vector2.zero;
            collider.edgeRadius = 0.02f;

            // Visual (raiz) -> Sprite (pivo nos pes). Assim o agachar reduz a
            // escala da raiz e os pes continuam apoiados no chao.
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform, false);

            GameObject spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(visual.transform, false);
            spriteGo.transform.localPosition = new Vector3(0f, PlayerFeetOffsetY, 0f);

            SpriteRenderer renderer = spriteGo.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            // Camada de sorting propria + ordem alta: o jogador fica sempre na
            // frente de todos os elementos do cenario.
            renderer.sortingLayerName = HasSortingLayer(PlayerSortingLayer) ? PlayerSortingLayer : "Default";
            renderer.sortingOrder = OrderPlayer;
            renderer.color = Color.white;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            PlayerVisualAnimator animator = spriteGo.AddComponent<PlayerVisualAnimator>();
            animator.Configure(renderer, idle, run, walk, jump, attack1, attack2, hurt, dead);

            PlayerController controller = player.AddComponent<PlayerController>();
            controller.ConfigurePresentation(visual.transform, renderer);
            controller.ConfigureGravity(PlayerGravityScale);
            controller.ConfigureVisual(animator);

            // Vida/mana e ataque magico do Bibliotecario.
            PlayerStats stats = player.AddComponent<PlayerStats>();
            stats.Configure(controller, renderer, new Vector3(ArenaSpawnX, 1.2f, 0f), animator);

            PlayerCombat combat = player.AddComponent<PlayerCombat>();
            combat.Configure(magicPrefab, stats, controller, animator);

            return player.transform;
        }

        // --------------------------------------------------------------- corredor
        private static void BuildCorridorBackground(Material unlit, Transform cameraTransform)
        {
            GameObject root = new GameObject("Corredor_Fundo");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            CreateParallaxStrip(root.transform, cameraTransform, "Parallax_Back",
                Art + "/Background/back.png", LayerHeight, OrderBack, 0.92f, unlit);
            CreateParallaxStrip(root.transform, cameraTransform, "Parallax_Far",
                Art + "/Background/far.png", LayerHeight, OrderFar, 0.85f, unlit);
            CreateParallaxStrip(root.transform, cameraTransform, "Parallax_Middle",
                Art + "/Background/middle.png", LayerHeight, OrderMiddle, 0.72f, unlit);
            CreateParallaxStrip(root.transform, cameraTransform, "Parallax_Near",
                Art + "/Background/near.png", LayerHeight, OrderNear, 0.55f, unlit);
        }

        private static void CreateParallaxStrip(Transform parent, Transform cameraTransform, string name,
            string spritePath, float targetHeight, int sortingOrder, float factor, Material unlit)
        {
            Sprite sprite = LoadSprite(spritePath);
            if (sprite == null)
            {
                return;
            }

            float scale = targetHeight / sprite.bounds.size.y;
            float tileWidth = sprite.bounds.size.x * scale;

            GameObject strip = new GameObject(name);
            strip.transform.SetParent(parent, false);
            float stripCenterY = targetHeight * (0.5f - LayerFloorFraction);
            strip.transform.position = new Vector3(CorridorStartX - tileWidth, stripCenterY, 0f);

            float travel = ArenaStartX - CorridorStartX;
            int copies = Mathf.CeilToInt((travel * (1f + Mathf.Abs(factor)) + tileWidth * 3f) / tileWidth);

            for (int i = 0; i < copies; i++)
            {
                GameObject tile = new GameObject(name + "_" + i);
                tile.transform.SetParent(strip.transform, false);
                tile.transform.localPosition = new Vector3(i * tileWidth, 0f, 0f);
                tile.transform.localScale = new Vector3(scale, scale, 1f);

                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
                if (unlit != null)
                {
                    renderer.sharedMaterial = unlit;
                }
            }

            ParallaxLayer parallax = strip.AddComponent<ParallaxLayer>();
            parallax.Configure(cameraTransform, factor, tileWidth);
        }

        private static void BuildCorridorFloor(Material unlit)
        {
            GameObject gridGo = new GameObject("Corredor_Grid");
            gridGo.transform.SetParent(GameObject.Find("Vazio").transform, false);
            Grid grid = gridGo.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject floorGo = new GameObject("Piso");
            floorGo.transform.SetParent(gridGo.transform, false);
            Tilemap tilemap = floorGo.AddComponent<Tilemap>();
            TilemapRenderer renderer = floorGo.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = OrderCorridorFloor;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            Tile[] topTiles = LoadTiles(24, 8);
            Tile[] midTiles = LoadTiles(32, 8);
            Tile[] bottomTiles = LoadTiles(40, 8);

            int startColumn = Mathf.FloorToInt(CorridorStartX);
            int endColumn = Mathf.FloorToInt(ArenaStartX) - 1;

            for (int x = startColumn; x <= endColumn; x++)
            {
                int variation = Mathf.Abs(x) % topTiles.Length;
                Tile tile = topTiles[variation];
                if (tile != null)
                {
                    tilemap.SetTile(new Vector3Int(x, -1, 0), tile);
                }

                tile = midTiles[variation];
                if (tile != null)
                {
                    tilemap.SetTile(new Vector3Int(x, -2, 0), tile);
                }

                tile = bottomTiles[variation];
                if (tile != null)
                {
                    tilemap.SetTile(new Vector3Int(x, -3, 0), tile);
                }
            }

            tilemap.CompressBounds();
            tilemap.RefreshAllTiles();
        }

        private static Tile[] LoadTiles(int first, int count)
        {
            Tile[] tiles = new Tile[count];
            for (int i = 0; i < count; i++)
            {
                tiles[i] = AssetDatabase.LoadAssetAtPath<Tile>(
                    Art + "/Environment/Tiles/tileset_" + (first + i) + ".asset");
            }

            return tiles;
        }

        // ------------------------------------------------------------------ arena
        private static void BuildArena(Material unlit)
        {
            GameObject root = new GameObject("Arena");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            // Fundo solido: esconde a arte do corredor atras da arena, que sem
            // isto aparecia atraves das areas transparentes dos assets pintados.
            Sprite white = LoadSprite(Art + "/Effects/white.png");
            GameObject backdrop = CreateSprite("Arena_Fundo", root.transform, white,
                new Vector3(ArenaCenterX, 6f, 0f), new Vector2(30f, 18f), OrderArenaBackdrop, unlit);
            if (backdrop != null)
            {
                backdrop.GetComponent<SpriteRenderer>().color = new Color32(0x12, 0x0A, 0x1C, 0xFF);
            }

            Sprite sky = LoadSprite(Art + "/Background/arena/arena-boss-sky.png");
            CreateSprite("Arena_Ceu", root.transform, sky, new Vector3(ArenaCenterX, 9.2f, 0f),
                new Vector2(30f, 10f), OrderArenaSky, unlit);

            Sprite arc = LoadSprite(Art + "/Background/arena/arena-boss-rocks-arc.png", "arc_0");
            CreateSprite("Arena_Arcos", root.transform, arc, new Vector3(ArenaCenterX, 4.9f, 0f),
                new Vector2(30f, 2.2f), OrderArenaArc, unlit);

            Sprite pillar = LoadSprite(Art + "/Background/arena/arena-boss-rocks-pillar.png", "pillar_0");
            CreateSprite("Arena_Pilares", root.transform, pillar, new Vector3(ArenaCenterX, 3.1f, 0f),
                new Vector2(30f, 1.6f), OrderArenaPillar, unlit);

            Sprite torchPillar = LoadSprite(Art + "/Background/arena/arena-boss-rocks-torch-pillar.png",
                "torch-pillar_2");
            CreateSprite("Arena_PilaresTocha_Esq", root.transform, torchPillar,
                new Vector3(ArenaCenterX - 7f, 1.6f, 0f), new Vector2(14f, 3.2f),
                OrderArenaTorchPillar, unlit);
            CreateSprite("Arena_PilaresTocha_Dir", root.transform, torchPillar,
                new Vector3(ArenaCenterX + 7f, 1.6f, 0f), new Vector2(14f, 3.2f),
                OrderArenaTorchPillar, unlit);

            Sprite floor = LoadSprite(Art + "/Background/arena/arena-boss-floor.png");
            CreateSprite("Arena_Piso", root.transform, floor, new Vector3(ArenaCenterX, -0.8f, 0f),
                new Vector2(30f, 1.6f), OrderArenaFloor, unlit);
        }

        private static GameObject CreateSprite(string name, Transform parent, Sprite sprite, Vector3 position,
            Vector2 targetSize, int sortingOrder, Material unlit)
        {
            if (sprite == null)
            {
                Debug.LogWarning("[Vazio] Sprite ausente: " + name);
                return null;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            Vector3 size = sprite.bounds.size;
            go.transform.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.0001f, size.x),
                targetSize.y / Mathf.Max(0.0001f, size.y),
                1f);

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            return go;
        }

        // ------------------------------------------------------- pilares/limites
        private static void BuildArenaBoundaryPillars(Material unlit)
        {
            Sprite sprite = LoadSprite(Art + "/Environment/pillar.png");
            GameObject root = new GameObject("Arena_Limites");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            CreateStandingPillar(root.transform, sprite, "Pilar_Esquerda", ArenaPillarLeftX, unlit);
            CreateStandingPillar(root.transform, sprite, "Pilar_Direita", ArenaPillarRightX, unlit);
        }

        private static void CreateStandingPillar(Transform parent, Sprite sprite, string name, float x,
            Material unlit)
        {
            GameObject go = CreateSprite(name, parent, sprite, new Vector3(x, 4.6f, 0f),
                new Vector2(2.6f, 9.2f), OrderArenaPillarFrame, unlit);

            if (go == null)
            {
                return;
            }

            // O collider usa o tamanho nativo do sprite (4 x 8) e e' escalado
            // junto com o transform, resultando em 2,6 x 9,2 no mundo.
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = sprite.bounds.size;
            box.offset = Vector2.zero;
        }

        // -------------------------------------------------------------- plataformas
        private static void BuildPlatforms(Material unlit)
        {
            Sprite sprite = LoadSprite(Art + "/Environment/platform.png");
            if (sprite == null)
            {
                Debug.LogWarning("[Vazio] Sprite de plataforma ausente.");
                return;
            }

            GameObject root = new GameObject("Plataformas");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            // (x, topo da plataforma, largura)
            (float x, float topY, float width)[] platforms =
            {
                (86.5f, 2.6f, 4.5f),
                (99.5f, 2.6f, 4.5f),
                (93.0f, 4.6f, 5.0f),
                (89.5f, 6.4f, 3.6f),
                (96.5f, 6.4f, 3.6f),
            };

            const float height = 0.8f;

            foreach ((float x, float topY, float width) in platforms)
            {
                GameObject go = CreateSprite("Plataforma", root.transform, sprite,
                    new Vector3(x, topY - height * 0.5f, 0f), new Vector2(width, height),
                    OrderPlatform, unlit);

                if (go == null)
                {
                    continue;
                }

                BoxCollider2D box = go.AddComponent<BoxCollider2D>();
                box.size = sprite.bounds.size;
                box.offset = Vector2.zero;
                box.usedByEffector = true;

                // Plataforma de sentido unico: so colide por cima, permitindo
                // subir por baixo e pousar em cima com o pulo.
                PlatformEffector2D effector = go.AddComponent<PlatformEffector2D>();
                effector.useOneWay = true;
                effector.useSideFriction = false;
                effector.useSideBounce = false;
                effector.rotationalOffset = 0f;
                effector.surfaceArc = 180f;
            }
        }

        // ------------------------------------------------------------------ portal
        private static Transform BuildArenaSpawn()
        {
            GameObject go = new GameObject("Spawn_Arena");
            go.transform.SetParent(GameObject.Find("Vazio").transform, false);
            go.transform.position = new Vector3(ArenaSpawnX, 1.05f, 0f);
            return go.transform;
        }

        private static CanvasGroup BuildFadeCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas_Transicao");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject fadeGo = new GameObject("Fade");
            fadeGo.transform.SetParent(canvasGo.transform, false);

            Image image = fadeGo.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 1f);
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CanvasGroup group = fadeGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            return group;
        }

        private static void BuildPortal(Material unlit, Transform arenaSpawn, CanvasGroup fade,
            CameraFollow2D cameraFollow, GameObject bossToActivate)
        {
            GameObject root = new GameObject("Portal");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            Sprite white = LoadSprite(Art + "/Effects/white.png");
            Sprite arch = LoadSprite(Art + "/Environment/portal_arch.png");

            // Parede que fecha o corredor, mergulhada em escuridao. Vai ate o
            // pilar da arena para nao deixar fresta entre o portal e a arena.
            GameObject wall = CreateSprite("Parede_Escuridao", root.transform, white,
                new Vector3(PortalX + 1.0f, 3.45f, 0f), new Vector2(5.6f, 6.9f),
                OrderPortalWall, unlit);
            if (wall != null)
            {
                wall.GetComponent<SpriteRenderer>().color = new Color32(0x0B, 0x07, 0x12, 0xFF);
            }

            // Escuridao total dentro do arco (area do portal).
            GameObject darkness = CreateSprite("Escuridao", root.transform, white,
                new Vector3(PortalX, 3.7f, 0f), new Vector2(4.4f, 7.4f),
                OrderPortalDarkness, unlit);
            if (darkness != null)
            {
                darkness.GetComponent<SpriteRenderer>().color = new Color32(0x04, 0x02, 0x08, 0xFF);
            }

            // Portal animado do pacote (Assets/Portal).
            // Primeira fileira do sheet = idle do portal (8 quadros).
            Sprite[] portalFrames = LoadFrames(PortalSheet);

            Sprite portalSprite = portalFrames.Length > 0 ? portalFrames[0] : null;

            // Os recortes ja sao justos: escala 1 deixa o portal com ~6,1 u de altura.
            const float portalScale = 1f;
            Vector2 portalSize = portalSprite != null
                ? new Vector2(portalSprite.bounds.size.x, portalSprite.bounds.size.y) * portalScale
                : new Vector2(4.4f, 6.2f);

            // Pivo no pe de cada quadro: basta apoiar o objeto no chao para o
            // portal "respirar" sem sair do lugar.
            GameObject portalVisual = CreateSprite("Portal_Visual", root.transform, portalSprite,
                new Vector3(PortalX, 0.05f, 0f), portalSize, OrderPortalArch, unlit);

            if (portalVisual == null)
            {
                Debug.LogWarning("[Vazio] Sprite do portal nao encontrado.");
            }
            else
            {
                SpriteLoopAnimator portalAnimator = portalVisual.AddComponent<SpriteLoopAnimator>();
                portalAnimator.Configure(portalVisual.GetComponent<SpriteRenderer>(), portalFrames, 8f);
            }

            // Trigger de transicao.
            GameObject triggerGo = new GameObject("Trigger_Portal");
            triggerGo.transform.SetParent(root.transform, false);
            triggerGo.transform.position = new Vector3(PortalX, 3f, 0f);

            BoxCollider2D trigger = triggerGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.4f, 6f);

            PortalTransition portal = triggerGo.AddComponent<PortalTransition>();
            portal.Configure(fade, arenaSpawn, cameraFollow, bossToActivate);
        }


        // --------------------------------------------------------------- combate
        private static GameObject BuildMagicPrefab(Material unlit)
        {
            Sprite[] sphereFrames = LoadFrames(PlayerSheetFolder + "Magic_sphere.png");
            Sprite sprite = sphereFrames.Length > 0 ? sphereFrames[0] : LoadSprite(Art + "/Combat/magic_orb.png");

            GameObject go = new GameObject("MagicProjectile");
            go.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = PlayerSortingLayer;
            renderer.sortingOrder = 300;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.35f;

            go.AddComponent<MagicProjectile>();
            return SavePrefab(go, "MagicProjectile");
        }

        private static GameObject BuildBookPrefab(Material unlit, Sprite[] bookPool)
        {
            Sprite sprite = bookPool != null && bookPool.Length > 0 ? bookPool[0] : null;

            GameObject go = new GameObject("ArchivistBook");

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = PlayerSortingLayer;
            renderer.sortingOrder = 250;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            go.AddComponent<ArchivistBook>();
            return SavePrefab(go, "ArchivistBook");
        }

        private static GameObject BuildLaserPrefab(Material unlit)
        {
            Sprite sprite = LoadSprite(Art + "/Combat/laser_beam.png");

            GameObject go = new GameObject("BossLaser");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = PlayerSortingLayer;
            renderer.sortingOrder = 240;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(4f, 1f);

            go.AddComponent<BossLaser>();
            return SavePrefab(go, "BossLaser");
        }

        private static GameObject BuildPuddlePrefab(Material unlit)
        {
            Sprite sprite = LoadSprite(Art + "/Combat/puddle.png");

            GameObject go = new GameObject("CorruptedPuddle");
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = -25;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(4f, 1.25f);

            go.AddComponent<CorruptedPuddle>();
            return SavePrefab(go, "CorruptedPuddle");
        }

        private static GameObject SavePrefab(GameObject go, string fileName)
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Vazio/Prefabs"));
            string path = "Assets/Vazio/Prefabs/" + fileName + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log("[Vazio] Prefab criado: " + path);
            return prefab;
        }

        // ------------------------------------------------------------------- boss
        private static ArchivistBoss BuildBoss(Material unlit, Transform player, GameObject bookPrefab,
            GameObject laserPrefab, GameObject puddlePrefab)
        {
            GameObject go = new GameObject("Arquivista");
            go.transform.SetParent(GameObject.Find("Vazio").transform, false);
            go.transform.position = new Vector3(99f, 2.4f, 0f);

            Sprite[] idle = LoadFrames(BossSprites + "Idle.png");
            Sprite[] run = LoadFrames(BossSprites + "Run.png");
            Sprite[] jump = LoadFrames(BossSprites + "Jump.png");
            Sprite[] fall = LoadFrames(BossSprites + "Fall.png");
            Sprite[] attack1 = LoadFrames(BossSprites + "Attack1.png");
            Sprite[] attack2 = LoadFrames(BossSprites + "Attack2.png");
            Sprite[] hit = LoadFrames(BossSprites + "Take hit.png");
            Sprite[] death = LoadFrames(BossSprites + "Death.png");

            GameObject visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(go.transform, false);
            visualGo.transform.localPosition = new Vector3(0f, -1.65f, 0f);

            SpriteRenderer renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = idle.Length > 0 ? idle[0] : null;
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 10;
            if (unlit != null)
            {
                renderer.sharedMaterial = unlit;
            }

            BossVisualAnimator animator = visualGo.AddComponent<BossVisualAnimator>();
            animator.Configure(renderer, idle, run, jump, fall, attack1, attack2, hit, death);

            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.constraints |= RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;

            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.6f, 3.3f);

            GameObject tip = new GameObject("StaffTip");
            tip.transform.SetParent(go.transform, false);
            tip.transform.localPosition = new Vector3(0.95f, 0.9f, 0f);

            ArchivistBoss boss = go.AddComponent<ArchivistBoss>();
            PlayerStats stats = player.GetComponent<PlayerStats>();
            Sprite[] bookPool = LoadBookSprites();
            boss.Configure(player, stats, tip.transform, bookPrefab, laserPrefab, puddlePrefab, renderer,
                animator, bookPool);

            // O chefe so entra em cena depois que o jogador atravessa o portal.
            go.SetActive(false);

            return boss;
        }

        // -------------------------------------------------------------------- hud
        private static void BuildHud(Material unlit, PlayerStats stats, ArchivistBoss boss)
        {
            Sprite white = LoadSprite(Art + "/Effects/white.png");

            GameObject canvasGo = new GameObject("Canvas_HUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameHud hud = canvasGo.AddComponent<GameHud>();

            Image health = CreateBar(canvasGo.transform, "Barra_Vida", white,
                new Vector2(0f, 1f), new Vector2(24f, -78f), new Vector2(280f, 22f),
                new Color(0.86f, 0.22f, 0.28f, 1f));

            Image mana = CreateBar(canvasGo.transform, "Barra_Mana", white,
                new Vector2(0f, 1f), new Vector2(24f, -106f), new Vector2(280f, 14f),
                new Color(0.2f, 0.55f, 1f, 1f));

            GameObject bossBar = new GameObject("Barra_Chefe");
            bossBar.transform.SetParent(canvasGo.transform, false);
            RectTransform bossRect = bossBar.AddComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0.5f, 1f);
            bossRect.anchorMax = new Vector2(0.5f, 1f);
            bossRect.pivot = new Vector2(0.5f, 0.5f);
            bossRect.sizeDelta = new Vector2(680f, 26f);
            bossRect.anchoredPosition = new Vector2(0f, -34f);

            CanvasGroup bossGroup = bossBar.AddComponent<CanvasGroup>();

            Image bossFill = CreateBar(bossBar.transform, "Chefe", white,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 26f),
                new Color(0.62f, 0.3f, 0.86f, 1f), true);

            hud.Configure(stats, boss, health, mana, bossFill, bossGroup);
        }

        private static Image CreateBar(Transform parent, string name, Sprite sprite, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, Color fillColor, bool centered = false)
        {
            GameObject background = new GameObject(name + "_Fundo");
            background.transform.SetParent(parent, false);

            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.sprite = sprite;
            backgroundImage.color = new Color(0.05f, 0.03f, 0.08f, 0.85f);
            backgroundImage.raycastTarget = false;

            RectTransform backgroundRect = backgroundImage.rectTransform;
            backgroundRect.anchorMin = anchor;
            backgroundRect.anchorMax = anchor;
            backgroundRect.pivot = centered ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
            backgroundRect.sizeDelta = size;
            backgroundRect.anchoredPosition = anchoredPosition;

            GameObject fillGo = new GameObject(name + "_Preenchimento");
            fillGo.transform.SetParent(background.transform, false);

            Image fill = fillGo.AddComponent<Image>();
            fill.sprite = sprite;
            fill.color = fillColor;
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            return fill;
        }

        // -------------------------------------------------------------- colisores
        private static void BuildColliders()
        {
            GameObject root = new GameObject("Colisores");
            root.transform.SetParent(GameObject.Find("Vazio").transform, false);

            float corridorWidth = ArenaStartX - CorridorStartX;

            AddBox(root.transform, "Piso_Corredor",
                new Vector3(CorridorStartX + corridorWidth * 0.5f, -0.5f, 0f),
                new Vector2(corridorWidth, 1f));

            // O piso da arena comeca atras do portal (x = 78) para que o
            // jogador nunca caia no vao entre o corredor e a arena.
            AddBox(root.transform, "Piso_Arena",
                new Vector3((PortalX + 1f + ArenaEndX) * 0.5f, -0.5f, 0f),
                new Vector2(ArenaEndX - PortalX - 1f, 1f));

            AddBox(root.transform, "Parede_Esquerda",
                new Vector3(CorridorStartX - 0.5f, CeilingY * 0.5f, 0f),
                new Vector2(1f, CeilingY + 4f));

            AddBox(root.transform, "Parede_Direita_Arena",
                new Vector3(ArenaEndX + 0.5f, CeilingY * 0.5f, 0f),
                new Vector2(1f, CeilingY + 4f));

            AddBox(root.transform, "Teto_Corredor",
                new Vector3(CorridorStartX + corridorWidth * 0.5f, CeilingY + 0.5f, 0f),
                new Vector2(corridorWidth, 1f));

            AddBox(root.transform, "Teto_Arena",
                new Vector3(ArenaCenterX, CeilingY + 0.5f, 0f),
                new Vector2(ArenaWidth, 1f));

            // Os limites laterais da arena sao os pilares visiveis criados em
            // BuildArenaBoundaryPillars (cada um ja carrega seu BoxCollider2D).
        }

        private static void AddBox(Transform parent, string name, Vector3 center, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        // ---------------------------------------------------------------- camera
        private static Transform BuildCamera(Transform player)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(PlayerSpawnX + 1.8f, CorridorCenterY, -10f);

            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CorridorOrtho;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            camera.depth = -1f;
            camera.allowHDR = true;

            go.AddComponent<AudioListener>();

            CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
            follow.Configure(player, 1f, PortalX - 3f, CorridorCenterY, CorridorOrtho,
                ArenaTriggerX, new Vector2(ArenaCenterX, 6f),
                new Vector2(ArenaWidth, CeilingY));

            return go.transform;
        }

        private static void ApplyPhysicsSettings()
        {
            Physics2D.gravity = new Vector2(0f, -9.81f);
        }

        private static void RegisterSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // --------------------------------------------------------------- sorting
        private static void EnsurePlayerSortingLayer()
        {
            if (HasSortingLayer(PlayerSortingLayer))
            {
                return;
            }

            const string tagManagerPath = "ProjectSettings/TagManager.asset";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(tagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Vazio] TagManager nao encontrado; usando a Sorting Layer padrao.");
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            if (layers == null)
            {
                Debug.LogWarning("[Vazio] Lista de Sorting Layers nao encontrada.");
                return;
            }

            layers.InsertArrayElementAtIndex(layers.arraySize);
            SerializedProperty added = layers.GetArrayElementAtIndex(layers.arraySize - 1);
            added.FindPropertyRelative("name").stringValue = PlayerSortingLayer;
            added.FindPropertyRelative("uniqueID").intValue = PlayerSortingLayer.GetHashCode();
            added.FindPropertyRelative("locked").boolValue = false;

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            Debug.Log("[Vazio] Sorting Layer criada: " + PlayerSortingLayer);
        }

        private static bool HasSortingLayer(string name)
        {
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name == name)
                {
                    return true;
                }
            }

            return false;
        }


        // ------------------------------------------------------- sprites do boss
        private const string BossSprites = "Assets/Arquivista do vazio/Sprites/";
        private const string BooksFolder = "Assets/Livros/";
        private const string PortalSheet = "Assets/Portal/Purple Portal Sprite Sheet.png";
        private const int PortalFrameCount = 8;

        private static readonly string[] BookFiles =
        {
            "Book_Small_Blue", "Book_Small_BrightRed", "Book_Small_Green", "Book_Small_Grey",
            "Book_Small_LightGreen", "Book_Small_LightRed", "Book_Small_Orange",
            "Book_Small_Purple", "Book_Small_Yellow",
        };

        private static void ConfigurePlayerSprites()
        {
            // As texturas do pacote ja vem fatiadas (128x128, pivo nos pes);
            // aqui so ajustamos a escala e o filtro para o tamanho do jogo.
            // Re-fatiamento uniforme (grade 128x128, pivo nos pes): alguns sheets
            // do pacote vinham com recorte justo e pivo no centro, o que fazia o
            // personagem afundar no chao ao trocar de animacao.
            foreach (string sheet in new[] { "Idle", "Run", "Walk", "Jump", "Attack_1", "Attack_2", "Hurt", "Dead", "Magic_sphere", "Magic_arrow" })
            {
                ConfigureSpriteSheet(PlayerSheetFolder + sheet + ".png", 128, 128, PlayerSpritePPU,
                    new Vector2(0.5f, 0f), sheet);
            }
        }

#pragma warning disable 0618
        private static void NormalizeSpritePivots(string path, Vector2 pivot)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.spriteImportMode != SpriteImportMode.Multiple)
            {
                return;
            }

            SpriteMetaData[] metas = importer.spritesheet;
            if (metas == null || metas.Length == 0)
            {
                return;
            }

            bool dirty = false;
            for (int i = 0; i < metas.Length; i++)
            {
                if (metas[i].alignment != (int)SpriteAlignment.Custom
                    || (metas[i].pivot - pivot).sqrMagnitude > 0.00001f)
                {
                    metas[i].alignment = (int)SpriteAlignment.Custom;
                    metas[i].pivot = pivot;
                    dirty = true;
                }
            }

            if (dirty)
            {
                importer.spritesheet = metas;
                importer.SaveAndReimport();
            }
        }
#pragma warning restore 0618

        private static void ConfigureSpritePixelsPerUnit(string path, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Vazio] Textura do player nao encontrada: " + path);
                return;
            }

            bool dirty = false;

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, ppu))
            {
                importer.spritePixelsPerUnit = ppu;
                dirty = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }

            if (importer.maxTextureSize < 4096)
            {
                importer.maxTextureSize = 4096;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        // Recortes (x, yTop, largura, altura) de cada quadro da primeira fileira,
        // medidos no PNG de 512x192 (celulas de 64x64).
        private static readonly int[,] PortalFrameRects =
        {
            { 12, 14, 29, 43 }, { 23, 16, 19, 41 }, { 22, 14, 25, 43 }, { 18, 16, 30, 41 },
            { 19, 15, 25, 42 }, { 13, 16, 28, 44 }, { 21, 17, 17, 40 }, { 19, 16, 19, 41 },
        };

#pragma warning disable 0618
        private static void ConfigurePortalSheet()
        {
            TextureImporter importer = AssetImporter.GetAtPath(PortalSheet) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Vazio] Sprite sheet do portal nao encontrada.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 7f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);

            int count = PortalFrameRects.GetLength(0);
            SpriteMetaData[] metas = new SpriteMetaData[count];

            for (int i = 0; i < count; i++)
            {
                int cellX = i * 64;
                int x = PortalFrameRects[i, 0];
                int yTop = PortalFrameRects[i, 1];
                int w = PortalFrameRects[i, 2];
                int h = PortalFrameRects[i, 3];
                int rectY = height - (yTop + h);

                metas[i] = new SpriteMetaData
                {
                    name = "Portal_" + i.ToString("D2"),
                    rect = new Rect(cellX + x, rectY, w, h),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                };
            }

            importer.spritesheet = metas;
            importer.SaveAndReimport();
        }
#pragma warning restore 0618

        private static void ConfigureBossSpriteSheets()
        {
            // Tiras horizontais de 250x250 (8 quadros) — pivo nos pes, para o
            // sprite encostar no chao do collider.
            Vector2 feetPivot = new Vector2(0.42f, 0.08f);
            foreach (string sheet in new[] { "Idle", "Run", "Jump", "Fall", "Attack1", "Attack2", "Take hit", "Death" })
            {
                ConfigureSpriteSheet(BossSprites + sheet + ".png", 250, 250, 20f, feetPivot,
                    sheet.Replace(" ", string.Empty));
            }

            // Portal: recortes justos por quadro, com pivo no pe de cada desenho,
            // para a primeira fileira animar sem "pular".
            ConfigurePortalSheet();

            // Cada arquivo de livro e' uma grade 4x4 de 32x32.
            foreach (string book in BookFiles)
            {
                ConfigureSpriteSheet(BooksFolder + book + ".png", 32, 32, 32f, new Vector2(0.5f, 0.5f), book);
            }
        }

#pragma warning disable 0618
        private static void ConfigureSpriteSheet(string path, int frameWidth, int frameHeight, float ppu,
            Vector2 pivot, string prefix)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Vazio] Sprite sheet nao encontrada: " + path);
                return;
            }

            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            int columns = Mathf.Max(1, width / frameWidth);
            int rows = Mathf.Max(1, height / frameHeight);

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 4096;

            SpriteMetaData[] metas = new SpriteMetaData[columns * rows];
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    metas[index] = new SpriteMetaData
                    {
                        name = prefix + "_" + index.ToString("D2"),
                        rect = new Rect(column * frameWidth, (rows - 1 - row) * frameHeight, frameWidth, frameHeight),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = pivot,
                    };
                }
            }

            importer.spritesheet = metas;
            importer.SaveAndReimport();
        }
#pragma warning restore 0618

        private static Sprite[] LoadFrames(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name)
                .ToArray();
        }

        private static Sprite[] LoadBookSprites()
        {
            List<Sprite> pool = new List<Sprite>();

            foreach (string book in BookFiles)
            {
                Sprite[] frames = LoadFrames(BooksFolder + book + ".png");
                if (frames.Length > 0)
                {
                    pool.Add(frames[0]);
                }
            }

            return pool.ToArray();
        }

        // ----------------------------------------------------------------- utils
        private static Sprite LoadSprite(string path, string nameContains = null)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    if (string.IsNullOrEmpty(nameContains) || sprite.name.Contains(nameContains))
                    {
                        return sprite;
                    }
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
