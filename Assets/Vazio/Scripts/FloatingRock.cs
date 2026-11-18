using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Gira lentamente a pedra flutuante no eixo Z com velocidade aleatoria
    /// (e direcao aleatoria) e aplica opacidade para dar profundidade.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloatingRock : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float minSpeed = 9f;
        [SerializeField, Min(0f)] private float maxSpeed = 18f;
        [SerializeField, Range(0f, 1f)] private float opacity = 0.25f;
        [SerializeField] private bool randomDirection = true;

        private SpriteRenderer spriteRenderer;
        private float speed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyOpacity();
            RollSpeed();
        }

        private void Update()
        {
            if (Mathf.Approximately(speed, 0f))
            {
                return;
            }

            transform.Rotate(0f, 0f, speed * Time.deltaTime);
        }

        private void RollSpeed()
        {
            speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
            if (randomDirection && UnityEngine.Random.value < 0.5f)
            {
                speed = -speed;
            }
        }

        private void ApplyOpacity()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            Color color = spriteRenderer.color;
            color.a = opacity;
            spriteRenderer.color = color;
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(float min, float max, float alpha)
        {
            minSpeed = min;
            maxSpeed = max;
            opacity = alpha;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyOpacity();
        }
    }
}
