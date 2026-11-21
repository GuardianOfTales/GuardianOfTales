using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Controle de opacidade do HUD, usado para dar profundidade a elementos de interface.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudOpacity : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float opacity = 0.25f;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            ApplyOpacity();
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
            opacity = alpha;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            ApplyOpacity();
        }
    }
}
