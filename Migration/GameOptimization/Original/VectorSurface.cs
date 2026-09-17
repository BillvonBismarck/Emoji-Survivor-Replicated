using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace EmojiBridge {
public sealed class VectorSurface : VisualElement {
 public Action Render;
 public Action<Exception> RenderFailed;
 public FontAsset MainFont,PixelFont;
 public Texture2D EmojiAtlas;
 public Dictionary<uint,Rect> EmojiGlyphs=new Dictionary<uint,Rect>();
 public Dictionary<string,string> EmojiSequences=new Dictionary<string,string>();
 readonly Dictionary<string,string[]> tokenCache=new Dictionary<string,string[]>();
 readonly Dictionary<string,FontAsset> fontCache=new Dictionary<string,FontAsset>();
 readonly HashSet<string> sequencePrefixes=new HashSet<string>();
 public int MissingGlyphCount {get;private set;}
 public double LastRenderMilliseconds {get;private set;}
 public long LastRenderAllocatedBytes {get;private set;}
 IEnumerable<string> Tokens(string input){
  if(tokenCache.TryGetValue(input,out var cached))return cached;
  if(sequencePrefixes.Count==0)foreach(var key in EmojiSequences.Keys)if(key.Length>=2)sequencePrefixes.Add(key.Substring(0,2));
  string text=input.Replace("\uFE0F","");var tokens=new List<string>();
  for(int i=0;i<text.Length;){string token=null;int consumed=0;
   if(i+1<text.Length&&sequencePrefixes.Contains(text.Substring(i,2)))for(int n=Math.Min(24,text.Length-i);n>1;n--)if(EmojiSequences.TryGetValue(text.Substring(i,n),out token)){consumed=n;break;}
   if(consumed==0){consumed=char.IsHighSurrogate(text[i])&&i+1<text.Length&&char.IsLowSurrogate(text[i+1])?2:1;token=text.Substring(i,consumed);if(consumed==1&&char.IsSurrogate(token[0]))token="?";}
   i+=consumed;
   // UrhoX's bundled font predates Unicode 15 WING. Use its existing dove glyph.
   if(token=="🪽")token="🕊";if(token=="✦")token="⭐";if(token=="✧")token="✨";
   tokens.Add(token);
  }
  var result=tokens.ToArray();if(input.Length<256){if(tokenCache.Count>=2048)tokenCache.Clear();tokenCache[input]=result;}return result;
 }
 public void Prewarm(string text){state.font=2;var seen=new HashSet<string>();foreach(var token in Tokens(text))if(seen.Add(token))CharacterFont(token);}

 MeshGenerationContext context;
 struct State {public Matrix4x4 matrix;public Color fill,stroke,outer;public float width,size,alpha,lineHeight,corner,feather;public int align,font,gradient,cap;public Vector4 paint;public Rect clip;}
 State state;
 readonly Stack<State> stack=new Stack<State>();
 readonly List<List<Vector2>> paths=new List<List<Vector2>>();
 List<Vector2> path;
 readonly List<Vertex> geometry=new List<Vertex>(8192);
 public VectorSurface(){style.width=720;style.height=1280;style.flexShrink=0;generateVisualContent+=Draw;}
 void Draw(MeshGenerationContext c){var renderTimer=System.Diagnostics.Stopwatch.StartNew();long allocated=GC.GetAllocatedBytesForCurrentThread();context=c;geometry.Clear();state=new State{matrix=Matrix4x4.identity,fill=Color.white,stroke=Color.white,alpha=1,width=1,size=20,lineHeight=1.2f,align=1,font=2,clip=new Rect(0,0,720,1280)};stack.Clear();paths.Clear();try{Render?.Invoke();Flush();}catch(Exception e){RenderFailed?.Invoke(e);Debug.LogError(e);}context=null;LastRenderMilliseconds=renderTimer.Elapsed.TotalMilliseconds;LastRenderAllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-allocated;}
 void Flush(){if(geometry.Count==0||context==null)return;var mesh=context.Allocate(geometry.Count,geometry.Count);for(int i=0;i<geometry.Count;i++){mesh.SetNextVertex(geometry[i]);mesh.SetNextIndex((ushort)i);}geometry.Clear();}
 Vector2 P(float x,float y)=>state.matrix.MultiplyPoint3x4(new Vector3(x,y));
 float Scale=>state.matrix.MultiplyVector(Vector3.right).magnitude;
 void Move(Vector2 p){path=new List<Vector2>{p};paths.Add(path);}
 void Add(Vector2 p){if(path==null)Move(p);else path.Add(p);}
 public int Op(LuaRuntime l){int op=(int)l.Num(1);float a=(float)l.Num(2),b=(float)l.Num(3),c=(float)l.Num(4),d=(float)l.Num(5),e=(float)l.Num(6);
 switch(op){
 case 1:case 2:break;
 case 3:paths.Clear();path=null;break;
 case 4:if(path!=null&&path.Count>0)Add(path[0]);break;
 case 5:Move(P(a,b));break;case 6:Add(P(a,b));break;
 case 7:Move(P(a,b));Add(P(a+c,b));Add(P(a+c,b+d));Add(P(a,b+d));Add(P(a,b));break;
 case 8:Rounded(a,b,c,d,e);break;
 case 9:Ellipse(a,b,c,c);break;case 10:Ellipse(a,b,c,d);break;
 case 11:float end=(float)l.Num(6);int steps=Mathf.Max(4,Mathf.CeilToInt(Mathf.Abs(end-d)*Mathf.Sqrt(Mathf.Max(c,1))));for(int i=0;i<=steps;i++){float t=Mathf.Lerp(d,end,i/(float)steps);Add(P(a+Mathf.Cos(t)*c,b+Mathf.Sin(t)*c));}break;
 case 12:foreach(var points in paths)Fill(points);break;
 case 13:foreach(var points in paths)Stroke(points);break;
 case 14:stack.Push(state);break;case 15:if(stack.Count>0)state=stack.Pop();break;
 case 16:state.matrix*=Matrix4x4.Translate(new Vector3(a,b));break;
 case 17:state.matrix*=Matrix4x4.Scale(new Vector3(a,b,1));break;
 case 18:state.matrix*=Matrix4x4.Rotate(Quaternion.Euler(0,0,a*Mathf.Rad2Deg));break;
 case 19:state.width=a;break;case 20:state.size=a;break;case 21:state.align=(int)a;break;case 22:state.alpha=a;break;
 case 23:var tl=P(a,b);var br=P(a+c,b+d);state.clip=Rect.MinMaxRect(Mathf.Max(0,tl.x),Mathf.Max(0,tl.y),Mathf.Min(720,br.x),Mathf.Min(1280,br.y));break;
 case 24:state.font=(int)a;break;case 25:state.cap=(int)a;break;case 26:state.lineHeight=a;break;
 case 30:state.fill=new Color(a,b,c,d);state.gradient=0;break;case 31:state.stroke=new Color(a,b,c,d);break;
 case 32:state.gradient=(int)a;var origin=P(b,c);state.paint=new Vector4(origin.x,origin.y,d*Scale,e*Scale);if(state.gradient==1){var dest=P(d,e);state.paint.z=dest.x;state.paint.w=dest.y;}state.fill=new Color((float)l.Num(7),(float)l.Num(8),(float)l.Num(9),(float)l.Num(10));break;
 case 33:state.outer=new Color(a,b,c,d);break;
 case 34:state.corner=a*Scale;state.feather=b*Scale;break;
 }return 0;}
 void Ellipse(float x,float y,float rx,float ry){int steps=Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(rx,ry))*5),16,80);for(int i=0;i<steps;i++){float t=i*Mathf.PI*2/steps;var p=P(x+Mathf.Cos(t)*rx,y+Mathf.Sin(t)*ry);if(i==0)Move(p);else Add(p);}Add(path[0]);}
 void Rounded(float x,float y,float w,float h,float r){r=Mathf.Min(r,Mathf.Min(Mathf.Abs(w),Mathf.Abs(h))/2);for(int corner=0;corner<4;corner++){float cx=x+(corner==0||corner==3?w-r:r),cy=y+(corner<2?h-r:r);for(int i=0;i<=6;i++){float t=(corner*90+i*15)*Mathf.Deg2Rad;var p=P(cx+Mathf.Cos(t)*r,cy+Mathf.Sin(t)*r);if(corner==0&&i==0)Move(p);else Add(p);}}Add(path[0]);}
 Color ColorAt(Vector2 p){Color color=state.fill;var g=state.paint;if(state.gradient==2){float t=(Vector2.Distance(p,new Vector2(g.x,g.y))-g.z)/Mathf.Max(1,g.w-g.z);color=Color.Lerp(state.fill,state.outer,t);}else if(state.gradient==1){var v=new Vector2(g.z-g.x,g.w-g.y);float t=Vector2.Dot(p-new Vector2(g.x,g.y),v)/Mathf.Max(1,v.sqrMagnitude);color=Color.Lerp(state.fill,state.outer,t);}else if(state.gradient==3){var q=new Vector2(Mathf.Abs(p.x-g.x-g.z/2)-g.z/2+state.corner,Mathf.Abs(p.y-g.y-g.w/2)-g.w/2+state.corner);float distance=Mathf.Min(Mathf.Max(q.x,q.y),0)+new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude-state.corner;color=Color.Lerp(state.fill,state.outer,(distance+state.feather/2)/Mathf.Max(1,state.feather));}color.a*=state.alpha;return color;}
 void FillTriangle(Vector2 a,Vector2 b,Vector2 c,int depth=0){if(state.gradient==3&&depth<2){var ab=(a+b)/2;var bc=(b+c)/2;var ca=(c+a)/2;FillTriangle(a,ab,ca,depth+1);FillTriangle(ab,b,bc,depth+1);FillTriangle(ca,bc,c,depth+1);FillTriangle(ab,bc,ca,depth+1);}else Triangle(a,b,c,ColorAt(a),ColorAt(b),ColorAt(c));}
 void Fill(List<Vector2> ps){int count=ps.Count;if(count>1&&(ps[0]-ps[count-1]).sqrMagnitude<.0001f)count--;if(count<3)return;bool positive=false,negative=false;for(int i=0;i<count;i++){float cross=Cross(ps[(i+1)%count]-ps[i],ps[(i+2)%count]-ps[(i+1)%count]);positive|=cross>.001f;negative|=cross<-.001f;}
 if(!(positive&&negative)){var center=Vector2.zero;for(int i=0;i<count;i++)center+=ps[i];center/=count;for(int i=0;i<count;i++)FillTriangle(center,ps[i],ps[(i+1)%count]);return;}
 var indices=new List<int>();float area=0;for(int i=0;i<count;i++){indices.Add(i);area+=Cross(ps[i],ps[(i+1)%count]);}if(area<0)indices.Reverse();int budget=count*count;while(indices.Count>2&&budget-->0){bool found=false;for(int i=0;i<indices.Count;i++){int ai=indices[(i+indices.Count-1)%indices.Count],bi=indices[i],ci=indices[(i+1)%indices.Count];var a=ps[ai];var b=ps[bi];var c=ps[ci];if(Cross(b-a,c-b)<=.00001f)continue;bool inside=false;foreach(int j in indices){if(j==ai||j==bi||j==ci)continue;var p=ps[j];if(Cross(b-a,p-a)>0&&Cross(c-b,p-b)>0&&Cross(a-c,p-c)>0){inside=true;break;}}if(inside)continue;FillTriangle(a,b,c);indices.RemoveAt(i);found=true;break;}if(!found)break;}}
 readonly List<Vector2> strokeNormals=new List<Vector2>(96);
 void Stroke(List<Vector2> ps){if(ps.Count<2)return;var color=state.stroke;color.a*=state.alpha;if(color.a<=0)return;float half=state.width*Scale*.5f;
  bool closed=(ps[0]-ps[ps.Count-1]).sqrMagnitude<.0001f;int count=closed?ps.Count-1:ps.Count;strokeNormals.Clear();
  for(int i=0;i<count;i++){var prev=ps[i]-ps[(i+count-1)%count];var next=ps[(i+1)%count]-ps[i];if(!closed&&i==0)prev=next;if(!closed&&i==count-1)next=prev;
   var n0=new Vector2(-prev.y,prev.x).normalized;var n1=new Vector2(-next.y,next.x).normalized;var join=(n0+n1).normalized;float denom=Mathf.Max(.5f,Vector2.Dot(join,n1));strokeNormals.Add(join*(half/denom));}
  for(int i=1;i<(closed?count+1:count);i++){int j=i%count;var a=ps[i-1];var b=ps[j];var n0=strokeNormals[i-1];var n1=strokeNormals[j];Triangle(a-n0,a+n0,b+n1,color,color,color);Triangle(a-n0,b+n1,b-n1,color,color,color);}
  if(state.cap==1&&!closed)for(int end=0;end<2;end++){var p=ps[end==0?0:count-1];for(int i=0;i<10;i++){float a=i*Mathf.PI/5,b=(i+1)*Mathf.PI/5;Triangle(p,p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*half,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*half,color,color,color);}}
 }

 struct CV {public Vector2 p;public Color c;public CV(Vector2 pos,Color col){p=pos;c=col;}}
 readonly List<CV> clipA=new List<CV>(8),clipB=new List<CV>(8);
 int ClipCode(Vector2 p){var r=state.clip;return (p.x<r.xMin?1:0)|(p.x>r.xMax?2:0)|(p.y<r.yMin?4:0)|(p.y>r.yMax?8:0);}
 void Triangle(Vector2 a,Vector2 b,Vector2 c,Color ca,Color cb,Color cc){if(context==null)return;int ac=ClipCode(a),bc=ClipCode(b),ccode=ClipCode(c);if((ac&bc&ccode)!=0)return;if((ac|bc|ccode)==0){Emit(new CV(a,ca));if(Cross(b-a,c-a)>=0){Emit(new CV(b,cb));Emit(new CV(c,cc));}else{Emit(new CV(c,cc));Emit(new CV(b,cb));}if(geometry.Count>60000)Flush();return;}clipA.Clear();clipA.Add(new CV(a,ca));clipA.Add(new CV(b,cb));clipA.Add(new CV(c,cc));var src=clipA;var dst=clipB;for(int edge=0;edge<4;edge++){dst.Clear();if(src.Count==0)return;var prev=src[src.Count-1];float pd=Distance(prev.p,edge);foreach(var cur in src){float cd=Distance(cur.p,edge);if((pd>=0)!=(cd>=0)){float t=pd/(pd-cd);dst.Add(new CV(Vector2.Lerp(prev.p,cur.p,t),Color.Lerp(prev.c,cur.c,t)));}if(cd>=0)dst.Add(cur);prev=cur;pd=cd;}var tmp=src;src=dst;dst=tmp;}
 if(src.Count<3)return;for(int i=1;i<src.Count-1;i++){var v0=src[0];var v1=src[i];var v2=src[i+1];if(Cross(v1.p-v0.p,v2.p-v0.p)<0){var tmp=v1;v1=v2;v2=tmp;}Emit(v0);Emit(v1);Emit(v2);}if(geometry.Count>60000)Flush();}
 static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
 void Emit(CV v){geometry.Add(new Vertex{position=new Vector3(v.p.x,v.p.y,Vertex.nearZ),tint=v.c});}
 float Distance(Vector2 p,int edge){var r=state.clip;return edge==0?p.x-r.xMin:edge==1?r.xMax-p.x:edge==2?p.y-r.yMin:r.yMax-p.y;}
 FontAsset Font=>state.font==2?PixelFont:MainFont;
 FontAsset CharacterFont(string s){string key=state.font+":"+s;if(fontCache.TryGetValue(key,out var cached))return cached;uint ch=(uint)char.ConvertToUtf32(s,0);if(EmojiGlyphs.ContainsKey(ch)){fontCache[key]=null;return null;}FontAsset selected;Font.TryAddCharacters(s,out _);if(Font.characterLookupTable.ContainsKey(ch))selected=Font;else{MainFont.TryAddCharacters(s,out _);selected=MainFont;if(!MainFont.characterLookupTable.ContainsKey(ch)&&ch>0x20&&ch!=0x200D&&ch!=0xFE0E)MissingGlyphCount++;}fontCache[key]=selected;return selected;}

 float Advance(string s,FontAsset font){if(s=="\uFE0F"||s=="\uFE0E"||s=="\u200D")return 0;uint ch=(uint)char.ConvertToUtf32(s,0);if(font==null)return state.size;if(font.characterLookupTable.TryGetValue(ch,out var glyph))return glyph.glyph.metrics.horizontalAdvance*state.size/font.faceInfo.pointSize;return state.size;}
 public float Measure(string text){if(Font==null)return text.Length*state.size*.6f;float w=0;foreach(var s in Tokens(text))w+=Advance(s,CharacterFont(s));return w;}
 public void Text(float x,float y,string text,float wrap){if(context==null||string.IsNullOrEmpty(text))return;if(wrap>0){string line="";foreach(var rune in Tokens(text)){if(rune=="\n"||Measure(line+rune)>wrap){Text(x,y,line,0);y+=state.size*state.lineHeight;line=rune=="\n"?"":rune;}else line+=rune;}if(line.Length>0)Text(x,y,line,0);return;}
 float w=Measure(text);if((state.align&2)!=0)x-=w/2;else if((state.align&4)!=0)x-=w;
 if((state.align&16)!=0)y-=state.size*.6f;else if((state.align&32)!=0)y-=state.size*1.2f;else if((state.align&8)==0)y-=state.size;
 var p=P(x,y);if(p.y+state.size*Scale<state.clip.yMin||p.y>state.clip.yMax)return;var col=state.fill;col.a*=state.alpha;
 Flush();foreach(var s in Tokens(text)){var font=CharacterFont(s);float advance=Advance(s,font);if(advance>0){var cp=P(x,y);if(cp.x>=state.clip.xMin&&cp.x+advance*Scale<=state.clip.xMax){if(font==null)DrawEmoji(s,x,y,state.size);else context.DrawText(s,cp,state.size*Scale,col,font);}}x+=advance;}}
 void DrawEmoji(string s,float x,float y,float size){uint ch=(uint)char.ConvertToUtf32(s,0);var texture=EmojiAtlas;var r=EmojiGlyphs[ch];float u=r.x/(float)texture.width,v=r.y/(float)texture.height,uw=r.width/(float)texture.width,vh=r.height/(float)texture.height;var m=context.Allocate(4,6,texture);var color=new Color(1,1,1,state.alpha*state.fill.a);var a=P(x,y);var b=P(x+size,y);var c=P(x+size,y+size);var d=P(x,y+size);m.SetNextVertex(new Vertex{position=new Vector3(a.x,a.y,Vertex.nearZ),tint=color,uv=new Vector2(u,v+vh)});m.SetNextVertex(new Vertex{position=new Vector3(b.x,b.y,Vertex.nearZ),tint=color,uv=new Vector2(u+uw,v+vh)});m.SetNextVertex(new Vertex{position=new Vector3(c.x,c.y,Vertex.nearZ),tint=color,uv=new Vector2(u+uw,v)});m.SetNextVertex(new Vertex{position=new Vector3(d.x,d.y,Vertex.nearZ),tint=color,uv=new Vector2(u,v)});m.SetNextIndex(0);m.SetNextIndex(1);m.SetNextIndex(2);m.SetNextIndex(0);m.SetNextIndex(2);m.SetNextIndex(3);}
}
static class TextElements {public static IEnumerable<string> AsEnumerable(this System.Globalization.TextElementEnumerator e){while(e.MoveNext())yield return e.GetTextElement();}}
}
