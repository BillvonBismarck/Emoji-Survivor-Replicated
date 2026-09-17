local C=require('Config') local E=require('battle.Enemy') local W=require('battle.Wave') local P=require('battle.Player') local B=require('battle.BattleScene') local EB=require('battle.EnemyBullet')
local S=require('SaveData')
local memory={};File=function(n,m)return {IsOpen=function()return m==FILE_WRITE or memory[n]~=nil end,ReadString=function()return memory[n] or '' end,WriteString=function(_,v)memory[n]=v end,Close=function()end}end
fileSystem.FileExists=function(_,n)return memory[n]~=nil end
S.equippedTotems={} S.equippedRunes={} S.relicLevels={}
local report={cases={},waves={}}
local function check(name,fn)local ok,err=pcall(fn);report.cases[#report.cases+1]={name=name,pass=ok,error=not ok and tostring(err) or nil}end
for _,w in ipairs({30,39,40,50,60,100,140})do
 B.Init('leaf');P.level=w+1;P.hpBonus=.5;P.relicHpBonus=0;P.totemHpFlat=0;P.runeAllStatBonus=0;P.RecalcStats()
 W.waveNum=w-1;W.timer=C.WAVE.interval;W.Update(0,P.x,P.y);E.Reset()
 local normal=E.Spawn(P.x,P.y,'normal',W.waveCoeff);local elite=E.Spawn(P.x,P.y,'elite',W.waveCoeff)
 local heavy=math.floor(elite.atk*1.8)
 report.waves[#report.waves+1]={wave=w,hp=P.maxHp,contact=normal.atk,eliteHeavy=heavy,contactHits=math.ceil(P.maxHp/normal.atk),eliteHits=math.ceil(P.maxHp/heavy)}
 check('W'..w..' reference contact 6-7 hits',function()local n=math.ceil(P.maxHp/normal.atk);assert(n>=6 and n<=7,'hits='..n)end)
 check('W'..w..' reference elite at least 3 hits',function()assert(math.ceil(P.maxHp/heavy)>=3,'damage='..heavy..' hp='..P.maxHp)end)
end
E.Reset();for i=1,200 do E.Spawn(100,100,'normal',1)end
check('Population cap',function()assert(#E.active<=C.WAVE.maxEnemiesAlive)end)
EB.Reset();EB.Spawn(P.x,P.y,P.x,P.y,10,0);EB.Spawn(P.x,P.y,P.x,P.y,10,0)
local d=EB.Update(0,P.x,P.y,P.radius)
check('Same-frame projectiles respect one-hit window',function()assert(d<=10,'damage='..d)end)
local tc=require('meta.TotemSystem').TYPE_CONFIG.hp_regen
check('Regen totem describes actual per-second healing',function()assert(tc.name=='生命再生' and tc.unit=='%/s',tc.name..' '..tc.desc)end)
gameState=0
REPAIR_BASELINE=report
