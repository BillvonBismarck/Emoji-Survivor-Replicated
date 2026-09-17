using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EmojiBridge {
public static class OptimizationValidation {
 public static string Status="Idle";
 public static void Capture(){EmojiGame.Instance.StartCoroutine(Run());}
 static IEnumerator Run(){
  var g=EmojiGame.Instance;var folder=Path.GetFullPath("Migration/GameOptimization/Captures");Directory.CreateDirectory(folder);
  g.Evaluate("require('utils.I18n').lang='en';require('utils.I18n').untranslated={};require('SaveData').highestWave=140;require('ui.SkillSelect').Hide();require('ui.HUD').paused=false");
  var scenarios=new[]{
   new[]{"title","gameState=0"},new[]{"characters","gameState=3"},
   new[]{"weekly","require('meta.WeeklyChallenge').ResetPageState();gameState=12"},
   new[]{"weekly-hero","local _,day=require('meta.WeeklyChallenge').GetSeasonInfo();HandleScreenTouch(360,186+(day-1)*56+20)"},
   new[]{"pause","HandleCharSelectTouch(360,674);require('battle.Player').invTimer=999;require('ui.HUD').paused=true"},
   new[]{"totems","require('ui.HUD').paused=false;require('SaveData').totems={{typeId='hp_regen',rarity='normal'}};require('ui.TotemShop').Reset();gameState=8"},
   new[]{"totem-detail","require('ui.TotemShop').HandleTouch(50,230)"},
   new[]{"runes","require('ui.RuneShop').Reset();gameState=9"},
   new[]{"rune-detail","require('ui.RuneShop').HandleTouch(650,340)"},new[]{"relics","gameState=7"},
   new[]{"skins","require('ui.SkinShop').Reset();gameState=10"},new[]{"codex","gameState=11"},new[]{"achievements","gameState=6"}
  };
  var results=new List<object>();
  foreach(var s in scenarios){Status=s[0];string error="";try{g.Evaluate(s[1]);}catch(Exception e){error=e.Message;}
   yield return new WaitForSecondsRealtime(.3f);yield return new WaitForEndOfFrame();
   var tex=ScreenCapture.CaptureScreenshotAsTexture();if(tex!=null){File.WriteAllBytes(Path.Combine(folder,s[0]+".png"),tex.EncodeToPNG());UnityEngine.Object.Destroy(tex);}else error+="Screenshot null";
   results.Add(new{name=s[0],state=g.GameState,error=error+g.LastError,callback=g.Lua.LastCallbackError});
  }
  string missing=null;g.Lua.Bind("capture_missing",()=>{missing=g.Lua.Str(1);return 0;});g.Evaluate("capture_missing(cjson.encode(require('utils.I18n').untranslated));gameState=0");
  File.WriteAllText(Path.Combine(folder,"untranslated.json"),missing??"null");File.WriteAllText(Path.Combine(folder,"manifest.json"),JsonConvert.SerializeObject(results,Formatting.Indented));Status="Complete";
 }
}
}
