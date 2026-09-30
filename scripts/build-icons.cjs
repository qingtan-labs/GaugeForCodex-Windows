// App SVG and per-size tray geometry are the editable masters. No AI or image retouching.
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
const assets = path.join(__dirname, '..', 'assets');
const tray = require('./tray-artwork.cjs');
async function ico(source, filename, sizes = [16, 20, 24, 32, 48, 64, 128, 256], render) {
  const pngs = await Promise.all(sizes.map(n => render ? render(n) : sharp(source).resize(n, n).png().toBuffer()));
  const header = Buffer.alloc(6); header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
  let offset = 6 + sizes.length * 16;
  const entries = pngs.map((png, i) => {
    const entry = Buffer.alloc(16), size = sizes[i];
    entry[0] = size === 256 ? 0 : size; entry[1] = entry[0];
    entry.writeUInt16LE(1, 4); entry.writeUInt16LE(32, 6);
    entry.writeUInt32LE(png.length, 8); entry.writeUInt32LE(offset, 12); offset += png.length;
    return entry;
  });
  fs.writeFileSync(path.join(assets, filename), Buffer.concat([header, ...entries, ...pngs]));
}
(async () => {
  fs.writeFileSync(path.join(assets, 'tray.svg'), tray.svg(32, false));
  fs.writeFileSync(path.join(assets, 'tray-light.svg'), tray.svg(32, true));
  await ico(path.join(assets, 'icon.svg'), 'app.ico');
  await ico(null, 'tray.ico', tray.sizes, n => tray.png(n, false));
  await ico(null, 'tray-light.ico', tray.sizes, n => tray.png(n, true));
  await sharp(path.join(assets, 'icon.svg')).resize(512,512).png().toFile(path.join(assets,'app-icon.png'));
  console.log('Built application icon (8 sizes) and pixel-hinted tray icons (11 sizes, transparent RGBA).');
})().catch(e => { console.error(e.message); process.exitCode = 1; });
