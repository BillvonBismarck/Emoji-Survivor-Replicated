using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace EmojiBridge {
public static class EmojiValidation {
 public static string Status="Idle";
 [MenuItem("Emoji Survivor/Validate UI and Capture")]
 public static void Capture(){if(EmojiGame.Instance==null)throw new InvalidOperationException("Enter Play mode first");EmojiGame.Instance.StartCoroutine(Run());}
 static IEnumerator Run(){
 var g=EmojiGame.Instance;var folder=Path.GetFullPath("Migration/Captures/Final");Directory.CreateDirectory(folder);
 g.Evaluate(@"local mem={}; File=function(n,m)return {IsOpen=function()return m==FILE_WRITE or mem[n]~=nil end,ReadString=function()return mem[n] or '' end,WriteString=function(_,s)mem[n]=s end,Close=function()end}end;fileSystem.FileExists=function(_,n)return mem[n]~=nil end;require('SaveData').UnlockAll();require('SaveData').highestWave=100;require('SaveData').metaGold=10000;require('ui.SkillSelect').Hide();require('ui.HUD').paused=false;");
 var scenarios=new[]{
 new[]{"01-title","gameState=0"},new[]{"01b-leaderboard","HandleScreenTouch(170,400)"},new[]{"02-nickname","gameState=4"},
 new[]{"03-cat","gameState=3"},new[]{"04-bean","HandleCharSelectTouch(588,375)"},new[]{"05-monkey","HandleCharSelectTouch(588,375)"},new[]{"06-hand","HandleCharSelectTouch(588,375)"},new[]{"07-leaf","HandleCharSelectTouch(588,375)"},new[]{"08-otto","HandleCharSelectTouch(588,375)"},
 new[]{"09-achievements","gameState=6"},new[]{"10-relics","gameState=7"},new[]{"11-totems","require('ui.TotemShop').Reset();gameState=8"},new[]{"12-runes","require('ui.RuneShop').Reset();gameState=9"},new[]{"13-skins","require('ui.SkinShop').Reset();gameState=10"},new[]{"14-codex","gameState=11"},new[]{"15-weekly","require('meta.WeeklyChallenge').ResetPageState();gameState=12"},
 new[]{"16-battle","gameState=3;HandleCharSelectTouch(588,375);HandleCharSelectTouch(360,674);local p=require('battle.Player');p.invTimer=999;require('ui.HUD').joystickPos='center';ApplyJoystickPosition('center')"},
 new[]{"17-skills","local p=require('battle.Player');require('battle.Loot').Spawn(p.x,p.y,100);for i=1,30 do require('battle.BattleScene').Update(1/60,0,0,720,1280) end"},
 new[]{"18-pause","require('ui.SkillSelect').Hide();require('battle.BattleScene').state=3;require('ui.HUD').paused=true"},
 new[]{"19-boss","require('ui.HUD').paused=false;require('battle.BattleScene').state=1;local p=require('battle.Player');local e=require('battle.Enemy');e.SpawnBoss(p.x+150,p.y-180,require('Config').BOSSES[1],1)"},
 new[]{"20-settlement","local p=require('battle.Player');p.hp=1;p.invTimer=0;p.guardianCharges=0;p.shieldCharges=0;require('SaveData').equippedRunes={};require('battle.RuneEffects').Reset();local e=require('battle.Enemy');e.Spawn(p.x,p.y,'tank',1);e.active[#e.active].atk=100000;e.active[#e.active].hp=100000"}
 };
 var results=new List<object>();
 foreach(var item in scenarios){Status=item[0];string error="";try{g.Evaluate(item[1]);}catch(Exception e){error=e.Message;}
 yield return new WaitForSecondsRealtime(.6f);yield return new WaitForEndOfFrame();
 var texture=ScreenCapture.CaptureScreenshotAsTexture();string path=Path.Combine(folder,item[0]+".png");if(texture!=null){File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);}else error+=" screenshot null";
 if(g.LastError!="")error+=" "+g.LastError;results.Add(new{name=item[0],state=g.GameState,error,path});
 }
 File.WriteAllText(Path.Combine(folder,"manifest.json"),JsonConvert.SerializeObject(results,Formatting.Indented));Status="Complete";Debug.Log("[Emoji UI Validation] Complete: "+folder);
 }
}
}
