using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Arena de combate modular e reutilizavel.
    ///
    /// Ao entrar no trigger o jogador fica preso entre as paredes invisiveis,
    /// a camera trava no centro da arena e as ondas de inimigos comecam.
    /// Cada cowboy/chefe morto decrementa a contagem; quando zera, o script
    /// segue para a proxima pausa/onda e, no fim, libera paredes e camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatArena : MonoBehaviour
    {
        [Header("Pontos de spawn")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Header("Barreiras invisiveis (BoxCollider2D solido)")]
        [SerializeField] private GameObject invisibleLeftWall;
        [SerializeField] private GameObject invisibleRightWall;

        [Header("Trigger de entrada")]
        [SerializeField] private BoxCollider2D entryTrigger;

        [Header("Prefabs")]
        [SerializeField] private GameObject spawnPortalPrefab;
        [SerializeField] private GameObject cowboyPrefab;
        [SerializeField] private GameObject[] cowboyVariants;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Ondas")]
        [SerializeField, Min(1)] private int enemiesPerWave = 10;
        [SerializeField, Min(0f)] private float spawnInterval = 2f;
        [SerializeField, Min(0f)] private float spawnWarningTime = 0.5f;
        [SerializeField, Min(1)] private int waves = 1;
        [SerializeField, Min(0f)] private float pauseBetweenWaves = 5f;
        [SerializeField, Min(0f)] private float bossSpawnDelay = 5f;

        [Header("Camera")]
        [SerializeField] private WestCameraFollow cameraFollow;
        [SerializeField] private bool lockCamera = true;

        [Header("Poção de cura")]
        [SerializeField] private GameObject potionPrefab;
        [SerializeField] private bool dropPotionOnClear = true;
        [SerializeField] private bool dropPotionDuringCombat;
        [SerializeField, Min(1f)] private float potionDropInterval = 15f;

        private readonly List<CowboyEnemy> _alive = new List<CowboyEnemy>();
        private readonly List<GameObject> _markers = new List<GameObject>();
        private CowboyBoss _boss;
        private PlayerStats _player;
        private bool _started;
        private bool _cleared;

        public bool Started => _started;
        public bool Cleared => _cleared;
        public int AliveCount => _alive.Count;

        /// <summary>Centro da arena (media dos pontos de spawn).</summary>
        public Vector3 Center
        {
            get
            {
                if (spawnPoints == null || spawnPoints.Count == 0)
                {
                    return transform.position;
                }

                Vector3 sum = Vector3.zero;
                int count = 0;
                foreach (Transform point in spawnPoints)
                {
                    if (point != null)
                    {
                        sum += point.position;
                        count++;
                    }
                }

                return count > 0 ? sum / count : transform.position;
            }
        }

        public event Action<CombatArena> ArenaCleared;

        private void Awake()
        {
            if (entryTrigger == null)
            {
                entryTrigger = GetComponent<BoxCollider2D>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<WestCameraFollow>();
            }

            if (invisibleLeftWall != null) invisibleLeftWall.SetActive(false);
            if (invisibleRightWall != null) invisibleRightWall.SetActive(false);
        }

        private void OnEnable()
        {
            _player = FindAnyObjectByType<PlayerStats>();
            if (_player != null)
            {
                _player.Respawned += HandlePlayerRespawned;
            }
        }

        private void OnDisable()
        {
            if (_player != null)
            {
                _player.Respawned -= HandlePlayerRespawned;
            }
        }

        /// <summary>Jogador morreu e renasceu: a arena volta ao estado inicial.</summary>
        private void HandlePlayerRespawned()
        {
            ResetArena();
        }

        /// <summary>
        /// Desfaz o combate: mata os inimigos restantes, desativa paredes,
        /// reativa o trigger e libera a camera (para o jogador refazer a arena).
        /// </summary>
        public void ResetArena()
        {
            StopAllCoroutines();

            _started = false;
            _cleared = false;

            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                CowboyEnemy enemy = _alive[i];
                if (enemy != null)
                {
                    enemy.Died -= HandleEnemyDied;
                    Destroy(enemy.gameObject);
                }
            }

            _alive.Clear();

            foreach (GameObject marker in _markers)
            {
                if (marker != null)
                {
                    Destroy(marker);
                }
            }

            _markers.Clear();

            if (_boss != null)
            {
                _boss.Died -= HandleBossDied;
                Destroy(_boss.gameObject);
                _boss = null;
            }

            if (invisibleLeftWall != null) invisibleLeftWall.SetActive(false);
            if (invisibleRightWall != null) invisibleRightWall.SetActive(false);
            if (entryTrigger != null) entryTrigger.enabled = true;
            if (cameraFollow != null) cameraFollow.Unlock();

            foreach (HealthPotion potion in FindObjectsByType<HealthPotion>(FindObjectsSortMode.None))
            {
                if (potion != null)
                {
                    Destroy(potion.gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_started || other == null)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerStats>() == null && !other.CompareTag("Player"))
            {
                return;
            }

            BeginCombat();
        }

        /// <summary>Comeca o combate (chamado pelo trigger ou manualmente).</summary>
        public void BeginCombat()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            if (entryTrigger != null)
            {
                entryTrigger.enabled = false;
            }

            if (invisibleLeftWall != null) invisibleLeftWall.SetActive(true);
            if (invisibleRightWall != null) invisibleRightWall.SetActive(true);

            if (lockCamera && cameraFollow != null)
            {
                // Alinha apenas o X no trigger e mantem o Y atual da camera
                // (usar o Y do trigger deixava a visao baixa demais).
                Vector3 lockPoint = entryTrigger != null ? entryTrigger.transform.position : Center;
                cameraFollow.LockAt(new Vector3(lockPoint.x, cameraFollow.transform.position.y, cameraFollow.transform.position.z));
            }

            StartCoroutine(CombatRoutine());

            if (dropPotionDuringCombat && potionPrefab != null)
            {
                StartCoroutine(PotionRoutine());
            }
        }

        private System.Collections.IEnumerator PotionRoutine()
        {
            while (!_cleared)
            {
                yield return new WaitForSeconds(potionDropInterval);

                if (_cleared)
                {
                    yield break;
                }

                DropPotion();
            }
        }

        /// <summary>Instancia a poção na posição do trigger; ela cai com gravidade.</summary>
        public void DropPotion()
        {
            if (potionPrefab == null)
            {
                return;
            }

            Vector3 position = entryTrigger != null ? entryTrigger.transform.position : transform.position;
            Instantiate(potionPrefab, position, Quaternion.identity);
        }

        private IEnumerator CombatRoutine()
        {
            for (int wave = 0; wave < Mathf.Max(1, waves); wave++)
            {
                if (wave > 0)
                {
                    yield return new WaitForSeconds(pauseBetweenWaves);
                }

                _alive.Clear();

                for (int i = 0; i < enemiesPerWave; i++)
                {
                    // Aviso: mini portal no ponto 0,5 s antes do cowboy nascer.
                    Transform point = PickSpawnPoint();
                    GameObject marker = SpawnMarker(point);

                    if (marker != null && spawnWarningTime > 0f)
                    {
                        yield return new WaitForSeconds(spawnWarningTime);
                        Destroy(marker);
                        _markers.Remove(marker);
                    }

                    SpawnEnemyAt(point);

                    float remaining = Mathf.Max(0f, spawnInterval - (marker != null ? spawnWarningTime : 0f));
                    yield return new WaitForSeconds(remaining);
                }

                // espera a onda ser totalmente eliminada
                while (_alive.Count > 0)
                {
                    yield return null;
                }
            }

            if (bossPrefab != null)
            {
                yield return new WaitForSeconds(bossSpawnDelay);
                SpawnBoss();

                while (_boss != null && _boss.IsAlive)
                {
                    yield return null;
                }
            }

            Finish();
        }

        private Transform PickSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                return null;
            }

            return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        }

        private GameObject SpawnMarker(Transform point)
        {
            if (spawnPortalPrefab == null || point == null)
            {
                return null;
            }

            GameObject marker = Instantiate(spawnPortalPrefab, point.position, Quaternion.identity);
            _markers.Add(marker);
            GameAudio.Instance?.PortalOpen();
            return marker;
        }

        private void SpawnEnemyAt(Transform point)
        {
            GameObject prefab = PickCowboyPrefab();
            if (prefab == null || point == null)
            {
                return;
            }

            GameObject go = Instantiate(prefab, point.position, Quaternion.identity);
            CowboyEnemy enemy = go.GetComponent<CowboyEnemy>();
            if (enemy == null)
            {
                return;
            }

            enemy.Died += HandleEnemyDied;
            _alive.Add(enemy);
        }

        private GameObject PickCowboyPrefab()
        {
            if (cowboyVariants != null && cowboyVariants.Length > 0)
            {
                GameObject candidate = cowboyVariants[UnityEngine.Random.Range(0, cowboyVariants.Length)];
                if (candidate != null)
                {
                    return candidate;
                }
            }

            return cowboyPrefab;
        }

        private void SpawnBoss()
        {
            Vector3 position = bossSpawnPoint != null ? bossSpawnPoint.position : Center;
            // O chefe cowboy mantem a musica padrao do velho oeste.
            GameObject go = Instantiate(bossPrefab, position, Quaternion.identity);
            _boss = go.GetComponent<CowboyBoss>();

            if (_boss != null)
            {
                _boss.Died += HandleBossDied;
            }
        }

        private void HandleEnemyDied(CowboyEnemy enemy)
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
            }

            _alive.Remove(enemy);
        }

        private void HandleBossDied(CowboyBoss boss)
        {
            if (boss != null)
            {
                boss.Died -= HandleBossDied;
            }
        }

        private void Finish()
        {
            if (_cleared)
            {
                return;
            }

            _cleared = true;

            if (invisibleLeftWall != null) invisibleLeftWall.SetActive(false);
            if (invisibleRightWall != null) invisibleRightWall.SetActive(false);

            if (cameraFollow != null)
            {
                cameraFollow.Unlock();
            }

            if (dropPotionOnClear)
            {
                DropPotion();
            }

            if (bossPrefab != null)
            {
                GameAudio.Instance?.PlayWestMusic();
            }

            ArenaCleared?.Invoke(this);
            Debug.Log("[CombatArena] Arena limpa: " + name);
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(List<Transform> points, GameObject leftWall, GameObject rightWall, BoxCollider2D trigger,
            GameObject cowboy, GameObject[] variants, GameObject boss, Transform bossPoint,
            int perWave, float interval, int waveCount, float wavePause, float bossDelay,
            WestCameraFollow follow, GameObject potion, bool potionDuringCombat = false, float potionInterval = 15f,
            GameObject spawnPortal = null, float warningTime = 0.5f)
        {
            spawnPortalPrefab = spawnPortal;
            spawnWarningTime = Mathf.Max(0f, warningTime);
            potionPrefab = potion;
            dropPotionOnClear = potion != null;
            dropPotionDuringCombat = potionDuringCombat;
            potionDropInterval = Mathf.Max(1f, potionInterval);
            spawnPoints = points ?? new List<Transform>();
            invisibleLeftWall = leftWall;
            invisibleRightWall = rightWall;
            entryTrigger = trigger;
            cowboyPrefab = cowboy;
            cowboyVariants = variants;
            bossPrefab = boss;
            bossSpawnPoint = bossPoint;
            enemiesPerWave = Mathf.Max(1, perWave);
            spawnInterval = Mathf.Max(0f, interval);
            waves = Mathf.Max(1, waveCount);
            pauseBetweenWaves = Mathf.Max(0f, wavePause);
            bossSpawnDelay = Mathf.Max(0f, bossDelay);
            cameraFollow = follow;

            if (invisibleLeftWall != null) invisibleLeftWall.SetActive(false);
            if (invisibleRightWall != null) invisibleRightWall.SetActive(false);
        }
    }
}
