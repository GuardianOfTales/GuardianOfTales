using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Animador do Bibliotecario usando os sprites de Assets/Player.
    /// Locomocao (Idle/Run/Walk/Jump) vem do estado fisico; ataques, dano e morte
    /// sao "one-shot" com duracao definida.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        public enum Clip { Idle, Run, Walk, Jump, Attack1, Attack2, Hurt, Dead }

        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Frames")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private Sprite[] jumpFrames;
        [SerializeField] private Sprite[] attack1Frames;
        [SerializeField] private Sprite[] attack2Frames;
        [SerializeField] private Sprite[] hurtFrames;
        [SerializeField] private Sprite[] deadFrames;

        [Header("Velocidades (fps)")]
        [SerializeField, Min(1f)] private float idleFps = 10f;
        [SerializeField, Min(1f)] private float runFps = 12f;
        [SerializeField, Min(1f)] private float walkFps = 10f;
        [SerializeField, Min(1f)] private float jumpFps = 12f;
        [SerializeField, Min(1f)] private float attackFps = 14f;
        [SerializeField, Min(1f)] private float hurtFps = 12f;
        [SerializeField, Min(1f)] private float deadFps = 6f;

        private Clip _clip = Clip.Idle;
        private Sprite[] _frames;
        private float _fps = 10f;
        private bool _loop = true;
        private float _timer;
        private int _index;
        private float _overrideUntil;

        public Clip CurrentClip => _clip;
        public bool IsBusy => Time.time < _overrideUntil;

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

        /// <summary>Locomocao continua: ignorada enquanto um clipe one-shot roda.</summary>
        public void PlayLocomotion(Clip clip)
        {
            if (Time.time < _overrideUntil)
            {
                return;
            }

            Play(clip);
        }

        /// <summary>Clipe curto (ataque/dano/morte) que sobrepoe a locomocao.</summary>
        public void PlayTimed(Clip clip, float duration)
        {
            _overrideUntil = Time.time + Mathf.Max(0.05f, duration);
            Play(clip, true);
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
            _loop = clip == Clip.Idle || clip == Clip.Run || clip == Clip.Walk;
            _index = 0;
            _timer = 0f;
            Apply();
        }

        private Sprite[] GetFrames(Clip clip)
        {
            switch (clip)
            {
                case Clip.Run: return runFrames;
                case Clip.Walk: return walkFrames;
                case Clip.Jump: return jumpFrames;
                case Clip.Attack1: return attack1Frames;
                case Clip.Attack2: return attack2Frames;
                case Clip.Hurt: return hurtFrames;
                case Clip.Dead: return deadFrames;
                default: return idleFrames;
            }
        }

        private float GetFps(Clip clip)
        {
            switch (clip)
            {
                case Clip.Run: return runFps;
                case Clip.Walk: return walkFps;
                case Clip.Jump: return jumpFps;
                case Clip.Attack1:
                case Clip.Attack2: return attackFps;
                case Clip.Hurt: return hurtFps;
                case Clip.Dead: return deadFps;
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
        public void Configure(SpriteRenderer renderer, Sprite[] idle, Sprite[] run, Sprite[] walk, Sprite[] jump,
            Sprite[] attack1, Sprite[] attack2, Sprite[] hurt, Sprite[] dead)
        {
            spriteRenderer = renderer;
            idleFrames = idle;
            runFrames = run;
            walkFrames = walk;
            jumpFrames = jump;
            attack1Frames = attack1;
            attack2Frames = attack2;
            hurtFrames = hurt;
            deadFrames = dead;
        }
    }
}
