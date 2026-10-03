"""Regenerate original illustrated sticker PNGs; requires cairosvg (build-time only)."""
import json, math
from pathlib import Path
import cairosvg
root=Path(__file__).parent/'Stickers';root.mkdir(exist_ok=True)
items=[]
def add(name,category,body):
 key=name.lower().replace(' ','-')
 if any(item['key']==key for item in items):return
 svg='<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128" viewBox="0 0 128 128"><defs><linearGradient id="gold" x2="0" y2="1"><stop stop-color="#ffe677"/><stop offset="1" stop-color="#ffbf25"/></linearGradient><linearGradient id="red" x2="0" y2="1"><stop stop-color="#ff6b67"/><stop offset="1" stop-color="#eb283a"/></linearGradient><linearGradient id="green" x2="0" y2="1"><stop stop-color="#55d889"/><stop offset="1" stop-color="#159c59"/></linearGradient></defs>'+body+'</svg>'
 (root/(key+'.svg')).write_text(svg)
 cairosvg.svg2png(bytestring=svg.encode(),write_to=str(root/(key+'.png')),output_width=192,output_height=192)
 items.append(dict(key=key,name=name,category=category))
face='<circle cx="64" cy="64" r="53" fill="url(#gold)" stroke="#d99419" stroke-width="2"/><ellipse cx="43" cy="28" rx="18" ry="8" fill="#fff" opacity=".23"/>'
eyes='<ellipse cx="45" cy="53" rx="6" ry="9" fill="#202333"/><ellipse cx="83" cy="53" rx="6" ry="9" fill="#202333"/>'
smile='<path d="M39 76 Q64 105 89 76" fill="none" stroke="#242535" stroke-width="7" stroke-linecap="round"/>'
heart='<path d="M64 108 C52 95 12 69 15 41 C19 9 50 13 64 33 C78 13 109 9 113 41 C116 69 76 95 64 108Z" fill="url(#red)" stroke="#cc2431" stroke-width="2"/><path d="M25 40 Q27 24 44 27" fill="none" stroke="#fff" stroke-width="7" stroke-linecap="round" opacity=".5"/>'
add('Smile','Smileys',face+eyes+smile)
add('Grin','Smileys',face+eyes+'<path d="M33 73H95Q91 105 64 105Q37 105 33 73Z" fill="#292934"/><path d="M37 76H91V86H37Z" fill="white"/>')
add('Wink','Smileys',face+'<path d="M34 52Q44 43 54 52" stroke="#252535" stroke-width="6" fill="none" stroke-linecap="round"/><ellipse cx="83" cy="53" rx="6" ry="9" fill="#202333"/>'+smile)
add('Heart eyes','Smileys',face+'<g transform="translate(18 29) scale(.34)">'+heart+'</g><g transform="translate(66 29) scale(.34)">'+heart+'</g>'+smile)
add('Cool sunglasses','Smileys',face+'<path d="M24 42H59L55 64Q34 73 27 60Z M69 42H104L101 60Q91 73 73 64Z" fill="#242c46"/><path d="M59 48H69" stroke="#242c46" stroke-width="7"/>'+smile)
for name,mouth in [('Sad','<path d="M42 91Q64 70 86 91" fill="none" stroke="#252535" stroke-width="7" stroke-linecap="round"/>'),('Surprised','<ellipse cx="64" cy="87" rx="11" ry="15" fill="#252535"/>'),('Tongue out','<path d="M38 73Q64 110 90 73Z" fill="#252535"/><path d="M60 84H81V99Q72 115 61 99Z" fill="#ff6598"/>'),('Happy','<path d="M33 72Q64 118 95 72Z" fill="#252535"/><path d="M36 76H92L86 85H42Z" fill="white"/>')]:add(name,'Smileys',face+eyes+mouth)
add('Laugh tears','Smileys',face+'<path d="M33 53Q45 37 57 53M71 53Q83 37 95 53" stroke="#242535" stroke-width="6" fill="none"/>'+smile+'<path d="M27 52Q10 68 23 75Q41 76 27 52Z M101 52Q118 68 105 75Q87 76 101 52Z" fill="#57bffc"/>')
add('Heart','Icons',heart)
star=lambda color:'<polygon points="64,9 80,44 119,49 90,77 97,116 64,97 31,116 38,77 9,49 48,44" fill="'+color+'" stroke="#d4901b" stroke-width="3" stroke-linejoin="round"/>'
add('Gold star','Icons',star('url(#gold)'))
add('Check','Icons','<circle cx="64" cy="64" r="49" fill="url(#green)" stroke="#098448" stroke-width="2"/><path d="M35 65L56 85L93 42" fill="none" stroke="white" stroke-width="11" stroke-linecap="round" stroke-linejoin="round"/>')
add('Cross','Icons','<path d="M30 28L100 98M98 28L28 98" fill="none" stroke="#e93e46" stroke-width="18" stroke-linecap="round"/>')
add('Thumb up','Icons','<path d="M38 60L54 47L62 15Q78 10 80 25L75 51H103Q117 54 113 67L105 101Q103 112 88 112H46L38 102Z" fill="url(#gold)" stroke="#d69218" stroke-width="3"/><rect x="17" y="58" width="25" height="55" rx="6" fill="#ffbf31" stroke="#d69218" stroke-width="3"/><path d="M79 66H105M76 82H103M74 98H99" stroke="#e0a123" stroke-width="3"/>')
add('Arrow right','Icons','<path d="M12 48H69V25L115 64L69 103V79H12Z" fill="url(#green)" stroke="#17804b" stroke-width="3" stroke-linejoin="round"/>')
add('Information','Icons','<circle cx="64" cy="64" r="50" fill="#2e66db"/><circle cx="64" cy="34" r="7" fill="white"/><path d="M64 53V92" stroke="white" stroke-width="12" stroke-linecap="round"/>')
add('Question','Icons','<circle cx="64" cy="64" r="50" fill="#2e66db"/><path d="M44 44Q45 23 66 24Q89 24 87 44Q86 55 64 66V76" fill="none" stroke="white" stroke-width="10" stroke-linecap="round"/><circle cx="64" cy="94" r="5" fill="white"/>')
add('Speech bubble','Icons','<path d="M28 19H100Q113 19 113 33V80Q113 93 100 93H59L31 112V93H28Q15 93 15 80V33Q15 19 28 19Z" fill="#e8f3ff" stroke="#4677e7" stroke-width="6"/>')
add('Warning','Icons','<path d="M64 12L119 110H9Z" fill="url(#gold)" stroke="#bc7d10" stroke-width="3" stroke-linejoin="round"/><path d="M64 42V75" stroke="#343440" stroke-width="10" stroke-linecap="round"/><circle cx="64" cy="94" r="5" fill="#343440"/>')
add('Flag','Icons','<path d="M29 20V113" stroke="#344664" stroke-width="7" stroke-linecap="round"/><path d="M31 20Q61 5 94 21V68Q61 51 31 68Z" fill="url(#red)"/>')
add('Fire','Objects','<path d="M69 8Q107 36 97 52Q117 56 108 86Q99 119 64 119Q23 119 19 88Q16 67 35 44Q34 66 48 66Q68 50 69 8Z" fill="#f45027"/><path d="M68 42Q88 64 82 79Q102 87 87 104Q65 123 46 108Q29 95 44 76Q45 91 53 89Q68 76 68 42Z" fill="#ffd346"/>')
add('Light bulb','Objects','<path d="M44 87Q17 51 40 24Q64 5 88 24Q111 51 84 87Z" fill="url(#gold)" stroke="#dcad22" stroke-width="3"/><path d="M54 78L48 50M74 78L80 50M48 50L64 61L80 50" fill="none" stroke="#bc8a21" stroke-width="3"/><path d="M45 87H83V102H45Z" fill="#8694aa"/><path d="M50 109H78M55 116H73" stroke="#566579" stroke-width="6"/>')
add('Push pin','Objects','<path d="M64 69L24 115" stroke="#707a8a" stroke-width="5"/><path d="M54 15L108 53L97 64L86 59L69 83L42 64L58 42L45 28Z" fill="url(#red)" stroke="#c32434" stroke-width="3"/>')
add('Target','Objects','<circle cx="64" cy="64" r="49" fill="#e64047"/><circle cx="64" cy="64" r="36" fill="white"/><circle cx="64" cy="64" r="23" fill="#e64047"/><circle cx="64" cy="64" r="10" fill="white"/><path d="M65 63L103 25" stroke="#178759" stroke-width="7"/><path d="M93 25V11L113 14L115 34H102Z" fill="#29b56d"/>')
add('Laptop','Objects','<rect x="21" y="19" width="86" height="63" rx="6" fill="#344461"/><rect x="27" y="25" width="74" height="50" rx="2" fill="#82c5f7"/><path d="M21 82H107L122 105Q123 114 113 114H15Q5 114 6 105Z" fill="#a1afc6" stroke="#546680" stroke-width="3"/><path d="M52 92H76L83 103H45Z" fill="#7387a4"/>')
add('Confetti','Objects','<path d="M16 113L38 42L85 90Z" fill="#ffcf45" stroke="#e49d2c" stroke-width="3"/><path d="M24 85L55 104M32 62L76 94" stroke="#ef5d92" stroke-width="9"/><path d="M50 42Q92 39 88 13M74 65Q109 60 113 36M59 62Q81 17 60 12" fill="none" stroke="#586fe6" stroke-width="6"/><g fill="#fb626c"><rect x="98" y="10" width="10" height="14" transform="rotate(20 98 10)"/><circle cx="107" cy="78" r="7"/><circle cx="36" cy="19" r="6"/></g><g fill="#32c693"><circle cx="109" cy="25" r="5"/><rect x="85" y="43" width="10" height="10"/></g>')
add('Gift','Objects','<rect x="17" y="49" width="94" height="64" rx="5" fill="#ff6371"/><rect x="12" y="40" width="104" height="22" rx="4" fill="#f24960"/><path d="M59 42V113" stroke="#ffd252" stroke-width="16"/><path d="M62 42Q11 35 35 15Q58 1 64 41Q68 1 94 16Q112 37 62 42Z" fill="none" stroke="#ffd252" stroke-width="9"/>')
for name,color in [('Blue heart','#4384ef'),('Green heart','#33b574'),('Purple heart','#a165d9'),('Pink heart','#fb78ad')]:add(name,'Icons',heart.replace('url(#red)',color))
# Original colour variants remain image assets, never font glyphs.
for kind in ('Heart','Star','Arrow','Badge'):
 for name,color in [('Coral','#ff6d68'),('Mint','#41cda4'),('Blue','#487af1'),('Purple','#9767df'),('Pink','#f074b6'),('Orange','#ffa349'),('Teal','#27b7ca'),('Lime','#a5ca37'),('Gold','#ffcf45'),('Navy','#3e507d'),('Rose','#d95b89'),('Violet','#6861ce'),('Emerald','#29aa6c'),('Sky','#76baf4'),('Peach','#ffb68d'),('Ruby','#dc3d58'),('Aqua','#62d4df'),('Lavender','#bba0e3'),('Amber','#eeb147'),('Ocean','#3c9aca'),('Cherry','#ea5d75'),('Forest','#35856d'),('Cobalt','#4367cf'),('Sunset','#ee8056')]:
  body=heart.replace('url(#red)',color) if kind=='Heart' else star(color) if kind=='Star' else '<path d="M12 48H69V25L115 64L69 103V79H12Z" fill="'+color+'" stroke="#ffffff" stroke-width="3"/>' if kind=='Arrow' else '<circle cx="64" cy="64" r="49" fill="'+color+'"/><path d="M35 65L56 85L93 42" fill="none" stroke="white" stroke-width="10" stroke-linecap="round"/>'
  add(name+' '+kind,'Icons',body)
from StickerVariety import expand
expand(add)
(root/'catalog.json').write_text(json.dumps(items,indent=2))
print('Generated',len(items),'illustrated stickers')
