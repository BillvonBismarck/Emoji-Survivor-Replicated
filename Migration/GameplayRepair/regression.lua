-- Run in the actual Unity Lua VM after baseline.lua establishes isolated save IO.
local B=require('battle.BattleScene') local P=require('battle.Player') local W=require('battle.Wave') local E=require('battle.Enemy') local EB=require('battle.EnemyBullet') local C=require('Config')
local T=require('battle.CombatTelemetry') local H=require('ui.HUD') local SS=require('ui.SkillSelect')
local results={cases={},encounters={}}
local function test(name,fn)local ok,err=pcall(fn);results.cases[#results.cases+1]={name=name,pass=ok,error=not ok and tostring(err) or nil}end
local function ready(w)
 B.Init('leaf');H.paused=false;SS.Hide();P.level=w+1;P.hpBonus=.5;P.relicHpBonus=0;P.totemHpFlat=0;P.runeAllStatBonus=0;P.RecalcStats();P.hp=P.maxHp;P.invTimer=0
 W.waveNum=w-1;W.timer=C.WAVE.interval;W.Update(0,P.x,P.y);W.timer=-1000;W.spawnTimer=-1000;E.Reset();EB.Reset();gameState=1
end
test('HP progression cannot scale enemy attack',function()ready(140);local a=E.Spawn(100,100,'normal',W.waveCoeff).atk;P.maxHp=P.maxHp*10;local b=E.Spawn(100,100,'normal',W.waveCoeff).atk;assert(a==b)end)
test('Quantity compression preserves HP pressure with bounded attack',function()ready(140);W.quantityFactor=1;local a=E.Spawn(100,100,'normal',W.waveCoeff);W.quantityFactor=8;local b=E.Spawn(100,100,'normal',W.waveCoeff);assert(b.hp>=a.hp*7.9 and b.atk<=a.atk*1.15);assert(b.expDrop==a.expDrop*8)end)
test('Boss reserve and hard cap across 1000 spawn requests',function()ready(140);for i=1,1000 do E.Spawn(100,100,'normal',W.waveCoeff)end;assert(#E.active==59);assert(E.SpawnBoss(100,100,C.BOSSES[1],W.waveCoeff));assert(#E.active==60);assert(E.SpawnBoss(100,100,C.BOSSES[1],W.waveCoeff)==nil)end)
test('Projectile, area overlap and telemetry candidates',function()ready(40);EB.Spawn(P.x,P.y,P.x,P.y,20,0);EB.Spawn(P.x,P.y,P.x,P.y,30,0);EB.AddArea({type='circle',x=P.x,y=P.y,radius=100,damage=12,timer=2,tickTimer=0,tickInterval=1});local d=EB.Update(0,P.x,P.y,P.radius);assert(d==16);assert(EB.lastHit.hitCount==3 and EB.lastHit.summedCandidateDamage==39);P.TakeDamage(d,EB.lastHit);assert(T.recent[#T.recent].hitCount==3)end)
test('Death includes source, damage stages and lifesaving resources',function()ready(140);P.hp=1;P.invTimer=0;T.BeginFrame();assert(P.TakeDamage(10,{source='regression',baseDamage=8,compressedDamage=10,compressionMultiplier=1.25,hitCount=2}));local r=T.lastDeath;assert(r.wave==140 and r.level==141 and r.hpBefore==1 and r.hpAfter==0 and r.sameFrameHitCount==2 and r.afterReductionDamage==10 and r.runeState.workerLayers~=nil)end)
test('Invincibility and shield are recorded without damage',function()ready(40);T.BeginFrame();P.invTimer=1;local hp=P.hp;P.TakeDamage(999);assert(P.hp==hp and T.recent[#T.recent].outcome=='invincible');P.invTimer=0;P.shieldCharges=1;P.TakeDamage(999);assert(P.hp==hp and T.recent[#T.recent].outcome=='shield')end)
test('Regen totem heals stated maxHP percent per second',function()ready(40);local t=require('meta.TotemSystem');local bonus=t.CalcEquippedBonuses({{typeId='hp_regen',rarity='normal'}});P.totemHpRegenBonus=bonus.hpRegenBonus;P.regenRate=0;P.hp=10;local hp=P.maxHp;P.Update(1,0,0);assert(math.abs(P.hp-10-hp*.1)<.001)end)
REPAIR_REGRESSION=results;gameState=0
