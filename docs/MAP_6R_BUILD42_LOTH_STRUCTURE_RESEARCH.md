# MAP-6R: Build 42 LOTH Structure Research

```text
Schema:           pzmapforge.map6r-loth-structure-research.v0.1
Claim boundary:   writer_research_only_not_implemented_not_load_tested
PZ build:         Build 42
Reference mod:    Dru_map (Drummondville) -- under .local/reference-build42-map/Dru_map
BUILD42_LOTH_STRUCTURE_INSPECTED
LOTH_REFERENCE_PREFIX_ANALYSED
CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED
WRITER_RESEARCH_ONLY
WRITER_NOT_CHANGED
LOAD_TEST_NOT_PERFORMED
PLAYABLE_EXPORT_CLAIM_ALLOWED=false
```

---

## 1. Why MAP-6R is needed

MAP-6Q confirmed that the MAP-6L LOTH lotheader (38 bytes) causes an
`IsoLot.readInt` EOF when PZ tries to load it. The MAP-6Q smoke comparison showed:

- Candidate LOTH: 38 bytes
- Smallest reference LOTH (Dru_map): 34920 bytes
- `candidate_smaller_than_all_references = true`

The candidate is roughly 918x smaller than the smallest known reference.
The failure at `IsoLot.readInt(IsoLot.java:75)` indicates PZ is attempting
to read a field that does not exist in the candidate because the file is
exhausted before PZ reaches the expected offset.

MAP-6R deepens the reference inspection to answer:
- What is at each byte offset in a Build 42 LOTH file?
- Are there additional fields between the 12-byte header and the string table?
- What is the minimum entry count required to not EOF?
- Where exactly does `IsoLot.readInt` at line 75 read from?

---

## 2. MAP-6K prior evidence

MAP-6K established from the Drummondville reference:

| Field | Finding |
|---|---|
| LOTH magic | `4C 54 5A 48` (LOTH) |
| version | U32 LE = 1 |
| entry_count (bytes 8-11) | variable; smallest observed = 36 |
| entry format | newline-delimited ASCII: `<pack>_<sprite_index>\n` |
| off-by-one | `parsed_count = declared_count + 1` consistently |
| trailing content | likely a non-printable section after the string table |

MAP-6L implemented a 1-entry LOTH using the MAP-4E confirmed format:
- LOTH magic + version=1 + entry_count=1 + one ASCII entry (total 38 bytes)

That 38-byte file was rejected by PZ at IsoLot.readInt.

---

## 3. What MAP-6R extracts

`scripts/inspect-build42-loth-structure.ps1` reads bounded prefixes of each
reference LOTH and extracts:

| Field | Purpose |
|---|---|
| `magic_ascii` | Confirm LOTH magic |
| `version_u32le` | Confirm version=1 |
| `field8_u32le` | The declared entry count (or another field) |
| `u32le_words_first_128` | 32 U32 words -- reveals field layout |
| `null_byte_count_in_prefix` | Binary density indicator |
| `newline_count_in_prefix` | String table density indicator |
| `first_printable_offset` | Where ASCII data begins |
| `first_newline_offset` | Where first entry ends |
| `ascii_lines_from_offset_12` | Lines parsed starting at string table start |
| `parsed_line_count` | How many entries were read from the prefix |
| `field8_matches_parsed_count` | Whether declared count matches parsed count |

Stable word summary across references reveals which fields are fixed
(magic, version) vs variable (entry_count, string table content).

---

## 4. Expected findings and writer implications

Based on MAP-6K evidence and MAP-6Q failure:

**Hypothesis A (most likely):** The LOTH string table has more entries than 1.
MAP-6K observed minimum 36 entries in 3 reference cells. PZ's `IsoLot.readInt`
at line 75 likely reads the `field8` (entry count) and then attempts to read
that many entries. If `field8=1` but PZ expects at least 36 entries (because
the cell has a non-trivial tile set), the read loop would not EOF -- but if PZ
reads tile-IDs or offsets per entry as additional U32 fields, even 1 entry
could run out of bytes.

**Hypothesis B (alternative):** There are additional binary fields after the
12-byte header and before the string table. If PZ reads a fixed-size section
of binary data before parsing the string table, the 38-byte candidate would
EOF during that binary section.

MAP-6R's word-level prefix extraction will confirm which hypothesis is correct
by showing whether bytes 12+ contain binary (non-printable) data before the
first ASCII entry.

---

## 5. How MAP-6R results guide MAP-6S

MAP-6S should be attempted only after MAP-6R provides evidence on:
1. Whether there is a binary section between the 12-byte header and the string table.
2. The minimum entry count required (confirmed minimum, not just MAP-6K minimum).
3. Whether each entry is just a bare ASCII string or has a preceding length/type field.

**If MAP-6R shows bytes 12+ are immediately ASCII entries:**
- MAP-6S should increase the entry count to the MAP-6K minimum (36+).
- Use the grass overlay tileset entries from MAP-4E/MAP-6K evidence.

**If MAP-6R shows binary fields before the first ASCII entry:**
- MAP-6S must decode that binary section before writing entries.
- Do not implement MAP-6S until the binary section format is understood.

---

## 6. Status labels

```text
BUILD42_LOTH_STRUCTURE_INSPECTED
  -- Reference LOTH files analysed with bounded prefix reader.

LOTH_REFERENCE_PREFIX_ANALYSED
  -- u32le word layout, ASCII run positions, and newline count captured.

CANDIDATE_LOTHEADER_TOO_SHORT_CONFIRMED
  -- MAP-6Q smoke: candidate 38 bytes vs min reference 34920 bytes.

WRITER_RESEARCH_ONLY
  -- This slice is inspection only. No writer changes.

WRITER_NOT_CHANGED
  -- The MAP-6L candidate binary files are unchanged.

LOAD_TEST_NOT_PERFORMED
  -- MAP-6R defines and runs the inspection only.
  -- No PZ load session was performed.

PLAYABLE_EXPORT_CLAIM_ALLOWED=false
  -- Binding until a LOAD_TEST_PASS record is committed.
```

---

## 7. Recommended next task: MAP-6S

MAP-6S: Build 42 LOTH candidate writer v2

Proceed with MAP-6S only after MAP-6R inspection confirms:
1. The field layout at bytes 12+ (binary vs immediately ASCII).
2. The minimum required entry count from the reference inspection.
3. Whether each entry has additional per-entry binary fields.

MAP-6S must not be started until these three questions have committed answers.

---

## 8. Non-claims

- No writer was changed as part of MAP-6R.
- No load test was performed.
- No PZ assets were copied into the repo.
- No writes to PZ mods or Zomboid folders.
- PLAYABLE_EXPORT_CLAIM_ALLOWED=false is binding.
