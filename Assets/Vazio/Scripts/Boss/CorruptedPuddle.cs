using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Poca de acido (fase 2). Fica no chao animando em loop (Acid VFX 02
    /// Repeatable), toca a animacao final (Acid VFX 02 Ending) nos ultimos
    /// instantes e some. Enquanto o jogador estiver em cima, aplica lentidao
    /// e dano periodico.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CorruptedPuddle : MonoBehaviour
    {
        [Header("Vida util")]
        [SerializeField, Min(0.5f)] private float lifetime = 5f;
        [SerializeField, Range(0.1f, 1f)] private float slowMultiplier = 0.45f;
        [SerializeField, Min(0.1f)] private float slowRefresh = 0.4f;
        [SerializeField, Min(0f)] private float damage = 1f;
        [SerializeField, Min(0.1f)] private float damageInterval = 0.8f;

        [Header("Animacao (Acid VFX 02)")]
        [SerializeField] private Sprite[] repeatFrames;
        [SerializeField] private Sprite[] endingFrames;
        [SerializeField, Min(1f)] private float repeatFps = 8f;
        [SerializeField, Min(1f)] private float endingFps = 10f;
        [SerializeField, Min(0.05f)] private float endingDuration = 0.6f;
        [SerializeField, Min(0.1f)] private float fadeDuration = 1f;

        private SpriteRenderer _renderer;
        private float _age;
        private float _damageTimer;
        private Color _baseColor;
        private bool _ending;
        private int _frame;
        private float _frameTimer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null)
            {
                _baseColor = _renderer.color;
                if (repeatFrames != null && repeatFrames.Length > 0)
                {
                    _renderer.sprite = repeatFrames[0];
                }
            }

            _damageTimer = damageInterval;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _age += dt;

            if (_damageTimer > 0f)
            {
                _damageTimer -= dt;
            }

            float remaining = lifetime - _age;
            if (remaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            bool hasEnding = endingFrames != null && endingFrames.Length > 0;

            if (hasEnding)
            {
                if (!_ending && remaining <= endingDuration)
                {
                    _ending = true;
                    _frame = 0;
                    _frameTimer = 0f;
                }

                TickFrames(dt);
            }
            else if (_renderer != null && remaining < fadeDuration)
            {
                Color color = _baseColor;
                color.a = Mathf.Clamp01(remaining / fadeDuration);
                _renderer.color = color;
            }
        }

        private void TickFrames(float dt)
        {
            if (_renderer == null)
            {
                return;
            }

            Sprite[] frames = _ending ? endingFrames : repeatFrames;
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            float step = 1f / Mathf.Max(1f, _ending ? endingFps : repeatFps);
            _frameTimer += dt;

            while (_frameTimer >= step)
            {
                _frameTimer -= step;

                if (_frame < frames.Length - 1)
                {
                    _frame++;
                }
                else if (!_ending)
                {
                    _frame = 0;
                }
            }

            _renderer.sprite = frames[Mathf.Clamp(_frame, 0, frames.Length - 1)];
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other == null)
            {
                return;
            }

            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null)
            {
                return;
            }

            stats.ApplySlow(slowMultiplier, slowRefresh);

            if (_damageTimer <= 0f)
            {
                _damageTimer = damageInterval;
                stats.TakeDamage(damage, transform.position);
            }
        }

        /// <summary>Ajuste usado pelo construtor de cena.</summary>
        public void ConfigureVisuals(Sprite[] repeat, Sprite[] ending, float fpsRepeat, float fpsEnding)
        {
            repeatFrames = repeat;
            endingFrames = ending;
            repeatFps = Mathf.Max(1f, fpsRepeat);
            endingFps = Mathf.Max(1f, fpsEnding);

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_renderer != null && repeatFrames != null && repeatFrames.Length > 0)
            {
                _renderer.sprite = repeatFrames[0];
            }
        }
    }
}
