using UnityEngine;

namespace Vazio
{
    /// <summary>Animador dos cowboys: Idle/Walk/Run/Jump/Shot/Special/Charging/Hurt/Dead/Stance.</summary>
    [DisallowMultipleComponent]
    public sealed class CowboyVisualAnimator : MonoBehaviour
    {
        public enum Clip { Idle, Walk, Run, Jump, Shot, Special, Charging, Hurt, Dead, Stance }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] idle;
        [SerializeField] private Sprite[] walk;
        [SerializeField] private Sprite[] run;
        [SerializeField] private Sprite[] jump;
        [SerializeField] private Sprite[] shot;
        [SerializeField] private Sprite[] special;
        [SerializeField] private Sprite[] charging;
        [SerializeField] private Sprite[] hurt;
        [SerializeField] private Sprite[] dead;
        [SerializeField] private Sprite[] stance;
        [SerializeField, Min(1f)] private float fps = 10f;

        private Sprite[] _frames;
        private int _index;
        private float _timer;
        private float _overrideUntil = -1f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            Play(Clip.Idle, true);
        }

        public void PlayLocomotion(Clip clip)
        {
            if (Time.time < _overrideUntil)
            {
                return;
            }

            Play(clip, true);
        }

        public void PlayTimed(Clip clip, float duration)
        {
            Play(clip, false);
            _overrideUntil = Time.time + Mathf.Max(0.05f, duration);
        }

        public void SetFacing(float direction)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = direction < 0f;
            }
        }

        private void Update()
        {
            if (_frames == null || _frames.Length <= 1)
            {
                return;
            }

            _timer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, fps);

            while (_timer >= step)
            {
                _timer -= step;
                _index = (_index + 1) % _frames.Length;
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = _frames[_index];
                }
            }
        }

        private void Play(Clip clip, bool loop)
        {
            Sprite[] frames = FramesOf(clip);
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            if (_frames != frames)
            {
                _frames = frames;
                _index = 0;
                _timer = 0f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frames[Mathf.Min(_index, frames.Length - 1)];
            }

            _ = loop;
        }

        private Sprite[] FramesOf(Clip clip)
        {
            switch (clip)
            {
                case Clip.Walk: return walk;
                case Clip.Run: return run;
                case Clip.Jump: return jump;
                case Clip.Shot: return shot;
                case Clip.Special: return special;
                case Clip.Charging: return charging;
                case Clip.Hurt: return hurt;
                case Clip.Dead: return dead;
                case Clip.Stance: return stance;
                default: return idle;
            }
        }

        public void Configure(SpriteRenderer renderer, Sprite[] idleFrames, Sprite[] walkFrames, Sprite[] runFrames,
            Sprite[] jumpFrames, Sprite[] shotFrames, Sprite[] specialFrames, Sprite[] chargingFrames,
            Sprite[] hurtFrames, Sprite[] deadFrames, Sprite[] stanceFrames)
        {
            spriteRenderer = renderer;
            idle = idleFrames;
            walk = walkFrames;
            run = runFrames;
            jump = jumpFrames;
            shot = shotFrames;
            special = specialFrames;
            charging = chargingFrames;
            hurt = hurtFrames;
            dead = deadFrames;
            stance = stanceFrames;
            _frames = null;
            Play(Clip.Idle, true);
        }
    }
}
