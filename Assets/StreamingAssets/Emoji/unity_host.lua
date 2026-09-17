-- Unity engine boundary. The files in scripts/ are byte-for-byte source copies.
package.path = UNITY_ROOT..'/scripts/?.lua;'..UNITY_ROOT..'/scripts/?/init.lua;'..package.path
local function noop() end
print=function(...) local t={} for i=1,select('#',...) do t[i]=tostring(select(i,...)) end unity_log(table.concat(t,'\t')) end
graphics={GetWidth=function()return 720 end,GetHeight=function()return 1280 end}
time={elapsedTime=0,timeStep=0}
input={mousePosition={x=0,y=0},mouseMoveWheel=0,GetKeyDown=function(_,k)return unity_key(k,0) end,GetKeyPress=function(_,k)return unity_key(k,1) end,GetMouseButtonDown=function()return UNITY_DOWN==1 end}
for _,k in ipairs{'A','D','W','S','UP','DOWN','LEFT','RIGHT','TAB','E','G','K','O','P','Q','ESCAPE','BACKSPACE','RETURN','KP_ENTER'} do _G['KEY_'..k]=k end
MOUSEB_LEFT=1 FILE_READ=0 FILE_WRITE=1 HA_LEFT=0 HA_CENTER=1 HA_RIGHT=2 VA_BOTTOM=2
NVG_ALIGN_LEFT=1 NVG_ALIGN_CENTER=2 NVG_ALIGN_RIGHT=4 NVG_ALIGN_TOP=8 NVG_ALIGN_MIDDLE=16 NVG_ALIGN_BOTTOM=32 NVG_CW=1 NVG_ROUND=1
Vector2=function(x,y)return {x=x,y=y}end
SubscribeToEvent=noop UnsubscribeFromAllEvents=noop
local unityJoystick
VirtualControls={Initialize=noop,Shutdown=noop,CreateJoystick=function(o)
 unityJoystick=o o.x=0 o.y=0
 o._updateShouldShow=noop o.getMovement=function(self)return self.x,self.y end
 return o end}
