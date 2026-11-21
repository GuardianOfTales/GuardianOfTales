# Arenas de combate (Velho Oeste)

Arquitetura modular: **um** componente — `CombatArena` — parametrizado por arena.
A cena do velho oeste tem 4 instâncias.

## Parâmetros (Inspector)

| Campo | Uso |
|---|---|
| `spawnPoints` (List<Transform>) | pontos onde os cowboys nascem |
| `invisibleLeftWall` / `invisibleRightWall` | GameObjects com BoxCollider2D sólido (desativados até o combate) |
| `entryTrigger` (BoxCollider2D) | trigger de ativação (o próprio objeto da arena) |
| `cowboyPrefab` / `cowboyVariants` | prefabs dos cowboys (variantes sorteadas) |
| `bossPrefab` / `bossSpawnPoint` | chefe opcional da arena |
| `enemiesPerWave` / `spawnInterval` / `waves` / `pauseBetweenWaves` / `bossSpawnDelay` | configuração das ondas |
| `cameraFollow` / `lockCamera` | trava da câmera durante o combate |

## Fluxo

1. Jogador entra no trigger (tag `Player` ou `PlayerStats`).
2. Trigger é desativado; paredes laterais ativadas; câmera travada no centro da arena.
3. Corrotina de spawn: 1 cowboy a cada `spawnInterval`, até `enemiesPerWave`.
4. Cowboys e chefe disparam `Died`; a arena decrementa e, ao zerar, segue para a pausa/onda seguinte.
5. No fim: paredes desativadas, câmera liberada, evento `ArenaCleared`.

## Instâncias e posições dos triggers

| Arena | Trigger (x, y) | Cowboys | Ondas | Intervalo | Pausa | Chefe |
|---|---|---|---|---|---|---|
| `Arena_1` | **(25, 1.4)** | 10 | 1 | 2 s | — | — |
| `Arena_2` | **(95, 1.4)** | 15 | 1 | 2 s | — | — |
| `Arena_3` | **(175, 1.4)** | 20 | 1 | 2 s | — | — |
| `Arena_Final` | **(268, 1.4)** | 20 + 20 | 2 | 2 s | 5 s | sim, em (300, chão), 5 s após a onda 2 |

Paredes: `Arena_1` 20/60 · `Arena_2` 78/136 · `Arena_3` 148/216 · `Arena_Final` 240/334.

## Validação (Play Mode)

- `Arena_1`: trigger ativou → paredes ativas, câmera travada em x = 40 (centro), 10 cowboys.
- `Arena_2`: 15 cowboys → ao eliminar todos: `Cleared = true`, paredes desativadas, câmera liberada.
- `Arena_Final`: **40 cowboys** eliminados; chefe instanciado aos **90 s** (40 s onda 1 + 5 s + 40 s onda 2 + 5 s); após a morte do chefe paredes e câmera liberadas.
- Consola: **0 erros**, sem `NullReferenceException`; logs `[CombatArena] Arena limpa` nas três arenas testadas.

## Morte do jogador (reset das arenas)

Cada `CombatArena` assina `PlayerStats.Respawned`. Quando o jogador morre e renasce:

1. o jogador volta para o **ponto inicial** do mundo (`PlayerStats.respawnPoint`);
2. as arenas **destroem os inimigos restantes** e param as corrotinas;
3. paredes laterais desativadas, **trigger reativado**, câmera **destravada**;
4. `Started`/`Cleared` voltam para `false` — o jogador pode refazer as arenas.

## Câmera travada no trigger

Ao ativar, a câmera centraliza **exatamente na posição do trigger** (x e y), e não no centro
médio dos pontos de spawn. Assim o jogador nunca aparece fora do enquadramento no início
do combate.

## Validação

- Entrar em `Arena_1`: `Started = 1`, câmera travada em **(25, 1,4)** — igual ao trigger ✔
- Morrer dentro da arena: após o respawn o jogador está em **x = −6** (início), `Started = 0`,
  paredes desligadas, trigger ativo, câmera destravada, 0 inimigos ✔
- Reentrar na arena: `Started = 1` e câmera travada de novo ✔
- Consola: 0 erros, sem `NullReferenceException` ✔

## Câmera (ajuste)

A trava alinha **apenas o X** da câmera na posição do trigger e **mantém o Y atual** —
usar o Y do trigger (1,4) deixava a visão baixa demais. Verificado: câmera em y = 2,02.

## Poção de cura (`Assets/VelhoOeste/Cura`)

`HealthPotion` + prefab `Cura.prefab` (sprite 32×32, PPU 32, Rigidbody2D com gravidade,
CircleCollider2D). Ao encostar no jogador restaura **25% da vida máxima** e desaparece.

- **Arenas 1–3:** uma poção cai na posição do trigger quando a arena é limpa.
- **Arena_Final:** além da poção do fim, cai uma **a cada 15 s** durante o combate
  (`dropPotionDuringCombat`).
- Ao resetar a arena (morte do jogador), as poções no chão são removidas.

Validação: poção criada em (25,0) e caiu até y = −1,2 (chão); vida 4,0 → **6,5** (+2,5 = 25% de 10)
e a poção foi consumida.

## Layout das arenas (dentro do enquadramento)

A câmera usa **ortho 7,36** (mantido no velho oeste) — a metade visível em 16:9 é ~13,1 u.
Para os mobs nunca nascerem fora da tela:

| Elemento | Posição |
|---|---|
| Paredes invisíveis (2 por arena) | **trigger x ± 11,0** |
| 5 pontos de spawn | **trigger x ± 9,0** (distribuídos) |
| Spawn do chefe (arena final) | trigger x + 2 |

| Arena | Trigger | Paredes | Spawns |
|---|---|---|---|
| Arena_1 | 25 | 14 / 36 | 16 … 34 |
| Arena_2 | 95 | 84 / 106 | 86 … 104 |
| Arena_3 | 175 | 164 / 186 | 166 … 184 |
| Arena_Final | 268 | 257 / 279 | 259 … 277 (chefe em 270) |

Validação: com a câmera travada em x = 25 (meia largura 9,24), **todos** os cowboys spawnados
ficaram dentro do enquadramento (4/4 e 3/3 nas duas medições).

## Aviso de spawn (mini portal)

Antes de cada cowboy nascer, um **mini portal** (`Portal_Spawn.prefab`, escala 0,3, sem colisor)
aparece no ponto de spawn por **0,5 s** (`spawnWarningTime`) e então é destruído e o cowboy nasce
ali. A cadência da onda continua sendo `spawnInterval`.

Validação: mini portal em t = 0,00 s e primeiro cowboy em 0,53 s (delta ≈ 0,5 s); nenhum inimigo
apareceu enquanto o aviso estava na tela. Ao resetar a arena, os avisos pendentes são destruídos.
