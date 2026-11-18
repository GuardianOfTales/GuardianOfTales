using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Veste a arena com o pacote ArenaDoVazio_Tecnico (atlas transparente),
    /// seguindo a composicao da foto de referencia: faixa de tijolos no topo,
    /// colunas com estatuas encapuzadas nas laterais, cemiterio no piso e
    /// braseiros com as chamas do pacote Arena.
    /// </summary>
    public static class ArenaDresser
    {
        private const string AtlasPath = "Assets/Arena/ArenaDoVazio_Tecnico/Atlas/ArenaDoVazio_Atlas_Transparente.png";
        private const string PieceFolder = "Assets/Vazio/Art/Arena";
        private const string TorchPath = "Assets/Arena/spr_torch.png";
        private const string MaterialPath = "Assets/Vazio/Materials/SpriteUnlit.mat";
        private const string RootName = "Arena_Decoracao";

        private struct Piece
        {
            public string Name;
            public int X, Y, W, H;

            public Piece(string name, int x, int y, int w, int h)
            {
                Name = name; X = x; Y = y; W = w; H = h;
            }
        }

        // Retangulos medidos no atlas (coordenadas de imagem, topo-esquerda).
        private static readonly Piece[] Pieces =
        {
            new Piece("wall_a", 31, 34, 96, 109),
            new Piece("wall_b", 287, 34, 117, 109),
            new Piece("wall_c", 419, 34, 119, 109),
            new Piece("wall_d", 752, 34, 89, 110),
            new Piece("wall_e", 855, 34, 85, 110),
            new Piece("wall_f", 31, 167, 98, 101),
            new Piece("wall_g", 286, 167, 94, 101),
            new Piece("wall_h", 31, 286, 97, 96),
            new Piece("wall_i", 286, 286, 91, 97),
            new Piece("jag_a", 170, 401, 104, 86),
            new Piece("jag_b", 289, 401, 139, 86),
            new Piece("jag_c", 31, 401, 124, 119),
            new Piece("banner_a", 1274, 37, 77, 179),
            new Piece("banner_b", 1359, 37, 78, 188),
            new Piece("hanger", 1323, 808, 44, 51),
            new Piece("statue_l", 672, 407, 93, 207),
            new Piece("statue_r", 780, 407, 94, 208),
            new Piece("column_a", 488, 629, 90, 190),
            new Piece("column_b", 1253, 238, 103, 189),
            new Piece("column_c", 962, 213, 86, 194),
            new Piece("grave_a", 976, 555, 62, 96),
            new Piece("grave_b", 891, 581, 77, 98),
            new Piece("grave_c", 1051, 565, 59, 86),
            new Piece("grave_d", 917, 723, 58, 98),
            new Piece("grave_e", 482, 846, 66, 82),
            new Piece("grave_f", 559, 844, 56, 84),
            new Piece("cross_a", 892, 407, 78, 153),
            new Piece("cross_b", 1001, 435, 47, 102),
            new Piece("cross_c", 1144, 446, 35, 102),
            new Piece("fence", 1192, 455, 131, 90),
            new Piece("gate", 1391, 451, 79, 115),
            new Piece("tree", 30, 495, 263, 331),
            new Piece("rock_a", 582, 608, 112, 111),
            new Piece("rock_b", 170, 668, 97, 105),
            new Piece("rubble", 588, 751, 105, 66),
            new Piece("rock_c", 369, 743, 95, 78),
            new Piece("rock_d", 796, 757, 63, 57),
            new Piece("grass", 203, 773, 72, 48),
            new Piece("brazier", 1390, 813, 57, 51),
            new Piece("tile_a", 1155, 949, 48, 64),
            new Piece("tile_b", 1215, 949, 55, 64),
            new Piece("tile_c", 1283, 949, 55, 64),
            new Piece("tile_d", 1342, 949, 69, 64),
        };

        [MenuItem("Vazio/Arena/Vestir Arena (atlas ArenaDoVazio)")]
        public static void Apply()
        {
            Prepare();
            Dress();
        }

        public static void Prepare()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PieceFolder });
            foreach (string guid in guids)
            {
                SetPointFilter(AssetDatabase.GUIDToAssetPath(guid));
            }

            SetPointFilter(TorchPath);
        }

        public static void Dress()
        {
            Dictionary<string, Sprite> sprites = LoadPieceSprites();
            Sprite[] torches = LoadSprites(TorchPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);

            GameObject arena = GameObject.Find("Arena");
            if (arena == null)
            {
                Debug.LogError("[Arena] Objeto Arena nao encontrado.");
                return;
            }

            // A decoracao anterior NAO e apagada: vira um backup desativado,
            // para nao perder ajustes manuais feitos no cenario.
            ArchivePrevious(arena.transform);

            GameObject root = new GameObject(RootName);
            root.transform.SetParent(arena.transform, false);
            Transform t = root.transform;

            // ---------- Piso (faixa de tijolos na frente) ----------
            string[] floorPieces = { "wall_b", "wall_c", "wall_d", "wall_a", "wall_e" };
            for (int i = 0; i < 13; i++)
            {
                Add(t, sprites, floorPieces[i % floorPieces.Length], "Piso_" + i.ToString("D2"),
                    material, new Vector3(81f + i * 2f, -1.05f, 0f), new Vector2(2.1f, 2.0f), -30);
            }

            // ---------- Faixa de tijolos do topo ----------
            string[] topPieces = { "wall_a", "wall_c", "wall_b", "wall_g", "wall_f", "wall_i", "wall_e", "wall_h", "wall_d" };
            for (int i = 0; i < 9; i++)
            {
                Add(t, sprites, topPieces[i], "Topo_" + i.ToString("D2"), material,
                    new Vector3(80.5f + i * 3f, 10.75f, 0f), new Vector2(3f, 1.9f), -5);
            }

            Add(t, sprites, "jag_b", "Topo_Rachado_1", material, new Vector3(84.5f, 9.85f, 0f), new Vector2(3f, 1.55f), -4);
            Add(t, sprites, "jag_a", "Topo_Rachado_2", material, new Vector3(93f, 9.85f, 0f), new Vector2(2.4f, 1.45f), -4);
            Add(t, sprites, "jag_c", "Topo_Rachado_3", material, new Vector3(101.5f, 9.85f, 0f), new Vector2(2.6f, 1.6f), -4);

            // ---------- Estandartes ----------
            Add(t, sprites, "banner_a", "Estandarte_Esq", material, new Vector3(86.2f, 9.35f, 0f), new Vector2(1.55f, 3.5f), -3);
            Add(t, sprites, "banner_b", "Estandarte_Dir", material, new Vector3(99.8f, 9.35f, 0f), new Vector2(1.55f, 3.5f), -3);
            Add(t, sprites, "hanger", "Pendente_Centro", material, new Vector3(93f, 10.15f, 0f), new Vector2(1.2f, 1.35f), -3);

            // ---------- Colunas com estatuas ----------
            Add(t, sprites, "column_a", "Coluna_Esq", material, new Vector3(82.35f, 2.45f, 0f), new Vector2(1.55f, 3.3f), -10);
            Add(t, sprites, "statue_l", "Estatua_Esq", material, new Vector3(82.35f, 6.15f, 0f), new Vector2(1.7f, 3.9f), -10);

            Add(t, sprites, "column_b", "Coluna_Dir", material, new Vector3(104.3f, 2.45f, 0f), new Vector2(1.75f, 3.3f), -10);
            Add(t, sprites, "statue_r", "Estatua_Dir", material, new Vector3(104.3f, 6.15f, 0f), new Vector2(1.7f, 3.9f), -10);

            // ---------- Cemiterio (esquerda) ----------
            Add(t, sprites, "tree", "Arvore_Seca", material, new Vector3(84.55f, 2.55f, 0f), new Vector2(4.1f, 5.1f), -20);
            Add(t, sprites, "cross_a", "Cruz_Esq", material, new Vector3(86.6f, 1.15f, 0f), new Vector2(0.95f, 1.85f), -20);
            Add(t, sprites, "grave_b", "Lapide_Esq_1", material, new Vector3(87.6f, 0.72f, 0f), new Vector2(0.95f, 1.25f), -20);
            Add(t, sprites, "grave_a", "Lapide_Esq_2", material, new Vector3(85.5f, 0.66f, 0f), new Vector2(0.8f, 1.2f), -20);
            Add(t, sprites, "rock_b", "Pedra_Esq", material, new Vector3(84.2f, 0.55f, 0f), new Vector2(1.35f, 1.4f), -19);
            Add(t, sprites, "grass", "Mato_Esq", material, new Vector3(86.9f, 0.28f, 0f), new Vector2(0.95f, 0.6f), -19);

            // ---------- Cemiterio (direita) ----------
            Add(t, sprites, "grave_c", "Lapide_Dir_1", material, new Vector3(98.6f, 0.65f, 0f), new Vector2(0.85f, 1.2f), -20);
            Add(t, sprites, "grave_d", "Lapide_Dir_2", material, new Vector3(99.7f, 0.7f, 0f), new Vector2(0.8f, 1.3f), -20);
            Add(t, sprites, "cross_b", "Cruz_Dir", material, new Vector3(100.9f, 1.1f, 0f), new Vector2(0.6f, 1.4f), -20);
            Add(t, sprites, "fence", "Cerca_Dir", material, new Vector3(102.4f, 0.8f, 0f), new Vector2(2.3f, 1.5f), -20);
            Add(t, sprites, "gate", "Portao_Dir", material, new Vector3(103.6f, 1.0f, 0f), new Vector2(1.1f, 1.7f), -20);
            Add(t, sprites, "rock_a", "Pedra_Dir", material, new Vector3(101.6f, 0.6f, 0f), new Vector2(1.4f, 1.4f), -19);
            Add(t, sprites, "rubble", "Entulho_Dir", material, new Vector3(102.9f, 0.35f, 0f), new Vector2(1.3f, 0.85f), -19);

            // ---------- Braseiros com chama ----------
            // ---------- Rochas flutuantes (giram devagar, mais transparentes) ----------
            AddRock(t, sprites, "rock_a", "Rocha_Flutuante_1", material, new Vector3(86.8f, 8.55f, 0f), new Vector2(1.7f, 1.7f), 0.62f);
            AddRock(t, sprites, "rock_c", "Rocha_Flutuante_2", material, new Vector3(97.2f, 7.55f, 0f), new Vector2(2.1f, 1.7f), 0.55f);
            AddRock(t, sprites, "rock_d", "Rocha_Flutuante_3", material, new Vector3(90.6f, 9.35f, 0f), new Vector2(1.05f, 0.95f), 0.72f);
            AddRock(t, sprites, "rock_b", "Rocha_Flutuante_4", material, new Vector3(101.2f, 9.0f, 0f), new Vector2(1.25f, 1.3f), 0.66f);

            Add(t, sprites, "brazier", "Braseiro_Esq", material, new Vector3(82.9f, 0.55f, 0f), new Vector2(0.8f, 0.72f), -18);
            Add(t, sprites, "brazier", "Braseiro_Dir", material, new Vector3(103.1f, 0.55f, 0f), new Vector2(0.8f, 0.72f), -18);

            if (torches.Length >= 4)
            {
                MakeFlame(t, "Chama_Braseiro_Esq", torches, material, new Vector3(82.9f, 0.95f, 0f));
                MakeFlame(t, "Chama_Braseiro_Dir", torches, material, new Vector3(103.1f, 0.95f, 0f));
            }

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Arena] Decoracao aplicada: " + t.childCount + " objetos.");
        }

        private const string BackupPrefix = "Arena_Decoracao_Backup_";

        private static void ArchivePrevious(Transform arena)
        {
            List<GameObject> backups = new List<GameObject>();
            for (int i = arena.childCount - 1; i >= 0; i--)
            {
                Transform child = arena.GetChild(i);
                if (child.name == RootName || child.name.StartsWith(BackupPrefix))
                {
                    if (child.name == RootName)
                    {
                        child.name = BackupPrefix + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    }

                    child.gameObject.SetActive(false);
                    backups.Add(child.gameObject);
                }
            }

            // mantem apenas o backup mais recente
            backups.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < backups.Count - 1; i++)
            {
                Object.DestroyImmediate(backups[i]);
            }
        }

        [MenuItem("Vazio/Arena/Backup da Cena (.unity)")]
        public static void BackupScene()
        {
            const string scenePath = "Assets/Vazio/Scenes/Vazio.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError("[Arena] Cena nao encontrada: " + scenePath);
                return;
            }

            string folder = System.IO.Path.Combine("docs", "backups");
            System.IO.Directory.CreateDirectory(folder);
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string target = System.IO.Path.Combine(folder, "Vazio_" + stamp + ".unity");
            System.IO.File.Copy(scenePath, target, true);
            Debug.Log("[Arena] Backup da cena salvo em " + target);
        }

        private static void Add(Transform parent, Dictionary<string, Sprite> sprites, string key, string name,
            Material material, Vector3 position, Vector2 targetSize, int order)
        {
            if (!sprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                Debug.LogWarning("[Arena] Sprite nao encontrado: " + key);
                return;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            Vector3 size = sprite.bounds.size;
            go.transform.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.001f, size.x),
                targetSize.y / Mathf.Max(0.001f, size.y),
                1f);
        }

        private static void AddRock(Transform parent, Dictionary<string, Sprite> sprites, string key, string name,
            Material material, Vector3 position, Vector2 targetSize, float opacity)
        {
            if (!sprites.TryGetValue(key, out Sprite sprite) || sprite == null)
            {
                Debug.LogWarning("[Arena] Sprite nao encontrado: " + key);
                return;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -40;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            Color color = renderer.color;
            color.a = opacity;
            renderer.color = color;

            Vector3 size = sprite.bounds.size;
            go.transform.localScale = new Vector3(
                targetSize.x / Mathf.Max(0.001f, size.x),
                targetSize.y / Mathf.Max(0.001f, size.y),
                1f);

            FloatingRock rock = go.AddComponent<FloatingRock>();
            rock.Configure(2f, 9f, opacity);
        }

        private static void MakeFlame(Transform parent, string name, Sprite[] frames, Material material, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = -17;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            Vector3 size = frames[0].bounds.size;
            go.transform.localScale = new Vector3(0.75f / Mathf.Max(0.001f, size.x), 1.05f / Mathf.Max(0.001f, size.y), 1f);

            SpriteLoopAnimator animator = go.AddComponent<SpriteLoopAnimator>();
            animator.Configure(renderer, frames, 10f);
        }

        // ---------- Importacao ----------

        private static Dictionary<string, Sprite> LoadPieceSprites()
        {
            Dictionary<string, Sprite> map = new Dictionary<string, Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { PieceFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name.StartsWith("arena_"))
                {
                    name = name.Substring("arena_".Length);
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    map[name] = sprite;
                }
            }

            return map;
        }

        private static Sprite[] LoadSprites(string path)
        {
            List<Sprite> sprites = new List<Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((a, b) => ParseIndex(a.name).CompareTo(ParseIndex(b.name)));
            return sprites.ToArray();
        }

        private static int ParseIndex(string name)
        {
            int underscore = name.LastIndexOf('_');
            return underscore >= 0 && int.TryParse(name.Substring(underscore + 1), out int value) ? value : 0;
        }

        private static void SetPointFilter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.filterMode == FilterMode.Point)
            {
                return;
            }

            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }
}
