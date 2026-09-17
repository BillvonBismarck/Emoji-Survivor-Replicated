-- Attack tuning uses fixed wave coefficients, never the live player's HP.
local Balance = {}
function Balance.AttackCoefficient(wave)
    local w=math.max(0,wave or 0)
    return 1 + .135*math.min(w,30) + .08*math.max(0,w-30)
end
function Balance.TypeMultiplier(kind)
    if kind=='elite' then return .42 end -- includes the existing 1.8 enrage multiplier
    if kind=='charger' then return .65 end
    return 1
end
function Balance.QuantityAttack(factor)
    return math.min(1.15,1+.08*math.log(math.max(1,factor or 1)))
end
function Balance.ProjectileDamage(raw)
    return math.max(1,math.floor(raw*.55))
end
return Balance
