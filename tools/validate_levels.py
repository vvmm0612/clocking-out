#!/usr/bin/env python3
"""Reference validator for Clocking Out level files.

Independent re-implementation of the simulator in design.md 5.1 (the C# one lives
in Assets/Scripts/Core/Simulator.cs). Standard library only.

Usage:
    python tools/validate_levels.py [--levels DIR] [--combos OUT.md] [--quiet]

Exit code 0 when every level passes, 1 otherwise.

Checks per level:
  (a) set of approved combinations == declared solutions
  (b) every rejected combination has a non-empty verdict
  (c) every rule fires for at least one combination, unless marked "unreachable": true
      (and a rule marked unreachable must really never fire)
  (d) every anchor referenced by beats / clues / show: facts is listed in "anchors"
  plus structural checks (ids unique, beat ops known, states match shift options,
  solutions well-formed) and (e) every combination that ends in a death (not stopped
  by a reject rule) plays a ghost_to and a die_<cause> beat.
"""
import argparse
import glob
import json
import os
import sys

BEAT_OPS = {"ghost_to", "die_burn", "die_drown", "die_fall", "fx", "move", "rotate",
            "toggle", "light", "npc_to", "sfx", "wait", "shake"}
# Ops whose `target` must be an anchor (or the placeholder "here").
TARGET_OPS = {"ghost_to", "die_burn", "die_drown", "die_fall", "fx", "move", "rotate",
              "toggle", "light", "npc_to"}
# Ops whose `arg` is also an anchor.
ARG_ANCHOR_OPS = {"move", "npc_to"}
FX_KINDS = {"fire", "water", "smoke", "sparks", "splash"}
HERE = "here"
DEATH_CAUSES = {"burn", "drown", "fall"}


# ----------------------------------------------------------------- simulator

def base_facts(level, choices):
    facts = set()
    for field_id, option_id in choices.items():
        facts.add(field_id + ":" + option_id)
    facts.update(level.get("always", []))
    shift = choices.get("shift")
    if shift is not None:
        for st in level.get("states", []):
            if st.get("shift") == shift:
                facts.update(st.get("facts", []))
    place = choices.get("place")
    if place is not None:
        suffix = "@" + place
        derived = set()
        for f in facts:
            if f.endswith(suffix) and len(f) > len(suffix):
                derived.add(f[: -len(suffix)] + "@" + HERE)
        facts |= derived
    return facts


def simulate(level, choices):
    facts = base_facts(level, choices)
    verdict = ""
    events = []
    rejected_by = None
    for rule in level.get("rules", []):
        if not all(w in facts for w in rule.get("when", [])):
            continue
        if any(u in facts for u in rule.get("unless", [])):
            continue
        events.append(rule)
        for a in rule.get("add", []):
            facts.add(a)
        for r in rule.get("remove", []):
            facts.discard(r)
        if rule.get("verdict"):
            verdict = rule["verdict"]
        if rule.get("reject"):
            rejected_by = rule["id"]
            break
    if rejected_by is None:
        wish = level.get("wish", {})
        ok = all(f in facts for f in wish.get("all", [])) and not any(f in facts for f in wish.get("none", []))
        if ok:
            return {"approved": True, "verdict": "", "events": events, "rejected_by": None, "facts": facts}
    if not verdict:
        verdict = level.get("defaultVerdict", "")
    return {"approved": False, "verdict": verdict, "events": events, "rejected_by": rejected_by, "facts": facts}


def enumerate_combos(level):
    combos = [dict()]
    for field in level["fields"]:
        nxt = []
        for c in combos:
            for opt in field["options"]:
                d = dict(c)
                d[field["id"]] = opt["id"]
                nxt.append(d)
        combos = nxt
    return combos


def combo_key(level, choices):
    return tuple(choices.get(f["id"], "") for f in level["fields"])


# ----------------------------------------------------------------- checks

