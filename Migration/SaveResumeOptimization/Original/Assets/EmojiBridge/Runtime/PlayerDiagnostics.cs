using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace EmojiBridge {
// Explicit command-line diagnostic mode; no effect in ordinary play.
public sealed class PlayerDiagnostics : MonoBehaviour {
 string output;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void StartRequested(){var a=Environment.GetCommandLineArgs();for(int i=0;i<a.Length-1;i++)if(a[i]=="-emoji-capture"){var d=new GameObject("Player diagnostics").AddComponent<PlayerDiagnostics>();d.output=a[i+1];d.StartCoroutine(d.Capture());break;}}
 IEnumerator Capture(){Application.runInBackground=true;yield return new WaitForSecondsRealtime(1);if(Array.IndexOf(Environment.GetCommandLineArgs(),"-emoji-battle")>=0&&EmojiGame.Instance!=null)EmojiGame.Instance.Evaluate("gameState=3;HandleCharSelectTouch(360,674)");if(EmojiGame.Instance!=null){var args=Environment.GetCommandLineArgs();
 if(Array.IndexOf(args,"-emoji-english")>=0)EmojiGame.Instance.Evaluate("require('utils.I18n').lang='en'");
 if(Array.IndexOf(args,"-emoji-pause")>=0)EmojiGame.Instance.Evaluate("require('ui.HUD').paused=true");
 if(Array.IndexOf(args,"-emoji-weekly-hero")>=0)EmojiGame.Instance.Evaluate("gameState=12;local _,day=require('meta.WeeklyChallenge').GetSeasonInfo();HandleScreenTouch(360,186+(day-1)*56+20)");
 }yield return new WaitForSecondsRealtime(4);yield return new WaitForEndOfFrame();
 var g=EmojiGame.Instance;var doc=g==null?null:g.GetComponent<UIDocument>();
 var report="state="+(g==null?-1:g.GameState)+" frames="+(g==null?0:g.Frames)+" error="+(g==null?"no host":g.LastError)+" root="+(doc==null?"none":doc.rootVisualElement.layout.ToString());
 if(doc!=null)foreach(var f in typeof(PanelSettings).GetFields(System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic))if(typeof(Shader).IsAssignableFrom(f.FieldType)){var shader=f.GetValue(doc.panelSettings) as Shader;report+="\n"+f.Name+"="+(shader==null?"NULL":shader.name);}
 var image=ScreenCapture.CaptureScreenshotAsTexture();if(image!=null){File.WriteAllBytes(output,image.EncodeToPNG());Destroy(image);}File.WriteAllText(output+".txt",report);Debug.Log("[PlayerCapture] "+report);Application.Quit();}
}
}
