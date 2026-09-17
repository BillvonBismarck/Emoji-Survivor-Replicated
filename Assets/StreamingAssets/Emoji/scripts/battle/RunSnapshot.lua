-- A versioned graph snapshot preserves shared references (homing targets,
-- charmed enemies, hit maps) without serializing functions or engine objects.
local Snapshot = {}
local modules = {
 'battle.Player','battle.Wave','battle.Enemy','battle.Projectile','battle.EnemyBullet',
 'battle.Loot','battle.Skill','battle.BattleScene','battle.MapVariant',
 'battle.MapEvent','battle.SpecialTerrain'
}
local excluded = {pool=true,spatialHash=true,bossDef=true,currentBossDef=true}
local function eligible(k,v)
 return type(k)=='string' and not excluded[k] and k:sub(1,1)~='_' and
 not k:match('^[A-Z_]+$') and (type(v)=='table' or type(v)=='number' or type(v)=='boolean' or type(v)=='string')
end
function Snapshot.Pack(root)
 local nodes,seen={},{}
 local function encode(v)
  local t=type(v)
  if t=='table' then
   if seen[v] then return {ref=seen[v]} end
   local id=#nodes+1;seen[v]=id;local entries={};nodes[id]={entries=entries}
   for k,value in pairs(v) do
    if type(k)~='function' and type(value)~='function' and type(value)~='userdata' then
     local ek,ev=encode(k),encode(value)
     if ek~=nil and ev~=nil then entries[#entries+1]={key=ek,value=ev} end
    end
   end
   return {ref=id}
  elseif t=='number' then if v==v and v~=math.huge and v~=-math.huge then return v end
  elseif t=='string' or t=='boolean' then return v end
 end
 local reference=encode(root)
 return {schema=1,root=reference,nodes=nodes}
end
function Snapshot.Unpack(data)
 assert(type(data)=='table' and data.schema==1 and type(data.nodes)=='table','Invalid run snapshot')
 assert(#data.nodes<=100000,'Run snapshot too large')
 local tables={};for i=1,#data.nodes do tables[i]={} end
 local function decode(v)
  if type(v)=='table' then assert(type(v.ref)=='number' and tables[v.ref],'Invalid snapshot reference');return tables[v.ref] end
  assert(type(v)=='string' or type(v)=='number' or type(v)=='boolean','Invalid snapshot value');return v
 end
 for i,node in ipairs(data.nodes) do
  assert(type(node.entries)=='table','Invalid snapshot node')
  for _,entry in ipairs(node.entries) do tables[i][decode(entry.key)]=decode(entry.value) end
 end
 return decode(data.root)
end
function Snapshot.Validate(root)
 assert(type(root)=="table" and type(root.modules)=="table" and type(root.context)=="table" and type(root.private)=="table","Incomplete snapshot")
 for _,name in ipairs(modules) do assert(type(root.modules[name])=="table","Missing module "..name) end
 local P=root.modules["battle.Player"];local W=root.modules["battle.Wave"];local E=root.modules["battle.Enemy"]
 assert(type(P.hp)=="number" and P.hp>0 and type(P.x)=="number" and type(P.skills)=="table","Invalid player")
 assert(type(W.waveNum)=="number" and type(E.active)=="table","Invalid wave")
 return root
end
function Snapshot.Capture(metadata)
 local root={modules={},private={},elapsed=time.elapsedTime,metadata=metadata or {}}
 for _,name in ipairs(modules) do
  local m=require(name);local values={};root.modules[name]=values
  for k,v in pairs(m) do if eligible(k,v) then values[k]=v end end
 end
 for _,name in ipairs({'battle.Skill','battle.RuneEffects','battle.RuneResonance','battle.SpecialTerrain','ui.DamageStats'}) do
  root.private[name]=require(name).ExportRunState()
 end
 local S=require('SaveData')
 root.sessionTotems=S.sessionTotems;root.sessionRunes=S.sessionRunes
 local D=require('meta.DailyChallenge');local W=require('meta.WeeklyChallenge')
 root.context={difficulty=require('Config').currentDiffIdx,daily=D.active,dailyConfig=D.todayConfig,singleSkillId=D.singleSkillId,
 weekly=W.active,weeklyRule=W.todayRule,weeklySeason=W.runSeasonId or W.seasonId,weeklyDay=W.runDay or W.seasonDay}
 return Snapshot.Pack(root)
end
function Snapshot.Prepare(root)
 local C=require('Config');C.currentDiffIdx=root.context.difficulty or 2
 local D=require('meta.DailyChallenge');local W=require('meta.WeeklyChallenge')
 D.Reset();W.Reset()
 D.active=root.context.daily==true;D.todayConfig=root.context.dailyConfig;D.singleSkillId=root.context.singleSkillId
 W.active=root.context.weekly==true;W.todayRule=root.context.weeklyRule
 W.runSeasonId=root.context.weeklySeason;W.runDay=root.context.weeklyDay
end
function Snapshot.Restore(root)
 for _,name in ipairs(modules) do
  local m=require(name);local values=assert(root.modules[name],'Missing snapshot module '..name)
  for k,v in pairs(m) do if eligible(k,v) then m[k]=nil end end
  for k,v in pairs(values) do m[k]=v end
 end
 local C=require('Config');local P=require('battle.Player');P.charDef=P.charDef or C.GetCharacter(P.charId)
 local E=require('battle.Enemy')
 for _,enemy in ipairs(E.active) do
  if enemy.isBoss then
   for _,def in ipairs(C.BOSSES) do if def.id==enemy.bossId then enemy.bossDef=def;break end end
  end
 end
 E.currentBossDef=E.currentBoss and E.currentBoss.bossDef or nil
 for name,data in pairs(root.private) do require(name).ImportRunState(data) end
 local S=require('SaveData');S.sessionTotems=root.sessionTotems or {};S.sessionRunes=root.sessionRunes or {}
 time.elapsedTime=root.elapsed or time.elapsedTime
 -- Spatial indices are deliberately rebuilt from restored live entities.
 local hash=E._spatialHash
 if hash then hash:Clear();for _,e in ipairs(E.active) do if e.alive and not e.dying then hash:Insert(e) end end end
end
return Snapshot
