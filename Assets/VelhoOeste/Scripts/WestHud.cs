using UnityEngine;
using UnityEngine.UI;

namespace Vazio
{
    /// <summary>HUD do velho oeste: vida/mana do jogador e vida do cowboy chefe.</summary>
    [DisallowMultipleComponent]
    public sealed class WestHud : MonoBehaviour
    {
        [SerializeField] private PlayerStats player;
        [SerializeField] private CowboyBoss boss;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image manaFill;
        [SerializeField] private Image bossFill;
        [SerializeField] private CanvasGroup bossGroup;
        [SerializeField, Min(1f)] private float bossShowDistance = 26f;

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerStats>();
            if (boss == null) boss = FindAnyObjectByType<CowboyBoss>();
        }

        private void Update()
        {
            if (player == null) player = FindAnyObjectByType<PlayerStats>();

            if (player != null)
            {
                if (healthFill != null) healthFill.fillAmount = player.HealthNormalized;
                if (manaFill != null) manaFill.fillAmount = player.ManaNormalized;
            }

            if (boss == null) boss = FindAnyObjectByType<CowboyBoss>();

            bool show = boss != null && boss.IsAlive && player != null
                && Mathf.Abs(boss.transform.position.x - player.transform.position.x) <= bossShowDistance;

            if (bossFill != null && boss != null)
            {
                bossFill.fillAmount = boss.MaxHealth > 0f ? Mathf.Clamp01(boss.Health / boss.MaxHealth) : 0f;
            }

            if (bossGroup != null)
            {
                bossGroup.alpha = Mathf.MoveTowards(bossGroup.alpha, show ? 1f : 0f, Time.deltaTime * 3f);
            }
        }

        public void Configure(PlayerStats stats, CowboyBoss cowboyBoss, Image health, Image mana, Image bossBar, CanvasGroup bossCanvas)
        {
            player = stats;
            boss = cowboyBoss;
            healthFill = health;
            manaFill = mana;
            bossFill = bossBar;
            bossGroup = bossCanvas;
        }
    }
}
