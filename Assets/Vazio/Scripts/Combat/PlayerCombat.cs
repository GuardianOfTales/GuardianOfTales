using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Vazio
{
    /// <summary>
    /// Ataque magico do Bibliotecario (Botao Esquerdo do Mouse ou tecla J).
    /// Cadencia de 0,3 s, custo de 10% da mana por tiro e mira automatica no
    /// inimigo vivo mais proximo.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Projetil")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField, Min(0.05f)] private float cooldown = 0.3f;
        [SerializeField, Min(0f)] private float manaCost = 10f;
        [SerializeField, Min(1f)] private float aimRange = 30f;
        [SerializeField] private Vector2 muzzleOffset = new Vector2(0.5f, 0.35f);

        [Header("Referencias")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerVisualAnimator visualAnimator;

        private float _nextShotTime;

        public bool CanShoot => Time.time >= _nextShotTime;
        public float CooldownRemaining => Mathf.Max(0f, _nextShotTime - Time.time);

        private void Awake()
        {
            if (stats == null)
            {
                stats = GetComponent<PlayerStats>();
            }

            if (controller == null)
            {
                controller = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            if (stats != null && !stats.IsAlive)
            {
                return;
            }

            if (Time.time < _nextShotTime)
            {
                return;
            }

            if (AttackPressed())
            {
                Fire();
            }
        }

        private static bool AttackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;

            bool mousePressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool keyPressed = keyboard != null && keyboard.jKey.wasPressedThisFrame;
            return mousePressed || keyPressed;
#else
            return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
#endif
        }

        private void Fire()
        {
            if (projectilePrefab == null)
            {
                return;
            }

            // Sem mana o disparo nem consome a janela de cadencia.
            if (stats != null && !stats.TrySpendMana(manaCost))
            {
                return;
            }

            _nextShotTime = Time.time + cooldown;

            if (visualAnimator != null)
            {
                visualAnimator.PlayTimed(PlayerVisualAnimator.Clip.Attack2, 0.3f);
            }

            Vector2 origin = (Vector2)transform.position
                + new Vector2(muzzleOffset.x * GetFacing(), muzzleOffset.y);

            Transform target = FindNearestEnemy(origin);
            Vector2 direction = target != null
                ? (Vector2)target.position - origin
                : new Vector2(GetFacing(), 0f);

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = new Vector2(GetFacing(), 0f);
            }

            GameObject projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
            MagicProjectile magic = PrepareProjectile(projectile);
            magic.Launch(direction, target);
        }

        /// <summary>
        /// Garante que o prefab escolhido funcione como magia do jogador, mesmo
        /// que venha de outro pacote (ex.: Spell_1/Spell_2): adiciona o
        /// MagicProjectile, normaliza a fisica de trigger e desliga scripts de
        /// projetil concorrentes.
        /// </summary>
        private static MagicProjectile PrepareProjectile(GameObject projectile)
        {
            foreach (MonoBehaviour behaviour in projectile.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null || behaviour is MagicProjectile)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName == "Projectile" || typeName == "SpellProjectile")
                {
                    behaviour.enabled = false;
                }
            }

            MagicProjectile magic = projectile.GetComponent<MagicProjectile>();
            if (magic == null)
            {
                magic = projectile.AddComponent<MagicProjectile>();
            }

            Rigidbody2D body = projectile.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = projectile.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Collider2D collider = projectile.GetComponent<Collider2D>();
            if (collider == null)
            {
                CircleCollider2D circle = projectile.AddComponent<CircleCollider2D>();
                circle.radius = 0.4f;
                collider = circle;
            }

            collider.isTrigger = true;

            SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerName = "Player";
                renderer.sortingOrder = 300;
            }

            return magic;
        }

        private float GetFacing()
        {
            return controller != null ? controller.Facing : 1f;
        }

        private Transform FindNearestEnemy(Vector2 from)
        {
            Transform best = null;
            float bestDistance = aimRange * aimRange;

            foreach (ArchivistBoss boss in ArchivistBoss.Active)
            {
                if (boss == null || !boss.IsAlive)
                {
                    continue;
                }

                float distance = ((Vector2)boss.transform.position - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = boss.transform;
                }
            }

            return best;
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(GameObject prefab, PlayerStats playerStats, PlayerController playerController,
            PlayerVisualAnimator animator)
        {
            projectilePrefab = prefab;
            stats = playerStats;
            controller = playerController;
            visualAnimator = animator;
        }
    }
}
