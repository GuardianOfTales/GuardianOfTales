using UnityEngine;

namespace Vazio
{
    /// <summary>Animador simples de sprites em loop (portais, tochas, efeitos).</summary>
    [DisallowMultipleComponent]
    public sealed class SpriteLoopAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float fps = 10f;
        [SerializeField] private bool loop = true;

        private float _timer;
        private int _index;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Apply();
        }

        private void Update()
        {
            if (frames == null || frames.Length <= 1)
            {
                return;
            }

            _timer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(1f, fps);

            while (_timer >= frameTime)
            {
                _timer -= frameTime;
                _index++;

                if (_index >= frames.Length)
                {
                    _index = loop ? 0 : frames.Length - 1;
                    if (!loop)
                    {
                        break;
                    }
                }

                Apply();
            }
        }

        private void Apply()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = frames[Mathf.Clamp(_index, 0, frames.Length - 1)];
        }

        public void Configure(SpriteRenderer renderer, Sprite[] sprites, float framesPerSecond)
        {
            spriteRenderer = renderer;
            frames = sprites;
            fps = framesPerSecond;
            _index = 0;
            _timer = 0f;
            Apply();
        }
    }
}
