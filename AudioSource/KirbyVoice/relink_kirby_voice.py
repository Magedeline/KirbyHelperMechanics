"""
Gives event:/char/dialogue/Celestellaris/kirby Kirby's own voice.

That event is a copy of Madeline's dialogue event (a sectioned timeline driven
by dialogue_portrait / dialogue_end, 401 instruments). This points every
instrument at the take with the same name in assets_dir
(char_dia_madeline_X or char_dia_kirby_X -> <assets_dir>/char_dia_kirby_X.wav,
made by generate_voice.py), keeping each instrument's position, so all of the
event's logic stays as it is. Instruments already on that exact file are
skipped, so it is safe to run again.

Instrument length: the new take's length, capped at the slot's original length.
Pass --orig-lengths <backup event .xml> (a copy of the event's Metadata/Event
file from before any relink) so a re-voice isn't capped by the previous takes;
without it the current length is the cap.

It also adds the event to kirbyhelper_sfx, so Kirby Helper Mechanics ships it.

FMOD Studio 1.10 must be open with D:/pusheen-fmod-project/celeste_audio.fspro
(scripting port 3663). Saves the project; does NOT build banks.

Usage: python relink_kirby_voice.py [assets_dir] [--orig-lengths event_backup.xml]
"""
import json
import os
import re
import socket
import sys
import tempfile
import time
import uuid
import xml.etree.ElementTree as ET

PORT = 3663
EVENT = "event:/char/dialogue/Celestellaris/kirby"
BANK = "kirbyhelper_sfx"
DEFAULT_ASSETS = "D:/pusheen-fmod-project/Assets/character/dialogue/kirby"

JS = r"""
var f = studio.system.getFile("__OUT__"); f.open(studio.system.openMode.WriteOnly);
function w(s){ f.writeText(s + "\n"); }
try {
  var ev = studio.project.lookup("__EVENT__");
  if (!ev) throw "event __EVENT__ not found";
  var bank = studio.project.lookup("bank:/__BANK__");
  if (!bank) throw "bank __BANK__ not found";

  var origLen = __ORIG__;
  var relinked = 0, skipped = 0, shortened = 0, missing = [];
  for (var t = 0; t < ev.groupTracks.length; t++) {
    var mods = ev.groupTracks[t].modules;
    for (var m = 0; m < mods.length; m++) {
      var snd = mods[m];
      if (snd.entity != "SingleSound" || !snd.audioFile) continue;
      var ap = snd.audioFile.assetPath || "";
      var base = ap.substring(ap.lastIndexOf("/") + 1).replace(/\.[^.]+$/, "");
      var mm = /^char_dia_(madeline|kirby)_(.+)$/.exec(base);
      if (!mm) { missing.push(ap + " (not a voice take)"); continue; }
      var target = "__ASSETS__/char_dia_kirby_" + mm[2] + ".wav";
      if (target.indexOf("/" + ap) == target.length - ap.length - 1) { skipped++; continue; }
      var af = studio.project.importAudioFile(target);
      if (!af || !af.isValid) { missing.push(target); continue; }
      var cap = origLen[snd.id] || snd.length;
      snd.audioFile = af;
      snd.length = Math.min(cap, af.length);
      if (af.length < cap) shortened++;
      relinked++;
    }
  }

  var inBank = false, banks = ev.relationships.banks.destinations;
  for (var b = 0; b < banks.length; b++) if (banks[b].name == "__BANK__") inBank = true;
  if (!inBank) ev.relationships.banks.add(bank);

  studio.project.save();
  w("relinked " + relinked + " instruments (" + shortened + " shortened), skipped " + skipped + " already Kirby");
  w("banks: " + ev.relationships.banks.destinations.map(function(x){ return x.name; }).join(", "));
  if (missing.length) w("NOT relinked (" + missing.length + "): " + missing.slice(0, 10).join("; "));
  w("project saved. Build banks to hear it in game.");
  w("DONE");
} catch (e) { w("ERROR " + e); }
f.close();
"""


def run(js, out_path, timeout=900.0):
    s = socket.create_connection(("127.0.0.1", PORT), timeout=10)
    s.sendall(js.encode("utf-8") + b"\0")
    s.settimeout(0.5)
    end, txt = time.time() + timeout, ""
    while time.time() < end:
        try:
            s.recv(65536)
        except (socket.timeout, OSError):
            pass
        if os.path.exists(out_path):
            txt = open(out_path, encoding="utf-8", errors="replace").read()
            if "DONE" in txt or "ERROR" in txt:
                break
    s.close()
    return txt.strip() or "(FMOD wrote nothing before the timeout; check Studio's console)"


def original_lengths(xml_path):
    """SingleSound id -> length from a backed-up Metadata/Event/<id>.xml."""
    lengths = {}
    for obj in ET.parse(xml_path).getroot().iter("object"):
        if obj.get("class") != "SingleSound":
            continue
        for prop in obj.findall("property"):
            if prop.get("name") == "length":
                lengths[obj.get("id")] = float(prop.find("value").text)
    return lengths


def main():
    args = sys.argv[1:]
    orig = {}
    if "--orig-lengths" in args:
        i = args.index("--orig-lengths")
        orig = original_lengths(args[i + 1])
        del args[i:i + 2]
    assets = (args[0] if args else DEFAULT_ASSETS).replace("\\", "/").rstrip("/")
    if not os.path.isdir(assets):
        sys.exit(f"{assets} not found (run generate_voice.py first)")
    out = os.path.join(tempfile.gettempdir(), f"khm_fmod_{uuid.uuid4().hex[:6]}.txt").replace("\\", "/")
    js = (JS.replace("__OUT__", out).replace("__EVENT__", EVENT).replace("__BANK__", BANK)
            .replace("__ASSETS__", assets).replace("__ORIG__", json.dumps(orig)))
    try:
        print(run(js, out))
    except OSError as e:
        sys.exit(f"FMOD Studio not reachable on port {PORT}: {e}\nOpen FMOD Studio 1.10 with the project first.")
    finally:
        if os.path.exists(out):
            os.remove(out)


if __name__ == "__main__":
    main()
