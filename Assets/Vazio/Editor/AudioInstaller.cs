using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vazio.EditorTools
{
    /// <summary>
    /// Instala o GameAudio na cena aberta: carrega os clipes de Assets/Sons e
    /// cria (ou atualiza) o objeto "Audio".
    /// </summary>
    public static class AudioInstaller
    {
        private const string Root = "Assets/Sons";
        private const string ObjectName = "Audio";

        [MenuItem("Vazio/Áudio/Instalar Sons na Cena")]
        public static void InstallInOpenScene()
        {
            GameObject existing = GameObject.Find(ObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            GameObject audioGo = Create();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Audio] GameAudio instalado na cena: " + audioGo.name);
        }

        /// <summary>Cria o objeto de audio como filho do transform informado.</summary>
        public static GameObject Create()
        {
            GameObject go = new GameObject(ObjectName);
            GameAudio audio = go.AddComponent<GameAudio>();

            audio.Configure(
                Load("player/pulo ideia 1.mp3"),
                Load("player/dash ideia 3.mp3"),
                Load("player/magia ideia 1.mp3"),
                Load("player/02. Damage Grunt (Male).wav"),
                Load("player/05. Death Groan (Male).wav"),
                Load("player/andar vazio ideia 1.mp3"),
                Load("player/cura ideia 1.mp3"),
                Load("arquivista/11. Damage Grunt (Male).wav"),
                Load("arquivista/ataque cajado boss ideia 2.mp3"),
                Load("arquivista/ataque livro boss ideia 2.mp3"),
                Load("arquivista/subir voo boss ideia 1.mp3"),
                Load("velho oeste/tiro ideia 4.mp3"),
                Load("velho oeste/tiro rifle.mp3"),
                Load("mundo/portal abrindo ideia 2.mp3"),
                Load("mundo/the_mountain-dark-western-618214.mp3"),
                Load("mundo/Impending Threat.mp3"));

            return go;
        }

        private static AudioClip Load(string relative)
        {
            string path = Root + "/" + relative;
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning("[Audio] Clipe nao encontrado: " + path);
            }

            return clip;
        }
    }
}
