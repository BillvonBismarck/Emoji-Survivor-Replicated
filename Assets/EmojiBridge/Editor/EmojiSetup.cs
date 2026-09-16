using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace EmojiBridge {
public static class EmojiSetup {
 [MenuItem("Emoji Survivor/Create Reproduction Scene")]
 public static void Create(){var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.065f);camera.gameObject.AddComponent<AudioListener>();new GameObject("Directional Light").AddComponent<Light>().type=LightType.Directional;new GameObject("Emoji Survivor • Original Lua").AddComponent<EmojiGame>();EditorSceneManager.SaveScene(scene,"Assets/Scenes/EmojiSurvivor.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/EmojiSurvivor.unity",true)};PlayerSettings.defaultScreenWidth=720;PlayerSettings.defaultScreenHeight=1280;Debug.Log("[Emoji Unity] Scene prepared.");}
}
}