def validate(level):
    errors = []
    lid = level.get("id", "?")

    def err(msg):
        errors.append(lid + ": " + msg)

    for key in ("id", "title", "deceased", "wishText", "fields", "rules", "wish", "defaultVerdict", "solutions", "anchors"):
        if key not in level:
            err("missing key '%s'" % key)
    if errors:
        return errors, []

    field_ids = [f["id"] for f in level["fields"]]
    if len(set(field_ids)) != len(field_ids):
        err("duplicate field ids")
    options = {}
    for f in level["fields"]:
        ids = [o["id"] for o in f["options"]]
        if len(set(ids)) != len(ids):
            err("duplicate option ids in field " + f["id"])
        if not ids:
            err("field %s has no options" % f["id"])
        options[f["id"]] = set(ids)
    if "place" not in options:
        err("no 'place' field")

    anchors = level["anchors"]
    anchor_set = set(anchors)
    if len(anchor_set) != len(anchors):
        err("duplicate anchors")
    for p in options.get("place", []):
        if p not in anchor_set:
            err("place '%s' is not listed in anchors" % p)

    # states
    shift_opts = options.get("shift")
    seen_shift = set()
    for st in level.get("states", []):
        s = st.get("shift")
        if shift_opts is None:
            err("states given but level has no shift field")
        elif s not in shift_opts:
            err("state for unknown shift '%s'" % s)
        if s in seen_shift:
            err("duplicate state for shift '%s'" % s)
        seen_shift.add(s)
        for fact in st.get("facts", []):
            if fact.startswith("show:"):
                body = fact[5:]
                if "@" in body:
                    who, where = body.split("@", 1)
                    for a in (who, where):
                        if a not in anchor_set:
                            err("show fact '%s' references unknown anchor '%s'" % (fact, a))
                elif "=" not in body:
                    err("show fact '%s' is neither who@where nor key=value" % fact)

    # show: facts must not be used by rules
    for rule in level["rules"]:
        for k in ("when", "unless", "add", "remove"):
            for fact in rule.get(k, []):
                if fact.startswith("show:"):
                    err("rule %s uses show: fact '%s'" % (rule["id"], fact))

    # clues
    for c in level.get("clues", []):
        if c.get("anchor") not in anchor_set:
            err("clue anchor '%s' not in anchors" % c.get("anchor"))
        if not c.get("text"):
            err("clue '%s' has empty text" % c.get("anchor"))

    # rules structure + beats
    rule_ids = [r["id"] for r in level["rules"]]
    if len(set(rule_ids)) != len(rule_ids):
        err("duplicate rule ids")
    for rule in level["rules"]:
        rid = rule["id"]
        if not rule.get("when"):
            err("rule %s has empty 'when'" % rid)
        if not rule.get("caption"):
            err("rule %s has empty caption" % rid)
        if rule.get("reject") and not rule.get("verdict"):
            err("reject rule %s has no verdict" % rid)
        beats = rule.get("beats", [])
        if not (2 <= len(beats) <= 5):
            err("rule %s has %d beats (expected 2-5)" % (rid, len(beats)))
        for b in beats:
            op = b.get("do")
            if op not in BEAT_OPS:
                err("rule %s: unknown beat op '%s'" % (rid, op))
                continue
            tgt = b.get("target", "")
            if op in TARGET_OPS:
                if tgt != HERE and tgt not in anchor_set:
                    err("rule %s: beat %s target '%s' not in anchors" % (rid, op, tgt))
            elif tgt and tgt != HERE and tgt not in anchor_set:
                err("rule %s: beat %s target '%s' not in anchors" % (rid, op, tgt))
            arg = b.get("arg", "")
            if op in ARG_ANCHOR_OPS and arg != HERE and arg not in anchor_set:
                err("rule %s: beat %s arg '%s' not in anchors" % (rid, op, arg))
            if op == "fx" and arg not in FX_KINDS:
                err("rule %s: fx kind '%s' unknown" % (rid, arg))
            if op in ("wait", "shake", "rotate"):
                try:
                    float(arg)
                except ValueError:
                    err("rule %s: beat %s arg '%s' is not a number" % (rid, op, arg))
            if op == "toggle" and arg not in ("on", "off"):
                err("rule %s: toggle arg '%s' must be on/off" % (rid, arg))
            if op == "sfx" and not arg:
                err("rule %s: sfx without arg" % rid)

    # solutions
    solution_keys = set()
    for s in level["solutions"]:
        ch = {}
        for c in s.get("choices", []):
            if c["field"] not in options:
                err("solution references unknown field '%s'" % c["field"])
            elif c["option"] not in options[c["field"]]:
                err("solution references unknown option '%s.%s'" % (c["field"], c["option"]))
            ch[c["field"]] = c["option"]
        if set(ch) != set(field_ids):
            err("solution %s does not set exactly the level's fields" % ch)
        k = combo_key(level, ch)
        if k in solution_keys:
            err("duplicate solution %s" % (k,))
        solution_keys.add(k)

    if errors:
        return errors, []

    # simulate everything
    rows = []
    fired = set()
    approved = set()
    for ch in enumerate_combos(level):
        res = simulate(level, ch)
        k = combo_key(level, ch)
        for e in res["events"]:
            fired.add(e["id"])
        if res["approved"]:
            approved.add(k)
        elif not res["verdict"]:
            err("(b) rejected combo %s has empty verdict" % (k,))
        # (e) deaths must be shown
        cause = ch.get("cause")
        if res["rejected_by"] is None and cause in DEATH_CAUSES:
            ops = [b["do"] for e in res["events"] for b in e.get("beats", [])]
            if "ghost_to" not in ops or ("die_" + cause) not in ops:
                err("(e) combo %s dies by %s but plays no ghost_to + die_%s beat" % (k, cause, cause))
        rows.append((ch, res))

    if approved != solution_keys:
        extra = sorted(approved - solution_keys)
        missing = sorted(solution_keys - approved)
        if extra:
            err("(a) approved but not declared as solution: %s" % extra)
        if missing:
            err("(a) declared solution not approved: %s" % missing)

    for rule in level["rules"]:
        if rule.get("unreachable"):
            if rule["id"] in fired:
                err("(c) rule %s is marked unreachable but fires" % rule["id"])
        elif rule["id"] not in fired:
            err("(c) rule %s never fires (mark it \"unreachable\": true if intended)" % rule["id"])

    return errors, rows


