using UnityEngine;

namespace Vazio
{
    /// <summary>
    /// Central de audio do jogo: efeitos do jogador, do Arquivista, tiros do
    /// Velho Oeste, som dos mini portais e musicas (mundo x chefe).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [Header("Jogador")]
        [SerializeField] private AudioClip playerJump;
        [SerializeField] private AudioClip playerDash;
        [SerializeField] private AudioClip playerMagic;
        [SerializeField] private AudioClip playerHurt;
        [SerializeField] private AudioClip playerDeath;
        [SerializeField] private AudioClip playerWalk;
        [SerializeField] private AudioClip playerHeal;

        [Header("Arquivista")]
        [SerializeField] private AudioClip bossHurt;
        [SerializeField] private AudioClip bossStaff;
        [SerializeField] private AudioClip bossBooks;
        [SerializeField] private AudioClip bossFlight;

        [Header("Velho Oeste")]
        [SerializeField] private AudioClip shot;
        [SerializeField] private AudioClip rifleShot;

        [Header("Mundo")]
        [SerializeField] private AudioClip portalOpen;
        [SerializeField] private AudioClip westMusic;
        [SerializeField] private AudioClip bossMusic;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float loopVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.45f;

        private AudioSource _sfx;
        private AudioSource _loop;
        private AudioSource _music;
        [SerializeField] private bool autoWestMusic;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _sfx = CreateSource("SFX", false);
            _loop = CreateSource("Loop", true);
            _music = CreateSource("Music", true);
        }

        private void Start()
        {
            if (autoWestMusic)
            {
                PlayWestMusic();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            GameObject go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip == null || _sfx == null)
            {
                return;
            }

            _sfx.PlayOneShot(clip, volume);
        }

        // ------------------------------------------------------------- jogador
        public void PlayerJump() => Play(playerJump, sfxVolume);
        public void PlayerDash() => Play(playerDash, sfxVolume);
        public void PlayerMagic() => Play(playerMagic, sfxVolume);
        public void PlayerHurt() => Play(playerHurt, sfxVolume);
        public void PlayerDeath() => Play(playerDeath, sfxVolume);
        public void PlayerHeal() => Play(playerHeal, sfxVolume);

        public void SetWalking(bool walking)
        {
            if (_loop == null || playerWalk == null)
            {
                return;
            }

            if (walking)
            {
                if (!_loop.isPlaying || _loop.clip != playerWalk)
                {
                    _loop.clip = playerWalk;
                    _loop.volume = loopVolume;
                    _loop.Play();
                }
            }
            else if (_loop.isPlaying)
            {
                _loop.Stop();
            }
        }

        // ----------------------------------------------------------- arquivista
        public void BossHurt() => Play(bossHurt, sfxVolume);
        public void BossStaff() => Play(bossStaff, sfxVolume);
        public void BossBooks() => Play(bossBooks, sfxVolume);
        public void BossFlight() => Play(bossFlight, sfxVolume);

        // ---------------------------------------------------------- velho oeste
        public void Shot() => Play(shot, sfxVolume);
        public void RifleShot() => Play(rifleShot, sfxVolume);

        // ---------------------------------------------------------------- mundo
        public void PortalOpen() => Play(portalOpen, sfxVolume);

        public void PlayWestMusic() => PlayMusic(westMusic);
        public void PlayBossMusic() => PlayMusic(bossMusic);

        public void PlayMusic(AudioClip clip)
        {
            if (_music == null)
            {
                return;
            }

            if (clip == null)
            {
                _music.Stop();
                return;
            }

            if (_music.isPlaying && _music.clip == clip)
            {
                return;
            }

            _music.clip = clip;
            _music.volume = musicVolume;
            _music.loop = true;
            _music.Play();
        }

        public void SetAutoWestMusic(bool enabled)
        {
            autoWestMusic = enabled;
        }

        /// <summary>Preenche os clipes (usado pelo instalador de cena).</summary>
        public void Configure(AudioClip jump, AudioClip dash, AudioClip magic, AudioClip hurt, AudioClip death,
            AudioClip walk, AudioClip heal, AudioClip bossHurtClip, AudioClip staff, AudioClip books,
            AudioClip flight, AudioClip shotClip, AudioClip rifle, AudioClip portal, AudioClip west, AudioClip bossTheme)
        {
            playerJump = jump;
            playerDash = dash;
            playerMagic = magic;
            playerHurt = hurt;
            playerDeath = death;
            playerWalk = walk;
            playerHeal = heal;
            bossHurt = bossHurtClip;
            bossStaff = staff;
            bossBooks = books;
            bossFlight = flight;
            shot = shotClip;
            rifleShot = rifle;
            portalOpen = portal;
            westMusic = west;
            bossMusic = bossTheme;
        }
    }
}
