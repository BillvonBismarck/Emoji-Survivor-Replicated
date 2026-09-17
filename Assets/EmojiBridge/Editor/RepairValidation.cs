using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
namespace EmojiBridge {
public static class RepairValidation {
 public static string Status="Idle";
 public static void Run(){EmojiGame.Instance.StartCoroutine(Measure());}
 static IEnumerator Measure(){var g=EmojiGame.Instance;
 g.Evaluate("local mem={};File=function(n,m)return {IsOpen=function()return m==FILE_WRITE or mem[n]~=nil end,ReadString=function()return mem[n] or '' end,WriteString=function(_,s)mem[n]=s end,Close=function()end}end;fileSystem.FileExists=function(_,n)return mem[n]~=nil end;local B=require('battle.BattleScene');local P=require('battle.Player');local W=require('battle.Wave');require('SaveData').equippedTotems={};require('SaveData').equippedRunes={};B.Init('leaf');P.level=141;P.hpBonus=.5;P.RecalcStats();P.invTimer=99999;P.ApplySkill('multi_shot',5);P.ApplySkill('atk_drone',5);W.waveNum=139;W.timer=require('Config').WAVE.interval;require('ui.HUD').paused=false;require('ui.SkillSelect').Hide();gameState=1");
 var s=(VectorSurface)typeof(EmojiGame).GetField("surface",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(g);
 Status="Sampling";var draw=new List<double>();var frame=new List<double>();
 for(int i=0;i<300;i++){yield return null;if(i>=60){draw.Add(s.LastRenderMilliseconds);frame.Add(Time.unscaledDeltaTime*1000);}}
 g.Evaluate("PERF_ENEMIES=#require('battle.Enemy').active;PERF_BULLETS=#require('battle.EnemyBullet').active;PERF_PLAYER_BULLETS=#require('battle.Projectile').active");
 draw.Sort();frame.Sort();var report=new {samples=draw.Count,renderMedianMs=draw[draw.Count/2],renderP95Ms=draw[(int)(draw.Count*.95)],frameMedianMs=frame[frame.Count/2],frameP95Ms=frame[(int)(frame.Count*.95)],enemies=g.Lua.GetNumber("PERF_ENEMIES"),enemyBullets=g.Lua.GetNumber("PERF_BULLETS"),playerBullets=g.Lua.GetNumber("PERF_PLAYER_BULLETS"),missing=s.MissingGlyphCount,error=g.LastError};
 Directory.CreateDirectory("Migration/GameplayRepair");File.WriteAllText("Migration/GameplayRepair/performance-runtime.json",Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented));
 yield return new WaitForEndOfFrame();var t=ScreenCapture.CaptureScreenshotAsTexture();if(t!=null){File.WriteAllBytes("Migration/GameplayRepair/stress-140.png",t.EncodeToPNG());UnityEngine.Object.Destroy(t);}Status="Complete";Debug.Log("[Repair Performance] "+Newtonsoft.Json.JsonConvert.SerializeObject(report));
 }
}
}
