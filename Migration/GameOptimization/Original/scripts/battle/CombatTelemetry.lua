local T={recent={},frame=0,frameHits=0,lastDeath=nil}
function T.Reset()T.recent={} T.frame=0 T.frameHits=0 T.lastDeath=nil end
function T.BeginFrame()T.frame=T.frame+1 T.frameHits=0 end
function T.Begin(p,amount,context)
    context=context or {source='unknown'}
    local hits=context.hitCount or 1
    T.frameHits=T.frameHits+hits
    return {frame=T.frame,wave=require('battle.Wave').waveNum,level=p.level,hpBefore=p.hp,maxHp=p.maxHp,
      source=context.source or 'unknown',enemyType=context.enemyType,enemyId=context.enemyId,
      hitCount=hits,sameFrameHitCount=T.frameHits,baseDamage=context.baseDamage or amount,
      quantityFactor=context.quantityFactor or 1,compressionMultiplier=context.compressionMultiplier or 1,
      compressedDamage=context.compressedDamage or amount,incomingDamage=amount,
      summedCandidateDamage=context.summedCandidateDamage or amount,
      invincibility=p.invTimer,guardian=p.guardianCharges,shield=p.shieldCharges,armor=p.relicArmorBonus,
      runeState=require('battle.RuneEffects').GetDiagnosticState(),contributors=context.contributors}
end
function T.Finish(p,r,amount,outcome)
    r.afterReductionDamage=amount r.hpAfter=p.hp r.outcome=outcome
    r.guardianAfter=p.guardianCharges r.shieldAfter=p.shieldCharges
    T.recent[#T.recent+1]=r;if #T.recent>64 then table.remove(T.recent,1) end
    if outcome=='death' then
        T.lastDeath=r
        local ok,json=pcall(function()return require('cjson').encode({death=r,recent=T.recent})end)
        if ok then
            print('[CombatDeath] '..json)
            local f=File('combat_death.json',FILE_WRITE)
            if f:IsOpen() then f:WriteString(json) f:Close() end
        end
    end
end
return T
