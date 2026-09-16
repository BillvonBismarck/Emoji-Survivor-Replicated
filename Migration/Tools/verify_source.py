"""Read-only source integrity and Lua 5.4 syntax audit. Python standard library only."""
import ctypes
import hashlib
import json
from pathlib import Path

SOURCE = Path(r'C:\Test\Emoji_Survivor')
PROJECT = Path(r'C:\Test\Emoji_Survivor_Unity_Test\Emoji_Survivor_Test')
runtime = ctypes.CDLL(str(PROJECT / 'Assets/Plugins/x86_64/emoji_lua54.dll'))
runtime.luaL_newstate.restype = ctypes.c_void_p
runtime.luaL_loadbufferx.argtypes = [ctypes.c_void_p, ctypes.c_char_p, ctypes.c_size_t, ctypes.c_char_p, ctypes.c_char_p]
runtime.lua_tolstring.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_void_p]
runtime.lua_tolstring.restype = ctypes.c_char_p
runtime.lua_settop.argtypes = [ctypes.c_void_p, ctypes.c_int]
runtime.lua_close.argtypes = [ctypes.c_void_p]
state = runtime.luaL_newstate()
files = []
try:
    for src in sorted((SOURCE / 'scripts').rglob('*.lua')):
        relative = src.relative_to(SOURCE)
        data = src.read_bytes()
        dst = PROJECT / 'Assets/StreamingAssets/Emoji' / relative
        status = runtime.luaL_loadbufferx(state, data, len(data), str(relative).encode(), None)
        error = runtime.lua_tolstring(state, -1, None).decode('utf8', 'replace') if status else None
        runtime.lua_settop(state, 0)
        files.append({'path': relative.as_posix(), 'sha256': hashlib.sha256(data).hexdigest(),
                      'bytes': len(data), 'identical': dst.is_file() and data == dst.read_bytes(),
                      'syntaxPass': status == 0, 'error': error})
finally:
    runtime.lua_close(state)
audio = []
for src in sorted((SOURCE / 'assets/audio').rglob('*.ogg')):
    relative = src.relative_to(SOURCE / 'assets')
    dst = PROJECT / 'Assets/EmojiBridge/Resources' / relative
    audio.append({'path':relative.as_posix(), 'sha256':hashlib.sha256(src.read_bytes()).hexdigest(),
                  'identical':dst.is_file() and dst.read_bytes()==src.read_bytes()})
result = {'luaFiles':len(files), 'luaBytes':sum(f['bytes'] for f in files),
          'allLuaIdentical':all(f['identical'] for f in files), 'allLuaSyntaxPass':all(f['syntaxPass'] for f in files),
          'audioFiles':len(audio), 'allAudioIdentical':all(f['identical'] for f in audio), 'files':files,'audio':audio}
print(json.dumps(result,ensure_ascii=False,indent=2))
raise SystemExit(0 if result['allLuaIdentical'] and result['allLuaSyntaxPass'] and result['allAudioIdentical'] else 1)
