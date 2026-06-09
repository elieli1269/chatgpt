#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BlockHaven.EditorTools
{
    public static class WindowsBuild
    {
        [MenuItem("BlockHaven/Build Windows EXE")]
        public static void BuildWindowsExe()
        {
            Directory.CreateDirectory("Assets/BlockHaven/Scenes");
            Directory.CreateDirectory("Builds/Windows");
            const string scenePath = "Assets/BlockHaven/Scenes/Main.unity";
            if (!File.Exists(scenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, scenePath);
            }

            BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = "Builds/Windows/BlockHavenCitySimulator.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
        }
    }
}
#endif
