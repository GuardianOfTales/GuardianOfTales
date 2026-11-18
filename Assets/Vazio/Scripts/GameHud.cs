using UnityEngine;
using UnityEngine.UI;

namespace Vazio
{
    /// <summary>HUD minimo: vida, mana e a barra grande do Arquivista.</summary>
    [DisallowMultipleComponent]
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private PlayerStats player;
        [SerializeField] private ArchivistBoss boss;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private Image bossFill;
        [SerializeField] private CanvasGroup bossGroup;

        private void Start()
        {
            if (player == null)
            {
                player = FindAnyObjectByType<PlayerStats>();
            }

            if (boss == null)
            {
                // O chefe fica desativado ate o jogador atravessar o portal.
                boss = FindAnyObjectByType<ArchivistBoss>(FindObjectsInactive.Include);
            }
        }

        private void Update()
        {
            if (player != null)
            {
                if (healthFill != null)
                {
                    healthFill.fillAmount = player.HealthNormalized;
                }

                if (manaFill != null)
                {
                    manaFill.fillAmount = player.ManaNormalized;
                }
            }

            if (boss == null)
            {
                boss = FindAnyObjectByType<ArchivistBoss>(FindObjectsInactive.Include);
            }

            bool bossActive = boss != null && boss.gameObject.activeInHierarchy;

            if (bossFill != null && bossActive)
            {
                bossFill.fillAmount = boss.HealthNormalized;
            }

            if (bossGroup != null)
            {
                if (!bossActive)
                {
                    bossGroup.alpha = 0f;
                }
                else
                {
                    bossGroup.alpha = boss.IsAlive
                        ? 1f
                        : Mathf.MoveTowards(bossGroup.alpha, 0f, Time.deltaTime);
                }
            }
        }

        public void Configure(PlayerStats playerStats, ArchivistBoss archivist, Image health, Image mana,
            Image bossBar, CanvasGroup barGroup)
        {
            player = playerStats;
            boss = archivist;
            healthFill = health;
            manaFill = mana;
            bossFill = bossBar;
            bossGroup = barGroup;
        }
    }
}
