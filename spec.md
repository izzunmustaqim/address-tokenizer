# Address Tokenizer — Product Specification

## 1. Purpose
A console application that parses a free-form Malaysian address string and tokenizes it into structured, named address components, emitted as JSON.

## 2. Scope
- **In scope:** console I/O, parsing/tokenizing, validation, JSON output, graceful error handling.
- **Out of scope:** persistence, network calls, GUI, address geocoding/enrichment.

## 3. Actors & Interfaces
| Interface | Description |
|---|---|
| Console (stdin) | User enters one address string per run (or interactively, looped). |
| Console (stdout) | Pretty-printed JSON object of detected components. |

## 4. Functional Requirements

### FR-1 — Input handling
- **FR-1.1** Accept a single line (or multi-token) free-form string.
- **FR-1.2** Components may appear in **any order**.
- **FR-1.3** Every component is **optional**; an empty or all-unmatched input must not crash.
- **FR-1.4** Addresses **with and without commas** must both parse successfully.
- **FR-1.5** Trailing punctuation (e.g. `.` `,` `;`) is stripped from tokens **before** rule matching, in both comma and comma-less modes (so `Terengganu.` matches state `Terengganu`).

### FR-2 — Component extraction

| ID | Component | Rule |
|---|---|---|
| FR-2.1 | `apt` | Token starting with `"No "` followed by one or more digits (e.g. `No 11`). |
| FR-2.2 | `city` | Case-insensitive exact match against the fixed city list (§5.1). |
| FR-2.3 | `state` | Case-insensitive exact match against the fixed state list (§5.2). |
| FR-2.4 | `postcode` | **Exactly 5 digits**, integer value in inclusive range `01000`–`98859`, preserved as the original 5-char string. `1000` (4 digits) and `99999` do not match. |
| FR-2.5 | `street` | Token beginning with `"Jalan "`, `"Jln "`, `"Lorong "`, or `"Persiaran"`. `Persiaran` matches only at end-of-token or when followed by a space (so `Persiaran Utama` matches; `PersiaranFoo` does not). |
| FR-2.6 | `section` | **Catch-all**: any remaining unparsed text block(s), joined with a single space. |

### FR-3 — Precedence & ambiguity
- **FR-3.1** When a token could match multiple rules, apply precedence: `apt > postcode > city > state > street > section`.
- **FR-3.2** A value assigned to a specific component is removed from the candidate pool so it is not re-captured by `section`.
- **FR-3.2a** Each rule scans **all remaining unconsumed tokens** in the candidate pool — not just the first — so component order in the input does not matter (e.g. `section` text appearing before `city` in AS-1/AS-2).
- **FR-3.3** Only the **first** occurrence of each singleton component (`apt`, `postcode`, `city`, `state`, `street`) is kept; later duplicates fall through to `section`. (`section` is not a singleton — it accumulates all leftovers.)

### FR-4 — Output
- **FR-4.1** Emit a JSON object containing **only the components that were found** (absent keys for missing components).
- **FR-4.2** Keys, in canonical order: `apt`, `section`, `postcode`, `city`, `state`, `street`.
- **FR-4.3** All values are JSON strings.
- **FR-4.4** Pretty-printed with 2-space indentation.

### FR-5 — Robustness
- **FR-5.1** Null/empty/whitespace-only input → emit `{}` (not an error).
- **FR-5.2** Never throw unhandled exceptions to the console for user input.
- **FR-5.3** Malformed tokens (e.g. `No abc`, `999999`) simply fail their rule and fall through to `section`.

## 5. Reference Data

### 5.1 Cities
`Kuala Terengganu`, `Kuala Lumpur`, `Kajang`, `Bangi`, `Damansara`, `Petaling Jaya`, `Puchong`, `Subang Jaya`, `Cyberjaya`, `Putrajaya`, `Mantin`, `Kuching`, `Seremban`

### 5.2 States
`Selangor`, `Terengganu`, `Pahang`, `Kelantan`, `Melaka`, `Pulau Pinang`, `Kedah`, `Johor`, `Perlis`, `Sabah`, `Sarawak`

### 5.3 Street prefixes
`Jalan `, `Jln `, `Lorong `, and `Persiaran` (the first three require the trailing space; `Persiaran` also matches at end-of-token — see FR-2.5)

## 6. Acceptance Scenarios

### AS-1 — Full address (with commas)
**Input:** `No 11, Chendering, 21080 Kuala Terengganu, Terengganu.`
**Output:**
```json
{
  "apt": "No 11",
  "section": "Chendering",
  "postcode": "21080",
  "city": "Kuala Terengganu",
  "state": "Terengganu"
}
```

### AS-2 — Incomplete address
**Input:** `No 11, Kuala Terengganu, Chendering`
**Output:**
```json
{
  "apt": "No 11",
  "section": "Chendering",
  "city": "Kuala Terengganu"
}
```

### AS-3 — No commas (bonus)
**Input:** `No 11 Jalan Chendering 21080 Kuala Terengganu Terengganu`
**Output:**
```json
{
  "apt": "No 11",
  "street": "Jalan Chendering",
  "postcode": "21080",
  "city": "Kuala Terengganu",
  "state": "Terengganu"
}
```

### AS-4 — Empty input
**Input:** `` (empty)
**Output:** `{}`

### AS-5 — Only section
**Input:** `Somewhere Nice`
**Output:** `{ "section": "Somewhere Nice" }`

## 7. Non-functional Requirements
| ID | Requirement |
|---|---|
| NFR-1 | Deterministic: identical input → identical output. |
| NFR-2 | Single-user, in-process, no external dependencies at runtime beyond .NET BCL. |
| NFR-3 | Parsing of a typical address completes in < 10 ms. |
| NFR-4 | Code must compile with zero warnings under the .NET 10 SDK (`TreatWarningsAsErrors`), nullable reference types enabled. |

## 8. Out-of-scope / Future
- Multi-line addresses
- Address lookup/validation against a postal database
- Localisation (Bahasa Malaysia synonyms such as `Jln.` with period)
