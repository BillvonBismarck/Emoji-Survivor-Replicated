using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace EmojiBridge {
public sealed class LuaRuntime : IDisposable {
 const string D="emoji_lua54";
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int Callback(IntPtr state);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr luaL_newstate();
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void luaL_openlibs(IntPtr l);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_close(IntPtr l);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern int luaL_loadbufferx(IntPtr l,byte[] b,UIntPtr size,string name,string mode);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern int lua_pcallk(IntPtr l,int args,int results,int err,IntPtr ctx,IntPtr k);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_pushcclosure(IntPtr l,Callback cb,int n);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_setglobal(IntPtr l,string name);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern int lua_getglobal(IntPtr l,string name);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_pushnumber(IntPtr l,double n);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr lua_pushlstring(IntPtr l,byte[] s,UIntPtr len);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_pushboolean(IntPtr l,int b);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern double lua_tonumberx(IntPtr l,int i,IntPtr valid);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern IntPtr lua_tolstring(IntPtr l,int i,out UIntPtr len);
 [DllImport(D,CallingConvention=CallingConvention.Cdecl)] static extern void lua_settop(IntPtr l,int i);
 public IntPtr State {get;private set;}
 readonly List<Callback> callbacks=new List<Callback>();
 public LuaRuntime(){State=luaL_newstate();luaL_openlibs(State);}
 public double Num(int i)=>lua_tonumberx(State,i,IntPtr.Zero);
 public string Str(int i){var p=lua_tolstring(State,i,out var n);if(p==IntPtr.Zero)return "";var b=new byte[(int)n.ToUInt64()];Marshal.Copy(p,b,0,b.Length);return Encoding.UTF8.GetString(b);}
 public int Push(double n){lua_pushnumber(State,n);return 1;}
 public int Push(bool b){lua_pushboolean(State,b?1:0);return 1;}
 public int Push(string s){var b=Encoding.UTF8.GetBytes(s??"");lua_pushlstring(State,b,(UIntPtr)b.Length);return 1;}
 public void Bind(string name,Func<int> action){Callback cb=_=>{try{return action();}catch(Exception e){UnityEngine.Debug.LogError("[Lua bridge] "+name+": "+e);return 0;}};callbacks.Add(cb);lua_pushcclosure(State,cb,0);lua_setglobal(State,name);}
 public void Set(string name,string s){Push(s);lua_setglobal(State,name);}
 public void Set(string name,double v){Push(v);lua_setglobal(State,name);}
 public void Execute(string source,string name="bridge"){var b=Encoding.UTF8.GetBytes(source);Check(luaL_loadbufferx(State,b,(UIntPtr)b.Length,name,null));Check(lua_pcallk(State,0,0,0,IntPtr.Zero,IntPtr.Zero));}
 public void Call(string name){lua_getglobal(State,name);Check(lua_pcallk(State,0,0,0,IntPtr.Zero,IntPtr.Zero));}
 public double GetNumber(string name){lua_getglobal(State,name);var n=Num(-1);lua_settop(State,-2);return n;}
 void Check(int result){if(result==0)return;string e=Str(-1);lua_settop(State,-2);throw new Exception(e);}
 public void Dispose(){if(State!=IntPtr.Zero){lua_close(State);State=IntPtr.Zero;}callbacks.Clear();}
}}
