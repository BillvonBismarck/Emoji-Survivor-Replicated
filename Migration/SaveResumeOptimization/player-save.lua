local S=require('SaveData');S.UnlockAll();S.nickname='Test'
gameState=3;HandleCharSelectTouch(360,674)
local P=require('battle.Player');local W=require('battle.Wave');local E=require('battle.Enemy');local B=require('battle.BattleScene');local H=require('ui.HUD')
P.maxHp=100;P.hp=73;P.level=27;P.invTimer=999;W.waveNum=40;W.bossAlive=true;B.sessionGold=127;E.Reset()
local boss=E.SpawnBoss(P.x+400,P.y,require('Config').BOSSES[4],3);boss.hp=321
H.paused=true;H.onSuspend();assert(gameState==0 and S.HasGameSave());print('[DiskResumeTest] SAVE PASS')
