local S=require('SaveData');local H=require('ui.HUD');local B=require('battle.BattleScene');local P=require('battle.Player');local W=require('battle.Wave');local E=require('battle.Enemy');local K=require('battle.Skill');local L=require('battle.Loot');local C=require('Config');local R=require('battle.RunSnapshot');local D=require('meta.DailyChallenge');local WC=require('meta.WeeklyChallenge');local I=require('utils.I18n')
local mem={};local originalFile=File;local originalExists=fileSystem.FileExists
File=function(n,m)return {IsOpen=function()return m==FILE_WRITE or mem[n]~=nil end,ReadString=function()return mem[n] or '' end,WriteString=function(_,v)mem[n]=v end,Close=function()end}end
fileSystem.FileExists=function(_,n)return mem[n]~=nil end
local results={};local function test(n,f)local ok,e=pcall(f);results[#results+1]={name=n,pass=ok,error=not ok and tostring(e) or nil}end
S.UnlockAll();S.nickname='Test';S.equippedRunes={};S.equippedTotems={};S.relicLevels={}
local function start()gameState=3;HandleCharSelectTouch(360,674);assert(gameState==1);H.paused=true end
local function resume()gameState=0;UnityRender();HandleScreenTouch(360,275);assert(gameState==1,'Continue failed: '..gameState)end
local saved
start()
test('Chinese and English title',function()I.lang='zh';assert(I.t('game_title')=='Emoji幸存者');I.lang='en';assert(I.t('game_title')=='Emoji Survivor');I.lang='zh'end)
test('Save and Exit returns to title without settlement',function()
 W.waveNum=40;W.bossAlive=true;E.Reset();local boss=E.SpawnBoss(P.x+400,P.y,C.BOSSES[4],3);boss.hp=321
 P.hp=73;P.level=27;P.invTimer=123;B.sessionGold=127
 K.ExportRunState().elephants[1]={x=P.x,y=P.y,life=5,targetEnemy=boss}
 local gold=S.metaGold;H.onSuspend();assert(gameState==0 and not H.paused);assert(S.metaGold==gold,'Settled on suspend');assert(S.HasGameSave())
 saved=mem['gamesave.json'];assert(#saved>1000);local d=S.LoadGame();assert(d.version==2)
end)
test('Continue preserves boss, player, coins and shared target reference',function()
 B.Init('leaf');resume();assert(P.hp==73 and P.level==27 and P.invTimer==123);assert(W.waveNum==40 and W.bossAlive);assert(B.sessionGold==127)
 assert(E.currentBoss and E.currentBoss.hp==321 and E.active[1]==E.currentBoss);assert(K.ExportRunState().elephants[1].targetEnemy==E.currentBoss)
 H.paused=true
end)
test('Snapshot graph handles cycles, numeric table keys and false',function()local a={flag=false};a.self=a;local b={};a[b]=b;local v=R.Unpack(R.Pack(a));assert(v.self==v and v.flag==false);for k,val in pairs(v)do if type(k)=='table'then assert(k==val)end end end)
test('Write failure keeps battle paused and existing save',function()
 local f=File;File=function(n,m)if m==FILE_WRITE then return {IsOpen=function()return false end}end return f(n,m)end
 H.onSuspend();File=f;assert(gameState==1 and H.paused and H.saveMessage);assert(mem['gamesave.json']==saved)
end)
test('Corrupt and incomplete saves hide Continue',function()
 mem['gamesave.json']='{invalid';S._hasGameSave=nil;assert(not S.HasGameSave());mem['gamesave.json']=require('cjson').encode({version=2,player={charId='cat',hp=10},run=R.Pack({modules={},context={},private={}})});S._hasGameSave=nil;assert(not S.HasGameSave());mem['gamesave.json']=saved;S._hasGameSave=nil;assert(S.HasGameSave())
end)
test('Daily rules and difficulty survive suspension',function()
 start();C.currentDiffIdx=3;D.todayConfig=D.GetTodayConfig();D.todayConfig.dateKey='20260917';D.active=true;D.singleSkillId='test';H.onSuspend();assert(not D.active);C.currentDiffIdx=1;resume();assert(C.currentDiffIdx==3 and D.active and D.todayConfig.dateKey=='20260917' and D.singleSkillId=='test');H.paused=true
end)
test('Weekly rules retain original season and day',function()
 start();WC.active=true;WC.todayRule=WC.GetTodayRule();local ruleId=WC.todayRule.id;WC.seasonId='2026W38';WC.seasonDay=4;H.onSuspend();resume();assert(WC.active and WC.todayRule.id==ruleId and WC.runSeasonId=='2026W38' and WC.runDay==4);H.paused=true
end)
test('Gift selection reopens after Continue',function()
 start();B.state=B.STATE_GIFT_AD;B.onGiftPickup();H.onSuspend();resume();assert(B.state==B.STATE_GIFT_AD and require('ui.GiftAd').visible);require('ui.GiftAd').Hide();H.paused=true
end)
test('Resumed run advances all combat subsystems',function()
 start();P.invTimer=999;H.onSuspend();resume();local old=B.heartbeat.frame;for n=1,180 do B.Update(1/60,0,0,720,1280) end;assert(B.heartbeat.frame>old and P.hp>0);assert(not B.GetErrorSummary(),B.GetErrorSummary());H.paused=true
end)
test('Autosave includes newly spawned boss',function()
 start();H.paused=false;P.invTimer=999;E.Reset();W.waveNum=9;W.timer=999;W.bossAlive=false
 HandleUpdate(nil,{TimeStep={GetFloat=function()return .016 end}})
 local d=assert(S.LoadGame());local root=R.Unpack(d.run);assert(root.modules['battle.Wave'].waveNum==10);assert(root.modules['battle.Enemy'].currentBoss,'Boss missing from autosave');H.paused=true
end)
test('Legacy v1 saves still resume',function()
 start();local player={};for k,v in pairs(P)do if type(v)=='number' or type(v)=='string' or type(v)=='boolean'then player[k]=v end end;player.skills=P.skills
 mem['gamesave.json']=require('cjson').encode({version=1,player=player,wave={waveNum=12,bossAlive=true},skill=K.ExportState(),loot={}});S._hasGameSave=nil;assert(S.HasGameSave());resume();assert(W.waveNum==12 and not W.bossAlive);H.paused=true
end)
test('Continue retains skin and does not discover a random map',function()
 start();local cp={};for k,v in pairs(P.charDef)do cp[k]=v end;cp.playerEmoji={idle='🐱',move='🐱'};P.charDef=cp
 local rune=require('battle.RuneEffects').ExportRunState().state;rune.doctorCD=17;rune.firefighterCD=23
 H.onSuspend();local codex=require('meta.Codex');local discover=codex.Discover;local discoveries=0;codex.Discover=function()discoveries=discoveries+1 end
 resume();codex.Discover=discover;assert(discoveries==0,'Spurious map discovery');assert(P.charDef.playerEmoji.idle=='🐱');local rs=require('battle.RuneEffects').ExportRunState().state;assert(rs.doctorCD==17 and rs.firefighterCD==23);H.paused=true
end)
SAVE_TEST_RESULTS=require('cjson').encode(results)
File=originalFile;fileSystem.FileExists=originalExists;gameState=0;S._hasGameSave=nil;D.Reset();WC.Reset();H.paused=false
