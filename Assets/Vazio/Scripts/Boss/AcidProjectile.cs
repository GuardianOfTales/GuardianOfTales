using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Projetil de acido do Arquivista (Acid VFX 01). Descreve um arco ate o
    /// alvo e, ao encostar no chao, deixa uma poca de acido (Acid VFX 02).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AcidProjectile : MonoBehaviour
    {
        [Header("Animacao (Acid VFX 01)")]
        [SerializeField] private Sprite[] flightFrames;
        [SerializeField, Min(1f)] private float fps = 14f;

        [Header("Voo")]
        [SerializeField, Min(0f)] private float gravity = 26f;
        [SerializeField] private float spinSpeed = 200f;
        [SerializeField, Min(0.1f)] private float maxLife = 3f;
        [SerializeField] private float groundY = 0f;

        [Header("Poca gerada")]
        [SerializeField] private GameObject puddlePrefab;

        private Vector2 _velocity;
        private float _life;
        private bool _landed;
        private SpriteRenderer _renderer;
        private int _frame;
        private float _frameTimer;

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            if (_renderer != null && flightFrames != null && flightFrames.Length > 0)
            {
                _renderer.sprite = flightFrames[0];
            }
        }

        /// <summary>Calcula a velocidade inicial para cair no alvo em flightTime segundos.</summary>
        public void LaunchTo(Vector2 target, float flightTime)
        {
            float t = Mathf.Max(0.15f, flightTime);
            Vector2 delta = target - (Vector2)transform.position;
            _velocity = new Vector2(delta.x / t, delta.y / t + 0.5f * gravity * t);
            _life = 0f;
            _landed = false;
        }

        private void Update()
        {
            if (_landed)
            {
                return;
            }

            float dt = Time.deltaTime;
            _life += dt;

            transform.position += (Vector3)(_velocity * dt);
            _velocity.y -= gravity * dt;

            if (!Mathf.Approximately(spinSpeed, 0f))
            {
                transform.Rotate(0f, 0f, spinSpeed * dt);
            }

            Animate(dt);

            if (transform.position.y <= groundY + 0.05f || _life >= maxLife)
            {
                Land();
            }
        }

        private void Animate(float dt)
        {
            if (_renderer == null || flightFrames == null || flightFrames.Length == 0)
            {
                return;
            }

            float step = 1f / Mathf.Max(1f, fps);
            _frameTimer += dt;

            while (_frameTimer >= step)
            {
                _frameTimer -= step;
                _frame = (_frame + 1) % flightFrames.Length;
                _renderer.sprite = flightFrames[_frame];
            }
        }

        private void Land()
        {
            _landed = true;

            if (puddlePrefab != null)
            {
                Instantiate(puddlePrefab, new Vector3(transform.position.x, groundY, 0f), Quaternion.identity);
            }

            Destroy(gameObject);
        }

        /// <summary>Ajuste usado pelo construtor de cena.</summary>
        public void ConfigureVisuals(Sprite[] frames, GameObject puddle, float ground)
        {
            flightFrames = frames;
            puddlePrefab = puddle;
            groundY = ground;

            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (_renderer != null && frames != null && frames.Length > 0)
            {
                _renderer.sprite = frames[0];
            }
        }
    }
}
