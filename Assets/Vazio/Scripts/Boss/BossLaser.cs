using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Feixe horizontal rasteiro do Arquivista. A altura e' calibrada para
    /// acertar quem esta em pe (collider ate 1,8 u) e passar por cima de quem
    /// esta agachado (collider ate 0,9 u).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossLaser : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float tickInterval = 0.25f;
        [SerializeField, Min(0f)] private float damage = 3f;
        [SerializeField, Min(0f)] private float lifetime = 0.9f;

        private BoxCollider2D _collider;
        private SpriteRenderer _renderer;
        private float _life;
        private float _tick;
        private Color _baseColor;
        private bool _hasDamaged;
        private Vector2 _damageCenter;
        private PlayerStats _player;
        private Vector2 _damageSize = new Vector2(4f, 1f);

        private void Awake()
        {
            _collider = GetComponent<BoxCollider2D>();
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _life = lifetime;
            _tick = 0f;

            if (_renderer != null)
            {
                _baseColor = _renderer.color;
            }
        }

        public void Configure(float width, float height, float damagePerTick, float duration)
        {
            damage = damagePerTick;
            lifetime = duration;
            _life = duration;

            // Caixa de dano calculada explicitamente: o bounds do collider so
            // acompanha a escala depois de um passo de fisica, o que fazia o
            // primeiro tick acertar quem estava agachado.
            _damageCenter = transform.position;
            _damageSize = new Vector2(width, Mathf.Max(0.2f, height));

            transform.localScale = new Vector3(width / 4f, Mathf.Max(0.2f, height), 1f);
            Physics2D.SyncTransforms();

            if (_collider == null)
            {
                _collider = GetComponent<BoxCollider2D>();
            }

            if (_collider != null)
            {
                _collider.isTrigger = true;
                _collider.size = new Vector2(4f, 1f);
                _collider.offset = Vector2.zero;
            }
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _tick -= Time.deltaTime;

            if (_renderer != null)
            {
                float pulse = 0.85f + Mathf.PingPong(Time.time * 8f, 0.15f);
                Color color = _baseColor;
                color.a = pulse;
                _renderer.color = color;
            }

            if (_tick > 0f)
            {
                return;
            }

            _tick = tickInterval;
            DamageOverlaps();
        }

        private void DamageOverlaps()
        {
            if (_hasDamaged)
            {
                return;
            }

            PlayerStats stats = _player;
            if (stats == null)
            {
                stats = FindAnyObjectByType<PlayerStats>();
                _player = stats;
            }

            if (stats == null || !stats.IsAlive)
            {
                return;
            }

            Collider2D playerCollider = stats.GetComponent<Collider2D>();
            if (playerCollider == null)
            {
                return;
            }

            // AABB explicito: usa os bounds atuais do jogador (que refletem o
            // agachar imediatamente) em vez da forma de fisica, que so e'
            // atualizada no passo seguinte.
            Bounds playerBounds = playerCollider.bounds;
            float halfWidths = (playerBounds.size.x + _damageSize.x) * 0.5f;
            float halfHeights = (playerBounds.size.y + _damageSize.y) * 0.5f;

            bool overlaps = Mathf.Abs(playerBounds.center.x - _damageCenter.x) < halfWidths
                && Mathf.Abs(playerBounds.center.y - _damageCenter.y) < halfHeights;

            if (!overlaps)
            {
                return;
            }

            // Um feixe fere uma unica vez: o dano nao se acumula a cada tick.
            stats.TakeDamage(damage, _damageCenter);
            _hasDamaged = true;
        }
    }
}
