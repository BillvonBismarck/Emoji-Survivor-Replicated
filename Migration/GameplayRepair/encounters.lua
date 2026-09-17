local B=require('battle.BattleScene') local P=require('battle.Player') local W=require('battle.Wave') local E=require('battle.Enemy') local EB=require('battle.EnemyBullet') local C=require('Config') local T=require('battle.CombatTelemetry')
local H=require('ui.HUD') local SS=require('ui.SkillSelect')
local output={wave=TEST_WAVE,runs={}}
for seed=1,6 do for _,moving in ipairs({false,true})do
 math.randomseed(seed);B.Init('leaf');H.paused=false;SS.Hide();P.level=TEST_WAVE+1;P.hpBonus=.5;P.relicHpBonus=0;P.totemHpFlat=0;P.runeAllStatBonus=0;P.RecalcStats();P.hp=P.maxHp;P.invTimer=0
 W.waveNum=TEST_WAVE-1;W.timer=C.WAVE.interval;W.Update(0,P.x,P.y);W.timer=-1000;W.spawnTimer=-1000;E.Reset();EB.Reset();W.bossAlive=false;W.gameOver=false;gameState=1
 for i=1,24 do local a=i*math.pi/12;local kind=i%4==0 and 'elite' or (i%3==0 and 'ranger' or 'normal');E.Spawn(P.x+math.cos(a)*180,P.y+math.sin(a)*180,kind,W.waveCoeff)end
 local seconds=0
 for frame=1,1800 do
  if B.state==B.STATE_GAME_OVER then break end
  if B.state==B.STATE_SKILL_SELECT then B.SelectSkill(1);SS.Hide()end
  if B.state==B.STATE_ULTIMATE_SELECT then B.SelectUltimate('heal_inv')end
  if B.state==B.STATE_GIFT_AD then B.ResolveGift(false)end
  local x=moving and math.cos(frame/120) or 0;local y=moving and math.sin(frame/120) or 0
  B.Update(1/60,x,y,720,1280);seconds=frame/60
 end
 output.runs[#output.runs+1]={seed=seed,moving=moving,seconds=seconds,state=B.state,hp=P.hp,maxHp=P.maxHp,death=T.lastDeath,error=B.GetErrorSummary()}
end end
ENCOUNTER_RESULT=output;gameState=0
