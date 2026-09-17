using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace EmojiBridge {
public sealed class EmojiGame : MonoBehaviour {
 public static EmojiGame Instance {get;private set;}
 public string LastError="";public int Frames;public int GameState;
 public LuaRuntime Lua {get;private set;}
 VectorSurface surface;UIDocument doc;string saveRoot;string pendingText="";Vector2 joystickOrigin;bool joystickActive;
 readonly Dictionary<int,AudioSource> sources=new Dictionary<int,AudioSource>();
 readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
 static readonly Dictionary<string,Key> keys=new Dictionary<string,Key>{{"A",Key.A},{"D",Key.D},{"W",Key.W},{"S",Key.S},{"UP",Key.UpArrow},{"DOWN",Key.DownArrow},{"LEFT",Key.LeftArrow},{"RIGHT",Key.RightArrow},{"TAB",Key.Tab},{"E",Key.E},{"G",Key.G},{"K",Key.K},{"O",Key.O},{"P",Key.P},{"Q",Key.Q},{"ESCAPE",Key.Escape},{"BACKSPACE",Key.Backspace},{"RETURN",Key.Enter},{"KP_ENTER",Key.NumpadEnter}};
 void Start(){Instance=this;try{
 Application.targetFrameRate=60;Application.runInBackground=true;saveRoot=Array.IndexOf(Environment.GetCommandLineArgs(),"-emoji-capture")>=0?Path.Combine(Application.temporaryCachePath,"EmojiDiagnostics"):Path.Combine(Application.persistentDataPath,"EmojiSurvivorLua");Directory.CreateDirectory(saveRoot);
 doc=gameObject.AddComponent<UIDocument>();var template=Resources.Load<PanelSettings>("EmojiPanelSettings");if(template==null)throw new InvalidOperationException("Missing EmojiPanelSettings render resources");var settings=Instantiate(template);settings.scaleMode=PanelScaleMode.ConstantPixelSize;settings.themeStyleSheet=Resources.Load<ThemeStyleSheet>("EmojiTheme");doc.panelSettings=settings;
 var root=doc.rootVisualElement;root.style.backgroundColor=new Color(.025f,.03f,.065f);root.style.alignItems=Align.Center;root.style.justifyContent=Justify.Center;
 surface=new VectorSurface();surface.RenderFailed=Fail;surface.EmojiSequences=JsonConvert.DeserializeObject<Dictionary<string,string>>(Resources.Load<UnityEngine.TextAsset>("emoji-sequences").text);surface.MainFont=FontAsset.CreateFontAsset(Resources.Load<Font>("Fonts/MiSans-Regular"));surface.PixelFont=FontAsset.CreateFontAsset(Resources.Load<Font>("Fonts/zpix"));
 surface.EmojiAtlas=Resources.Load<Texture2D>("Fonts/EmojiAtlas0");if(surface.EmojiAtlas==null)throw new InvalidOperationException("Missing emoji atlas");foreach(var item in JsonConvert.DeserializeObject<Dictionary<string,int[]>>(Resources.Load<UnityEngine.TextAsset>("emoji-glyphs").text)){var r=item.Value;surface.EmojiGlyphs[uint.Parse(item.Key)]=new Rect(r[0],r[1],r[2],r[3]);}
 surface.MainFont.atlasPopulationMode=AtlasPopulationMode.Dynamic;surface.PixelFont.atlasPopulationMode=AtlasPopulationMode.Dynamic;surface.MainFont.fallbackFontAssetTable=new List<FontAsset>();surface.PixelFont.fallbackFontAssetTable=new List<FontAsset>{surface.MainFont};root.Add(surface);
 var warm=Resources.Load<UnityEngine.TextAsset>("font-prewarm");if(warm!=null)surface.Prewarm(warm.text);
 Lua=new LuaRuntime();Lua.Set("UNITY_ROOT",Path.Combine(Application.streamingAssetsPath,"Emoji").Replace('\\','/'));
 Lua.Bind("unity_log",()=>{string s=Lua.Str(1);if(s.Contains("[ERROR]")||s.Contains("[CRITICAL]")||s.StartsWith("ERROR")){LastError=s;Debug.LogError(s);}else Debug.Log(s);return 0;});
 Lua.Bind("unity_key",()=>Lua.Push(keys.TryGetValue(Lua.Str(1),out var k)&&Keyboard.current!=null&&((int)Lua.Num(2)==1?Keyboard.current[k].wasPressedThisFrame:Keyboard.current[k].isPressed)));
 Lua.Bind("unity_json_quote",()=>Lua.Push(JsonConvert.SerializeObject(Lua.Str(1))));
 Lua.Bind("unity_json_lua",()=>Lua.Push(JsonToLua(JToken.Parse(Lua.Str(1)))));
 Lua.Bind("unity_exists",()=>Lua.Push(File.Exists(SavePath(Lua.Str(1)))));
 Lua.Bind("unity_read",()=>Lua.Push(File.ReadAllText(SavePath(Lua.Str(1)))));
 Lua.Bind("unity_write",()=>{var p=SavePath(Lua.Str(1));File.WriteAllText(p+".tmp",Lua.Str(2));File.Copy(p+".tmp",p,true);File.Delete(p+".tmp");return 0;});
 Lua.Bind("unity_audio",()=>{Audio((int)Lua.Num(1),Lua.Str(2),(float)Lua.Num(3),Lua.Num(4)==1);return 0;});
 Lua.Bind("unity_draw",()=>surface.Op(Lua));Lua.Bind("unity_measure",()=>Lua.Push(surface.Measure(Lua.Str(1))));Lua.Bind("unity_text",()=>{float x=(float)Lua.Num(1);string s=Lua.Str(3);surface.Text(x,(float)Lua.Num(2),s,(float)Lua.Num(4));return Lua.Push(x+surface.Measure(s));});
 Lua.Execute(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"Emoji/unity_host.lua")),"unity_host");Lua.Call("UnityStart");surface.Render=()=>Lua.Call("UnityRender");
 if(Keyboard.current!=null)Keyboard.current.onTextInput+=OnText;
 Debug.Log("[Emoji Unity] Original Lua entry running. Saves: "+saveRoot);
 }catch(Exception e){Fail(e);}}
 void OnText(char c){if(!char.IsControl(c))pendingText+=c;}
 string SavePath(string name){if(Path.GetFileName(name)!=name)throw new IOException("Invalid save name");return Path.Combine(saveRoot,name);}
 static string QuoteLua(string s){var b=System.Text.Encoding.UTF8.GetBytes(s);return "\""+string.Concat(b.Select(c=>"\\"+c.ToString("D3")))+"\"";}
 static string JsonToLua(JToken t){if(t is JObject o)return "{"+string.Join(",",o.Properties().Select(p=>"["+QuoteLua(p.Name)+"]="+JsonToLua(p.Value)))+"}";if(t is JArray a)return "{"+string.Join(",",a.Select(JsonToLua))+"}";if(t.Type==JTokenType.String)return QuoteLua((string)t);if(t.Type==JTokenType.Null)return "nil";return t.ToString(Formatting.None);}
 void Update(){if(Lua!=null&&!string.IsNullOrEmpty(Lua.LastCallbackError)){if(LastError=="")Fail(new Exception(Lua.LastCallbackError));return;}if(Lua==null||surface==null||LastError!="")return;try{
 if(Screen.width<=0||Screen.height<=0)return;float scale=Mathf.Min(Screen.width/720f,Screen.height/1280f);surface.style.scale=new Scale(new Vector3(scale,scale,1));
 var mouse=Mouse.current;Vector2 pos=mouse!=null?mouse.position.ReadValue():Vector2.zero;pos=new Vector2((pos.x-(Screen.width-720*scale)/2)/scale,(Screen.height-pos.y-(Screen.height-1280*scale)/2)/scale);
 bool down=mouse!=null&&mouse.leftButton.isPressed,press=mouse!=null&&mouse.leftButton.wasPressedThisFrame,release=mouse!=null&&mouse.leftButton.wasReleasedThisFrame;
 if(Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.isPressed||Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasReleasedThisFrame){var touch=Touchscreen.current.primaryTouch;var tp=touch.position.ReadValue();pos=new Vector2((tp.x-(Screen.width-720*scale)/2)/scale,(Screen.height-tp.y-(Screen.height-1280*scale)/2)/scale);down=touch.press.isPressed;press=touch.press.wasPressedThisFrame;release=touch.press.wasReleasedThisFrame;}
 if(press&&pos.y>820&&GameState==1){joystickOrigin=pos;joystickActive=true;}if(!down)joystickActive=false;Vector2 joy=joystickActive?Vector2.ClampMagnitude((pos-joystickOrigin)/70,1):Vector2.zero;
 Lua.Set("UNITY_DT",Mathf.Min(Time.unscaledDeltaTime,.05f));Lua.Set("UNITY_MX",pos.x);Lua.Set("UNITY_MY",pos.y);Lua.Set("UNITY_PRESS",press?1:0);Lua.Set("UNITY_DOWN",down?1:0);Lua.Set("UNITY_RELEASE",release?1:0);Lua.Set("UNITY_WHEEL",mouse!=null?mouse.scroll.ReadValue().y/120:0);Lua.Set("UNITY_JX",joy.x);Lua.Set("UNITY_JY",joy.y);Lua.Set("UNITY_TEXT",pendingText);pendingText="";
 Lua.Call("UnityTick");GameState=(int)Lua.GetNumber("gameState");Frames++;surface.MarkDirtyRepaint();
 }catch(Exception e){Fail(e);}}
 void Audio(int id,string path,float gain,bool loop){if(!sources.TryGetValue(id,out var a)){a=gameObject.AddComponent<AudioSource>();a.playOnAwake=false;sources.Add(id,a);}if(path==""){a.Stop();return;}string key=path.Replace("assets/","");key=key.Substring(0,key.LastIndexOf('.'));if(!clips.TryGetValue(key,out var clip)){clip=Resources.Load<AudioClip>(key);clips[key]=clip;}if(clip==null){Debug.LogWarning("Audio missing: "+key);return;}a.clip=clip;a.volume=gain;a.loop=loop;a.Play();}
 void OnGUI(){if(string.IsNullOrEmpty(LastError))return;GUI.color=Color.white;GUI.Box(new Rect(20,20,Mathf.Max(280,Screen.width-40),180),"游戏运行遇到错误，请查看 Player.log\n"+LastError);}
 void Fail(Exception e){LastError=e.ToString();Debug.LogError("[Emoji Unity] "+e);}
 public void Evaluate(string code){Lua.Execute(code,"validation");LastError="";surface.MarkDirtyRepaint();}
 void OnDestroy(){if(Keyboard.current!=null)Keyboard.current.onTextInput-=OnText;surface?.RemoveFromHierarchy();Lua?.Dispose();Lua=null;if(Instance==this)Instance=null;}
}
}
