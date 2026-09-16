-- Runs inside the Unity Lua VM. All save IO is replaced with an in-memory
-- filesystem before synthetic progression is applied. Restart Play afterwards.
local memory={}
File=function(name,mode)
 return {IsOpen=function()return mode==FILE_WRITE or memory[name]~=nil end,
 ReadString=function()return memory[name] or '' end,
 WriteString=function(_,s)memory[name]=s end,Close=function()end}
end
fileSystem.FileExists=function(_,name)return memory[name]~=nil end
local C=require('Config') local P=require('battle.Player') local B=require('battle.BattleScene')
local E=require('battle.Enemy') local W=require('battle.Wave') local S=require('battle.Skill')
local Save=require('SaveData') local Loot=require('battle.Loot') local H=require('ui.HUD')
local SS=require('ui.SkillSelect') local R=require('meta.RuneSystem') local T=require('meta.TotemSystem')
local results={passed=0,failed=0,cases={},counts={characters=#C.CHARACTERS,skills=#C.SKILLS,bosses=#C.BOSSES,runes=R.GetTotalCount(),skins=#require('meta.SkinSystem').SKINS}}
local function test(name,fn)
 local ok,err=pcall(fn) results.cases[#results.cases+1]={name=name,pass=ok,error=not ok and tostring(err) or nil}
 if ok then results.passed=results.passed+1 else results.failed=results.failed+1 end
end
local function ready(id)
 H.paused=false SS.Hide() B.Init(id or 'cat') B.ResetErrors() P.invTimer=9999
 gameState=1
end
local function tick(n)
 for i=1,n do
  if B.state==B.STATE_SKILL_SELECT then B.SelectSkill(1) SS.Hide() end
  if B.state==B.STATE_ULTIMATE_SELECT then B.SelectUltimate('heal_inv') end
  if B.state==B.STATE_GIFT_AD then B.ResolveGift(false) end
  B.Update(1/60,0,0,720,1280)
 end
 assert(B.GetErrorSummary()==nil,B.GetErrorSummary())
 assert(P.x==P.x and P.hp==P.hp,'nonfinite state')
end
test('JSON UTF8 nested objects and arrays',function()
 local v={name='猫😺"\\\n',gold=123,flags={true,false},nested={a='中文'}}
 local w=cjson.decode(cjson.encode(v)) assert(w.name==v.name and w.flags[2]==false and w.nested.a=='中文')
 assert(require('cjson')==cjson)
end)
test('Profile save reload preserves progression',function()
 Save.metaGold=1234 Save.nickname='Unity验证' Save.SaveProfile() Save.metaGold=0 Save.LoadProfile()
 assert(Save.metaGold==1234 and Save.nickname=='Unity验证')
end)
test('Settings persist language controls quality',function()
 local I=require('utils.I18n') H.joystickPos='right' H.eightDirMode=true I.lang='en' H.SaveSettings()
 H.joystickPos='left' H.eightDirMode=false I.lang='zh' H.LoadSettings()
 assert(H.joystickPos=='right' and H.eightDirMode and I.lang=='en') I.lang='zh' H.joystickPos='center'
end)
for _,ch in ipairs(C.CHARACTERS) do
 test('Character movement and combat '..ch.id,function()
  ready(ch.id) local x=P.x B.Update(.1,1,0,720,1280) assert(P.x>x,'movement')
  tick(180) assert(P.maxHp>0 and P.atk>0)
 end)
end
for _,def in ipairs(C.SKILLS) do
 test('Skill runtime '..def.id,function()
  ready(def.charId or 'cat') P.ApplySkill(def.id,def.maxLevel or 1)
  assert(P.skills[def.id]>0)
  for i=1,4 do E.SpawnAroundPlayer(P.x,P.y,'normal',1) end
  tick(90)
 end)
end
for _,boss in ipairs(C.BOSSES) do
 test('Boss runtime '..boss.id,function()
  ready('cat') E.SpawnBossAroundPlayer(P.x,P.y,boss,1) tick(360)
 end)
end
test('Level-up choices apply and resume',function()
 ready('cat') Loot.Spawn(P.x,P.y,100) for i=1,30 do B.Update(1/60,0,0,720,1280)end
 assert(B.state==B.STATE_SKILL_SELECT and #B.skillChoices>0,'skill selection did not open')
 local id=B.skillChoices[1].def.id B.SelectSkill(1) SS.Hide()
 assert(P.skills[id]>=1 and B.state==B.STATE_PLAYING)
end)
test('Pause freezes battle state',function()
 ready('cat') B.state=B.STATE_PAUSED local x=P.x local t=W.totalTime B.Update(1,1,0,720,1280)
 assert(P.x==x and W.totalTime==t) B.state=B.STATE_PLAYING B.Update(.1,1,0,720,1280) assert(P.x>x)
end)
test('Save and resume battle restores state',function()
 ready('cat') P.ApplySkill('multi_shot',2) P.x=P.x+123 W.waveNum=8
 Save.SaveGame(P,W,S,Loot) local data=Save.LoadGame() assert(data,'missing game save')
 local expectedX=P.x ready('bean') Save.ApplyToGame(data,P,W,S,Loot)
 assert(W.waveNum==8 and P.skills.multi_shot==2 and P.x==expectedX)
end)
test('Relic upgrade debits gold and applies stat',function()
 Save.metaGold=100 Save.relicLevels={} Save.UpgradeRelic('strength',2)
 assert(Save.metaGold==98 and Save.relicLevels.strength==1)
end)
test('Totem equip unequip sell',function()
 Save.totems={} Save.equippedTotems={} Save.highestWave=100
 assert(Save.AddTotem({typeId='hp',rarity='junk'})) assert(Save.EquipTotem(1))
 assert(#Save.equippedTotems==1) Save.UnequipTotem(1) assert(#Save.equippedTotems==0)
 local count=#Save.totems Save.SellTotem(1) assert(#Save.totems==count-1)
end)
test('Rune ownership and two-slot limit',function()
 Save.ownedRunes={} Save.equippedRunes={} local ids={}
 for _,r in ipairs(R.RUNES)do ids[#ids+1]=r.id assert(Save.AddRune(r.id))end
 assert(Save.EquipRune(ids[1])) assert(Save.EquipRune(ids[2])) assert(not Save.EquipRune(ids[3]))
 assert(not Save.EquipRune(ids[1])) assert(Save.UnequipRune(1))
end)
test('All skin equip routes',function()
 Save.ownedSkins={}
 for _,skin in ipairs(require('meta.SkinSystem').SKINS)do assert(Save.UnlockSkin(skin.id)) assert(Save.EquipSkin(skin.charId,skin.id)) ready(skin.charId) assert(P.charDef.playerEmoji.idle==skin.playerEmoji.idle)end
end)
test('Death transitions to settlement',function()
 Save.equippedRunes={} Save.equippedTotems={} ready('cat') P.invTimer=0 P.hp=1 P.guardianCharges=0 P.shieldCharges=0
 E.Spawn(P.x,P.y,'tank',1) E.active[#E.active].atk=100000 E.active[#E.active].hp=100000
 B.Update(.016,0,0,720,1280)
 assert(B.state==B.STATE_GAME_OVER or gameState==2)
end)
UNITY_RESULTS=results
print('UNITY_ACCEPTANCE_RESULTS '..cjson.encode(results))
gameState=0 H.paused=false SS.Hide()
