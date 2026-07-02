// Dev tool: WCAG contrast audit of the current page. Walks visible text,
// composites effective backgrounds through transparent layers, returns
// elements below 4.5:1 (3:1 for large text). Run window.icsContrastAudit()
// from the browser console. Kept tiny; no effect unless invoked.
window.icsContrastAudit = function () {
    const parse = c => { const m = (c || '').match(/rgba?\(([\d.]+),\s*([\d.]+),\s*([\d.]+)(?:,\s*([\d.]+))?\)/); return m ? [+m[1], +m[2], +m[3], m[4] === undefined ? 1 : +m[4]] : null; };
    const lum = ([r, g, b]) => { const f = v => { v /= 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }; return .2126 * f(r) + .7152 * f(g) + .0722 * f(b); };
    const ratio = (a, b) => { const l1 = Math.max(lum(a), lum(b)), l2 = Math.min(lum(a), lum(b)); return (l1 + .05) / (l2 + .05); };
    const base = document.documentElement.getAttribute('data-theme') === 'dark' ? [18, 18, 18] : [1, 40, 95];
    function effBg(el) {
        const stack = [];
        for (let e = el; e && e.nodeType === 1; e = e.parentElement) {
            const c = parse(getComputedStyle(e).backgroundColor);
            if (c && c[3] > 0) { stack.push(c); if (c[3] >= 1) break; }
        }
        let bg = base.slice();
        for (let i = stack.length - 1; i >= 0; i--) {
            const [r, g, b, a] = stack[i];
            bg = [r * a + bg[0] * (1 - a), g * a + bg[1] * (1 - a), b * a + bg[2] * (1 - a)];
        }
        return bg;
    }
    const bad = [];
    document.querySelectorAll('body *').forEach(el => {
        const hasText = Array.from(el.childNodes).some(n => n.nodeType === 3 && n.textContent.trim().length > 1);
        if (!hasText) return;
        const cs = getComputedStyle(el);
        if (cs.visibility === 'hidden' || cs.display === 'none' || +cs.opacity === 0) return;
        const rect = el.getBoundingClientRect();
        if (rect.width === 0 || rect.height === 0) return;
        const fg = parse(cs.color); if (!fg || fg[3] === 0) return;
        const r = ratio(fg, effBg(el));
        const px = parseFloat(cs.fontSize);
        const bold = +cs.fontWeight >= 600;
        const min = (px >= 24 || (px >= 18.5 && bold)) ? 3 : 4.5;
        if (r < min) bad.push({ text: el.textContent.trim().slice(0, 40), cls: (el.className || '').toString().slice(0, 45), ratio: +r.toFixed(2), min });
    });
    bad.sort((a, b) => a.ratio - b.ratio);
    return { theme: document.documentElement.getAttribute('data-theme'), url: location.pathname, failures: bad.length, worst: bad.slice(0, 8) };
};