package.preload['urhox-libs.UI.VirtualControls']=function()return VirtualControls end
local function quote(s)return unity_json_quote(s)end
local function encode(v,seen)
 local t=type(v)
 if t=='nil' then return 'null' elseif t=='boolean' or t=='number' then return tostring(v) elseif t=='string' then return quote(v) elseif t~='table' then error('unsupported JSON type '..t) end
 seen=seen or {} if seen[v] then error('JSON cycle')end seen[v]=true
 local n=0 local arr=true for k in pairs(v) do n=n+1 if type(k)~='number' or k<1 or k%1~=0 then arr=false end end
 arr=arr and n>0 and n==#v
 local out={} if arr then for i=1,n do out[i]=encode(v[i],seen)end else for k,x in pairs(v) do out[#out+1]=quote(tostring(k))..':'..encode(x,seen)end end
 seen[v]=nil return (arr and '[' or '{')..table.concat(out,',')..(arr and ']' or '}')
end
cjson={encode=encode,decode=function(s)local src=unity_json_lua(s) assert(src and #src>0,'Invalid JSON') local f,e=load('return '..src,'json','t',{}) if not f then error(e)end return f()end}
package.loaded.cjson=cjson
File=function(name,mode)
 local f={name=name,mode=mode,open=true}
 function f:IsOpen()return self.open and (self.mode==FILE_WRITE or unity_exists(self.name))end
 function f:ReadString()return unity_read(self.name)end
 function f:WriteString(s)assert(unity_write(self.name,s),'Save write failed')end
 function f:Close()self.open=false end
 return f
end
fileSystem={FileExists=function(_,n)return unity_exists(n)end,SystemOpen=function(_,url)print('[Unity] External link: '..url)end}
local sourceId=0
Node=function()return {CreateComponent=function()
 sourceId=sourceId+1 local s={id=sourceId,gain=1}
 s.Play=function(self,clip)unity_audio(self.id,clip.path,self.gain,clip.looped and 1 or 0)end
 s.Stop=function(self)unity_audio(self.id,'',0,0)end
 return s end}end
cache={GetResource=function(_,kind,path)return {path=path}end}
nvgCreate=function()return 1 end nvgDelete=noop nvgCreateFont=function(_,name)return name=='zpix' and 2 or 1 end
nvgRGBA=function(r,g,b,a)return {r/255,g/255,b/255,a/255}end
nvgRGBAf=function(r,g,b,a)return {r,g,b,a}end
local ops={'BeginFrame','EndFrame','BeginPath','ClosePath','MoveTo','LineTo','Rect','RoundedRect','Circle','Ellipse','Arc','Fill','Stroke','Save','Restore','Translate','Scale','Rotate','StrokeWidth','FontSize','TextAlign','GlobalAlpha','Scissor','FontFaceId','LineCap','TextLineHeight'}
for i,name in ipairs(ops)do _G['nvg'..name]=function(_,...)return unity_draw(i,...)end end
nvgFontFace=function(_,s)unity_draw(24,s=='zpix' and 2 or 1)end
nvgFillColor=function(_,c)unity_draw(30,table.unpack(c))end
nvgStrokeColor=function(_,c)unity_draw(31,table.unpack(c))end
nvgLinearGradient=function(_,x,y,x2,y2,c1,c2)return {1,x,y,x2,y2,c1,c2}end
nvgRadialGradient=function(_,x,y,r1,r2,c1,c2)return {2,x,y,r1,r2,c1,c2}end
nvgBoxGradient=function(_,x,y,w,h,r,f,c1,c2)return {3,x,y,w,h,c1,c2,r,f}end
nvgFillPaint=function(_,p)unity_draw(32,p[1],p[2],p[3],p[4],p[5],table.unpack(p[6]));unity_draw(33,table.unpack(p[7]));if p[1]==3 then unity_draw(34,p[8],p[9])end end
local I18n = require("utils.I18n")
nvgUserText=function(_,x,y,s)return unity_text(x,y,tostring(s),0)end
nvgUserTextBounds=function(_,x,y,s)return unity_measure(tostring(s))end
nvgText=function(_,x,y,s)return unity_text(x,y,I18n.Localize(s),0)end
nvgTextBounds=function(_,x,y,s)return unity_measure(I18n.Localize(s))end
nvgTextBox=function(_,x,y,w,s)return unity_text(x,y,I18n.Localize(s),w)end
local function ev(v)return {GetFloat=function()return v end,GetInt=function()return v end,GetString=function()return v end}end
require('main')
function UnityStart()Start()end
function UnityTick()
 time.timeStep=UNITY_DT time.elapsedTime=time.elapsedTime+UNITY_DT
 input.mousePosition.x=UNITY_MX input.mousePosition.y=UNITY_MY input.mouseMoveWheel=UNITY_WHEEL
 local j=unityJoystick
 if j then
  j.cx=(j.alignment[1]==HA_LEFT and 0 or j.alignment[1]==HA_RIGHT and 720 or 360)+j.position.x
  j.cy=1280+j.position.y
  if UNITY_PRESS==1 and j.visible and (UNITY_MX-j.cx)^2+(UNITY_MY-j.cy)^2<=150^2 then j.active=true j.bx=UNITY_MX j.by=UNITY_MY end
  if UNITY_DOWN~=1 or not j.visible then j.active=false end
  j.x=0 j.y=0
  if j.active then local dx,dy=UNITY_MX-j.bx,UNITY_MY-j.by local len=math.sqrt(dx*dx+dy*dy) local mag=math.min(1,len/50) if mag>.15 then j.x=dx/len*(mag-.15)/.85 j.y=dy/len*(mag-.15)/.85 end end
 end
 HandleUpdate(nil,{TimeStep=ev(UNITY_DT)})
 if UNITY_PRESS==1 then HandleMouseDown(nil,{Button=ev(1)})end
 if UNITY_DOWN==1 then HandleMouseMove(nil,{})end
 if UNITY_RELEASE==1 then HandleMouseUp(nil,{Button=ev(1)})end
 if UNITY_TEXT~='' then HandleTextInput(nil,{Text=ev(UNITY_TEXT)})end
end
function UnityRender()
 HandleNanoVGRender(nil,{})
 local j=unityJoystick
 if j and j.visible and gameState==1 and not require("ui.HUD").paused then
  local x,y=j.active and j.bx or j.cx,j.active and j.by or j.cy
  x=x or 360 y=y or 1080
  local alpha=j.active and 217 or 128
  nvgSave(1) nvgBeginPath(1) nvgCircle(1,x,y,80) nvgFillColor(1,nvgRGBA(0,0,0,alpha*.3)) nvgFill(1)
  nvgStrokeWidth(1,2) nvgStrokeColor(1,nvgRGBA(255,255,255,alpha*.15)) nvgStroke(1)
  nvgBeginPath(1) nvgCircle(1,x,y,40) nvgStrokeWidth(1,1) nvgStrokeColor(1,nvgRGBA(255,255,255,alpha*.2)) nvgStroke(1)
  local kx,ky=x+j.x*50,y+j.y*50 local radius=j.knobRadius or 30
  nvgBeginPath(1) nvgCircle(1,kx,ky,radius)
  nvgFillPaint(1,nvgRadialGradient(1,kx,ky-radius*.3,radius*.1,radius,nvgRGBA(255,255,255,alpha*.5),nvgRGBA(200,200,200,alpha*.5))) nvgFill(1)
  nvgStrokeWidth(1,1.5) nvgStrokeColor(1,nvgRGBA(255,255,255,alpha*.4)) nvgStroke(1) nvgRestore(1)
 end
end
