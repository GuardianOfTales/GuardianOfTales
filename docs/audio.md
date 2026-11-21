# Áudio

Implementado com o `GameAudio` (singleton por cena), instalado nas cenas **Vazio** e
**Velho Oeste** pelo menu **Vazio ▸ Áudio ▸ Instalar Sons na Cena** (ou pelo builder do oeste).

## Mapa dos sons (`Assets/Sons`)

| Clip | Onde toca | Volume |
|---|---|---|
| `player/pulo ideia 1` | pulo (e pulo duplo) do jogador | SFX |
| `player/dash ideia 3` | dash | SFX |
| `player/magia ideia 1` | cada disparo de magia | SFX |
| `player/02. Damage Grunt (Male)` | jogador toma dano | SFX |
| `player/05. Death Groan (Male)` | morte do jogador | SFX |
| `player/andar vazio ideia 1` | passos (loop enquanto anda) | 0,35 |
| `player/cura ideia 1` | ao pegar a poção de cura | SFX |
| `arquivista/11. Damage Grunt (Male)` | Arquivista toma dano | SFX |
| `arquivista/ataque cajado boss ideia 2` | melee e laser do Arquivista | SFX |
| `arquivista/ataque livro boss ideia 2` | ataque de livros | SFX |
| `arquivista/subir voo boss ideia 1` | quando ele começa a flutuar | SFX |
| `velho oeste/tiro ideia 4` | tiros normais (cowboys e rajada do chefe) | SFX |
| `velho oeste/tiro rifle` | **tiro carregado** do chefe do velho oeste | SFX |
| `mundo/portal abrindo ideia 2` | cada mini portal de spawn | SFX |
| `mundo/the_mountain-dark-western-618214` | música do mundo Velho Oeste (loop) | 0,45 |
| `mundo/Impending Threat` | música do **Arquivista** (Vazio), ao ativar o chefe (loop) | 0,45 |

## Comportamento

- **Velho Oeste:** *the mountain* toca do início ao fim — inclusive durante a luta com o
  **cowboy chefe** (a arena não troca mais a música).
- **Vazio:** os efeitos do jogador/Arquivista + *Impending Threat* quando o **Arquivista** é
  ativado (ao atravessar o portal); ela para quando ele morre.
- O som de passos usa um `AudioSource` em loop dedicado, ligado/desligado conforme a
  velocidade do jogador.

## Validação em Play Mode

- Música do oeste tocando (`isPlaying = 1`, clip com 32 caracteres = the_mountain) ✔
- Pulo e magia disparam o SFX (source em reprodução após a ação) ✔
- Mini portal toca o som ao aparecer ✔
- **Arquivista** (cena Vazio): ao ativar o chefe a música vira *Impending Threat* (clip com 16 caracteres) ✔
- **Chefe cowboy** (velho oeste): antes e depois de spawnar o chefe a música continua *the mountain* (32) ✔