# ----------------------------------------------------------------- report

def md_escape(s):
    return s.replace("|", "\\|")


def write_combos(path, results):
    lines = ["# Combination results", "",
             "Generated by `tools/validate_levels.py`. Do not edit by hand.", "",
             "Columns: the player's choices, the outcome, the rules that fired (in order; the last one of a "
             "rejected row is the reject rule if it ends in `!`), and the verdict shown on REJECTED.", ""]
    for level, rows in results:
        fields = [f["id"] for f in level["fields"]]
        n_ok = sum(1 for _, r in rows if r["approved"])
        lines.append("## %s %s" % (level["id"], level["title"]))
        lines.append("")
        lines.append("Wish: \"%s\" -- %d combinations, %d approved." % (level["wishText"], len(rows), n_ok))
        lines.append("")
        lines.append("| " + " | ".join(fields) + " | result | rules fired | verdict |")
        lines.append("|" + "---|" * (len(fields) + 3))
        for ch, res in rows:
            chain = []
            for e in res["events"]:
                chain.append(e["id"] + ("!" if e.get("reject") else ""))
            result = "**APPROVED**" if res["approved"] else "rejected"
            lines.append("| " + " | ".join(ch[f] for f in fields) + " | " + result + " | " +
                         " > ".join(chain) + " | " + md_escape(res["verdict"]) + " |")
        unreachable = [r["id"] for r in level["rules"] if r.get("unreachable")]
        if unreachable:
            lines.append("")
            lines.append("Rules marked unreachable: " + ", ".join(unreachable))
        lines.append("")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))


def main():
    here = os.path.dirname(os.path.abspath(__file__))
    root = os.path.dirname(here)
    ap = argparse.ArgumentParser()
    ap.add_argument("--levels", default=os.path.join(root, "Assets", "Resources", "Levels"))
    ap.add_argument("--combos", default=None, help="write the per-level combination table to this .md file")
    ap.add_argument("--quiet", action="store_true", help="only print the per-level summary")
    args = ap.parse_args()

    files = sorted(glob.glob(os.path.join(args.levels, "level_*.json")))
    if not files:
        print("No level files found in " + args.levels)
        return 1

    all_ok = True
    results = []
    for path in files:
        with open(path, encoding="utf-8") as f:
            level = json.load(f)
        errors, rows = validate(level)
        results.append((level, rows))
        sols = [" + ".join(c["option"] for c in s["choices"]) for s in level.get("solutions", [])]
        status = "PASS" if not errors else "FAIL"
        all_ok = all_ok and not errors
        print("%s  %-8s %-18s combos=%-3d solutions=%d [%s]" % (
            status, level.get("id"), level.get("title"), len(rows), len(sols), "; ".join(sols)))
        if not args.quiet:
            for ch, res in rows:
                k = " + ".join(ch[f["id"]] for f in level["fields"])
                if res["approved"]:
                    print("      %-28s approved" % k)
                else:
                    print("      %-28s rejected  %s" % (k, res["verdict"]))
        for e in errors:
            print("    ERROR " + e)

    if args.combos:
        write_combos(args.combos, results)
        print("wrote " + args.combos)

    print("ALL PASS" if all_ok else "FAILURES")
    return 0 if all_ok else 1


if __name__ == "__main__":
    sys.exit(main())
