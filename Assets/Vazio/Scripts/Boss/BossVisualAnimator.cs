using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Animador leve por sprites: cada "clipe" e' uma tira de frames do pacote
    /// do Arquivista. Evita depender de prefabs de Animator e mantem o controle
    /// exato de Idle / Run / Attack2 / Jump / Fall / Hit / Death.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossVisualAnimator : MonoBehaviour
    {
        public enum Clip { Idle, Run, Jump, Fall, Attack1, Attack2, Hit, Death }

        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] fallFrames;
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2Frames;
        [SerializeField] private Sprite[] hitFrames;
        [SerializeField] private Sprite[] deathFrames;

        [Header("Velocidades (fps)")]
        [SerializeField, Min(1f)] private float idleFps = 8f;
        [SerializeField, Min(1f)] private float runFps = 12f;
        [SerializeField, Min(1f)] private float jumpFps = 9f;
        [SerializeField, Min(1f)] private float fallFps = 8f;
        [SerializeField, Min(1f)] private float attackFps = 14f;
        [SerializeField, Min(1f)] private float hitFps = 10f;
        [SerializeField, Min(1f)] private float deathFps = 8f;

        private Clip _clip = Clip.Idle;
        private Sprite[] _frames;
        private float _fps = 8f;
        private bool _loop = true;
        private float _timer;
        private int _index;

        public Clip CurrentClip => _clip;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            Play(Clip.Idle, true);
        }

        private void Update()
        {
            if (_frames == null || _frames.Length <= 1)
            {
                return;
            }

            _timer += Time.deltaTime;
            float frameTime = 1f / Mathf.Max(1f, _fps);

            while (_timer >= frameTime)
            {
                _timer -= frameTime;
                _index++;

                if (_index >= _frames.Length)
                {
                    if (_loop)
                    {
                        _index = 0;
                    }
                    else
                    {
                        _index = _frames.Length - 1;
                        break;
                    }
                }

                Apply();
            }
        }

        public void Play(Clip clip, bool restart = false)
        {
            if (_clip == clip && !restart)
            {
                return;
            }

            _clip = clip;
            _frames = GetFrames(clip);
            _fps = GetFps(clip);
            _loop = clip == Clip.Idle || clip == Clip.Run;
            _index = 0;
            _timer = 0f;
            Apply();
        }

        private Sprite[] GetFrames(Clip clip)
        {
            switch (clip)
            {
                case Clip.Run: return runFrames;
                case Clip.Jump: return jumpFrames;
                case Clip.Fall: return fallFrames;
                case Clip.Attack1: return attack1Frames;
                case Clip.Attack2: return attack2Frames;
                case Clip.Hit: return hitFrames;
                case Clip.Death: return deathFrames;
                default: return idleFrames;
            }
        }

        private float GetFps(Clip clip)
        {
            switch (clip)
            {
                case Clip.Run: return runFps;
                case Clip.Jump: return jumpFps;
                case Clip.Fall: return fallFps;
                case Clip.Attack1: return attackFps;
                case Clip.Attack2: return attackFps;
                case Clip.Hit: return hitFps;
                case Clip.Death: return deathFps;
                default: return idleFps;
            }
        }

        private void Apply()
        {
            if (spriteRenderer == null || _frames == null || _frames.Length == 0)
            {
                return;
            }

            spriteRenderer.sprite = _frames[Mathf.Clamp(_index, 0, _frames.Length - 1)];
        }

        /// <summary>Configuracao usada pelo construtor de cena.</summary>
        public void Configure(SpriteRenderer renderer, Sprite[] idle, Sprite[] run, Sprite[] jump, Sprite[] fall,
            Sprite[] attack1, Sprite[] attack2, Sprite[] hit, Sprite[] death)
        {
            spriteRenderer = renderer;
            idleFrames = idle;
            runFrames = run;
            jumpFrames = jump;
            fallFrames = fall;
            attack1Frames = attack1;
            attack2Frames = attack2;
            hitFrames = hit;
            deathFrames = death;
        }
    }
}
