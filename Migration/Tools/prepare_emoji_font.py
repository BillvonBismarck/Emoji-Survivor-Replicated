"""Derive a Unity-compatible cmap for the local UrhoX COLR font's ligatures."""
import sys,json
from pathlib import Path
sys.path.insert(0,r'C:\Test\Emoji_Survivor_Unity_Test\Emoji_Survivor_Test\Migration\PythonLibs')
from fontTools.ttLib import TTFont
from fontTools.ttLib.tables._c_m_a_p import CmapSubtable
root=Path(__file__).parent
source=Path(r'C:\UrhoXEditor\versions\0.9.0-beta.17_20260915.050138.7b1f206186bd\Res\Fonts\Twemoji.Mozilla.ttf')
font=TTFont(source)
cmap=font.getBestCmap()
reverse={glyph:chr(cp) for cp,glyph in cmap.items()}
lookups=font['GSUB'].table.LookupList.Lookup
ligatures=[]
for lookup in lookups:
    for table in lookup.SubTable:
        table=getattr(table,'ExtSubTable',table)
        for first,values in getattr(table,'ligatures',{}).items():
            for ligature in values:
                ligatures.append(([first]+list(ligature.Component),ligature.LigGlyph))
mapping={}
for _ in range(4):
    for components,glyph in ligatures:
        if all(c in reverse for c in components):
            sequence=''.join(reverse[c] for c in components)
            reverse.setdefault(glyph,sequence)
            if len(sequence)>1:
                mapping[sequence.replace('\ufe0f','')]=glyph
custom={}
mapping_out={}
for index,(sequence,glyph) in enumerate(sorted(mapping.items())):
    cp=0xF0000+index
    custom[cp]=glyph
    mapping_out[sequence]=chr(cp)
for table in font['cmap'].tables:
    if table.format==12:
        table.cmap.update(custom)
font.save(root/'EmojiUnity.ttf')
(root/'emoji-sequences.json').write_text(json.dumps(mapping_out,ensure_ascii=False),encoding='utf8')
print(json.dumps({'ligatures':len(ligatures),'mapped':len(mapping_out),'examples':{s:mapping_out.get(s.replace('\ufe0f','')) for s in ['👨‍💻','👮‍♂️','👨‍🍳','👨‍🚒','👩‍🔬']},'lookupTypes':[l.LookupType for l in lookups]},ensure_ascii=False))
