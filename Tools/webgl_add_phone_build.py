#!/usr/bin/env python3
"""Merge the ASTC (phone) WebGL build into the DXT (desktop) build.

Used by .github/workflows/deploy-webgl.yml after both Unity builds finish:

    python3 Tools/webgl_add_phone_build.py <desktop build dir> <astc build dir>

What it does, following Unity's dual-build pattern (Manual > WebGL texture compression):
  1. Copies the ASTC build's .data file into the desktop build's Build/ folder as
     <name>.astc.data.unityweb. The code files are the same in both builds, so only
     the .data file (which holds the textures) is needed.
  2. Patches the desktop index.html so it loads the ASTC data file when the browser
     supports WEBGL_compressed_texture_astc (phones, tablets, Apple-silicon Macs),
     and the normal DXT data file otherwise.

For testing, add ?textures=astc or ?textures=dxt to the page URL to force one.
Exits non-zero, changing nothing, if the files or the line it patches aren't found,
or if the phone build has no ASTC textures (needs UnityPy: pip install UnityPy).
"""
import gzip
import pathlib
import re
import shutil
import sys

MARKER = "/* MIHLab: pick texture format */"

PICKER = """      {marker}
      // Phones need ASTC textures; desktops need DXT. A browser given textures it
      // can't read unpacks them to full size in memory, which phones can't afford.
      var textureFormat = (function () {{
        var forced = new URLSearchParams(location.search).get("textures");
        if (forced === "astc" || forced === "dxt") return forced;
        try {{
          var c = document.createElement("canvas");
          var gl = c.getContext("webgl2") || c.getContext("webgl");
          if (gl && gl.getExtension("WEBGL_compressed_texture_astc")) return "astc";
        }} catch (e) {{}}
        return "dxt";
      }})();
      console.log("[MIHLab] Loading " + textureFormat.toUpperCase() + " textures");
"""


def fail(msg):
    print(f"::error::{msg}")
    sys.exit(1)


def texture_formats(path):
    """Count Texture2D formats in a WebGL .data file (needs: pip install UnityPy)."""
    import collections
    import tempfile
    import UnityPy
    import UnityPy.enums
    raw = path.read_bytes()
    if raw[:2] == b"\x1f\x8b":
        raw = gzip.decompress(raw)
    with tempfile.NamedTemporaryFile(suffix=".data") as tmp:
        tmp.write(raw)
        tmp.flush()
        counts = collections.Counter()
        for obj in UnityPy.load(tmp.name).objects:
            if obj.type.name == "Texture2D":
                fmt = obj.read().m_TextureFormat
                try:
                    fmt = UnityPy.enums.TextureFormat(int(fmt)).name
                except (ValueError, TypeError):
                    fmt = str(fmt).split(".")[-1]
                counts[fmt] += 1
    return counts


def check_astc(dxt, astc):
    """Refuse to publish if the 'phone' build doesn't actually contain ASTC textures.

    The first dual build shipped desktop (DXT) textures in both files because the
    ASTC setting was ignored; nothing failed, the phone file was just useless.
    """
    try:
        d, a = texture_formats(dxt), texture_formats(astc)
    except ImportError:
        fail("UnityPy is not installed (pip install UnityPy); can't verify the ASTC build.")
    print(f"Desktop data textures: {dict(d.most_common())}")
    print(f"Phone data textures:   {dict(a.most_common())}")
    n_astc = sum(v for k, v in a.items() if "ASTC" in k)
    if n_astc == 0:
        fail("The phone build's data file has no ASTC textures, so it didn't switch "
             "formats. Not publishing. Check the '[WebGLBuild] Textures ASTC' line in "
             "the ASTC build log.")
    print(f"OK: {n_astc} ASTC textures in the phone build.")


def main():
    if len(sys.argv) != 3:
        fail("usage: webgl_add_phone_build.py <desktop build dir> <astc build dir>")
    desktop, astc = (pathlib.Path(a) for a in sys.argv[1:])
    index = desktop / "index.html"
    if not index.is_file():
        fail(f"{index} not found")

    html = index.read_text(encoding="utf-8")
    if MARKER in html:
        print("index.html already patched; nothing to do.")
        return

    m = re.search(r'dataUrl:\s*buildUrl\s*\+\s*"/([^"]+)\.data(\.unityweb|\.gz|\.br)?"', html)
    if not m:
        fail("Could not find the dataUrl line in index.html (has the WebGL template changed?)")
    name, ext = m.group(1), m.group(2) or ""

    src = astc / "Build" / f"{name}.data{ext}"
    if not src.is_file():
        fail(f"ASTC data file {src} not found")
    check_astc(desktop / "Build" / f"{name}.data{ext}", src)
    dst = desktop / "Build" / f"{name}.astc.data{ext}"
    shutil.copyfile(src, dst)

    new_data = (f'dataUrl: buildUrl + (textureFormat === "astc" ? '
                f'"/{name}.astc.data{ext}" : "/{name}.data{ext}")')
    html = html[:m.start()] + new_data + html[m.end():]

    cfg = re.search(r"^(\s*)var config = \{", html, re.M)
    if not cfg:
        fail("Could not find 'var config = {' in index.html")
    html = html[:cfg.start()] + PICKER.format(marker=MARKER) + html[cfg.start():]

    index.write_text(html, encoding="utf-8")
    mb = lambda p: p.stat().st_size / 1048576
    print(f"Added {dst.name} ({mb(dst):.1f} MB) next to {name}.data{ext} "
          f"({mb(desktop / 'Build' / f'{name}.data{ext}'):.1f} MB) and patched index.html.")


if __name__ == "__main__":
    main()
