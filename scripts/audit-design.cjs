const fs=require('node:fs'), path=require('node:path'), sharp=require('sharp');
const tray=require('./tray-artwork.cjs');
const root=path.join(__dirname,'..'), assets=path.join(root,'assets'), out=path.join(assets,'screenshots');
const luminance=hex=>{const v=hex.replace('#','').match(/../g).map(s=>parseInt(s,16)/255).map(n=>n<=.04045?n/12.92:Math.pow((n+.055)/1.055,2.4));return v[0]*.2126+v[1]*.7152+v[2]*.0722;};
const contrast=(a,b)=>{const x=luminance(a),y=luminance(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);};
const sources=['src/Ui.cs','src/QuotaWindow.xaml'].map(p=>fs.readFileSync(path.join(root,p),'utf8'));
const inks=new Set();
for(const c of sources) {
  for(const m of c.matchAll(/Foreground="(#[0-9A-Fa-f]{6})"/g)) inks.add(m[1]);
  for(const m of c.matchAll(/(?:Label\([^;\n]+?|color\s*=\s*)"(#[0-9A-Fa-f]{6})"/g)) inks.add(m[1]);
}
for(const ink of inks){const ratio=contrast(ink,'#F0F2EC'); if(ratio<4.5)throw Error('Insufficient normal text contrast '+ink+': '+ratio.toFixed(2));}
if(contrast('#35383F','#F3F3F3')<7 || contrast('#F2F3F5','#202020')<7)throw Error('Tray contrast regression');
console.log('PASS: '+inks.size+' text inks >=4.5:1 against the matte preview surface; light/dark tray >=7:1. Desktop colors may vary with system blur.');
// Prove the widget's contrast floor, not just contrast on a pale sample matte.
// An opaque foreground over a >=56% neutral scrim is dimmest over RGB black.
const widgetSource=fs.readFileSync(path.join(root,'src/QuotaWindow.xaml'),'utf8');
const widgetInks=new Set([...widgetSource.matchAll(/Foreground="(#[0-9A-Fa-f]{6})"/g)].map(m=>m[1]));
widgetInks.add('#12251A'); // glass-specific additional-period rows
const worstSurface='#'+[248,249,247].map(v=>Math.floor(v*Math.floor(.56*255)/255).toString(16).padStart(2,'0')).join('');
for(const ink of widgetInks){const ratio=contrast(ink,worstSurface);if(ratio<4.5)throw Error('Insufficient widget contrast over black: '+ink+' '+ratio.toFixed(2));}
console.log('PASS: '+widgetInks.size+' widget text inks >=4.5:1 at the minimum scrim over black; any brighter RGB backdrop increases contrast.');
(async()=>{
  fs.mkdirSync(out,{recursive:true});
  const layers=[], W=920,H=280;
  layers.push({input:Buffer.from('<svg width="'+W+'" height="'+H+'"><rect width="'+W+'" height="'+H+'" rx="24" fill="#F4F5F1"/><text x="28" y="37" font-family="Segoe UI" font-size="18" fill="#313A33">Gauge for Codex · Windows / Icon scale audit</text><rect x="24" y="60" width="424" height="190" rx="16" fill="#E6E9E1"/><rect x="464" y="60" width="432" height="190" rx="16" fill="#25292E"/><text x="42" y="91" font-family="Segoe UI" font-size="13" fill="#434D43">LIGHT TASKBAR</text><text x="482" y="91" font-family="Segoe UI" font-size="13" fill="#DDE1DB">DARK TASKBAR</text></svg>')});
  for(const [column,name] of [['light','tray-light.svg'],['dark','tray.svg']]) {
    for(const [i,n] of [16,20,24,32].entries()){
      const x=(column==='light'?58:498)+i*93;
      const exact=await tray.frame(n,column==='light');
      const raw=await sharp(exact).ensureAlpha().raw().toBuffer();
      let opaque=0, transparent=0;
      for(let a=3;a<raw.length;a+=4){if(raw[a]===255)opaque++;if(raw[a]===0)transparent++;}
      if(opaque<n*n*.18 || transparent<n*n*.3)throw Error('Tray frame too faint or missing transparency: '+n);
      layers.push({input:exact,left:x+24-Math.floor(n/2),top:129});
      layers.push({input:await sharp(exact).resize(n*2,n*2,{kernel:'nearest'}).png().toBuffer(),left:x+24-n,top:178});
    }
  }
  await sharp({create:{width:W,height:H,channels:4,background:'#F4F5F1'}}).composite(layers).png().toFile(path.join(out,'tray-icon-preview.png'));
  const iconLayers=[{input:Buffer.from('<svg width="760" height="220"><rect width="760" height="220" rx="24" fill="#F4F5F1"/><text x="28" y="36" font-family="Segoe UI" font-size="18" fill="#313A33">Application icon · 16 / 32 / 48 / 64 / 128 px</text></svg>')}];
  for(const [i,n] of [16,32,48,64,128].entries()) iconLayers.push({input:await sharp(path.join(assets,'icon.svg')).resize(n,n).png().toBuffer(),left:48+i*134+(128-n)/2,top:70+(128-n)/2});
  await sharp({create:{width:760,height:220,channels:4,background:'#F4F5F1'}}).composite(iconLayers).png().toFile(path.join(out,'app-icon-sizes.png'));
  console.log('Generated icon scale galleries from exact SVG masters.');
})().catch(e=>{console.error(e);process.exitCode=1;});
