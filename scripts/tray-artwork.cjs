// Same circular C and rounded terminal strokes as the application icon.
// Hint each native size separately; omit the tile/ring at taskbar scale.
const fs = require('node:fs'), path = require('node:path'), sharp = require('sharp');
const sizes = [16, 20, 24, 28, 32, 36, 40, 48, 64, 128, 256];
function svg(size, light) {
  const color = light ? '#35383F' : '#F2F3F5';
  const stroke = size === 24 ? 3.5 : Math.max(3, Math.round(size / 8));
  const middle = size / 2;
  const radius = middle - Math.max(1, Math.round(size / 24)) - stroke / 2;
  const dx = Math.round(radius / Math.sqrt(2) * 2) / 2;
  const arrowX = Math.round(size * .37 * 2)/2, arrowRight = Math.round(size * .5 * 2)/2;
  const arrowY = Math.round(size * .38 * 2)/2, bottom = size-arrowY;
  const promptStroke = Math.max(2, Math.round(size / 12));
  const underlineX = Math.round(size * .61 * 2)/2, underlineRight = Math.round(size * .78 * 2)/2;
  const underlineY = size === 16 ? 9.5 : bottom;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">
    <path d="M${middle+dx} ${middle-dx} A${radius} ${radius} 0 1 0 ${middle+dx} ${middle+dx}" fill="none" stroke="${color}" stroke-width="${stroke}" stroke-linecap="round"/>
    <path d="M${arrowX} ${arrowY} L${arrowRight} ${middle} L${arrowX} ${bottom} M${underlineX} ${underlineY} H${underlineRight}" fill="none" stroke="${color}" stroke-width="${promptStroke}" stroke-linecap="round" stroke-linejoin="round"/>
  </svg>`;
}
async function png(size, light) { return sharp(Buffer.from(svg(size, light))).png().toBuffer(); }
async function frame(size, light) {
  const bytes = fs.readFileSync(path.join(__dirname, '..', 'assets', light ? 'tray-light.ico' : 'tray.ico'));
  for (let i=0; i<bytes.readUInt16LE(4); i++) {
    const entry=6+i*16;
    if ((bytes[entry] || 256) === size) return bytes.subarray(bytes.readUInt32LE(entry+12), bytes.readUInt32LE(entry+12)+bytes.readUInt32LE(entry+8));
  }
  throw Error('Missing ICO frame '+size);
}
module.exports = { sizes, svg, png, frame };
