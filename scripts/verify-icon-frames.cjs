const fs = require('node:fs'), path = require('node:path'), sharp = require('sharp');
const tray = require('./tray-artwork.cjs');
const assets = path.join(__dirname, '..', 'assets');
(async () => {
  let count = 0;
  for (const name of ['app.ico', 'tray.ico', 'tray-light.ico']) {
    const bytes = fs.readFileSync(path.join(assets, name));
    const expected = name === 'app.ico' ? [16,20,24,32,48,64,128,256] : tray.sizes;
    if(bytes.readUInt16LE(0)!==0 || bytes.readUInt16LE(2)!==1 || bytes.readUInt16LE(4)!==expected.length) throw Error('Invalid ICO header '+name);
    for(let i=0; i<expected.length; i++) {
      const entry=6+i*16, n=expected[i];
      const length=bytes.readUInt32LE(entry+8), offset=bytes.readUInt32LE(entry+12);
      if((bytes[entry] || 256)!==n || (bytes[entry+1] || 256)!==n || offset+length>bytes.length) throw Error('ICO directory mismatch '+name+' '+n);
      const frame=bytes.subarray(offset,offset+length);
      const meta=await sharp(frame).metadata();
      if(meta.width!==n || meta.height!==n || !meta.hasAlpha) throw Error('Wrong frame dimensions/alpha '+name+' '+n);
      const raw=await sharp(frame).ensureAlpha().raw().toBuffer();
      let min=255,max=0;
      for(let a=3;a<raw.length;a+=4){min=Math.min(min,raw[a]);max=Math.max(max,raw[a]);}
      if(min!==0 || max!==255) throw Error('Icon must include fully transparent and opaque pixels '+name+' '+n);
      if(name!=='app.ico' && !frame.equals(await tray.png(n,name==='tray-light.ico'))) throw Error('ICO frame differs from pixel master '+name+' '+n);
      count++;
    }
  }
  console.log('PASS: '+count+' icon frames; dimensions, RGBA transparency and exact tray masters verified.');
})().catch(error=>{console.error(error);process.exitCode=1;});
