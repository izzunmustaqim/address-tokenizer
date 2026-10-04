# Address Tokenizer — Task Breakdown

> Status legend: `[ ]` todo · `[~]` in progress · `[x]` done  
> Each task is sized to be completable in one sitting.

---

## Phase 0 — Setup
- [x] **T0.1** Initialize .NET 10 solution: `AddressTokenizer.sln` with projects `AddressTokenizer.Spec` (classlib), `AddressTokenizer.Cli` (console), `AddressTokenizer.Tests` (xUnit). SDK check: `dotnet --version` → 10.0.x.
- [x] **T0.2** Add project references: `Cli → Spec`, `Tests → Spec`.
- [x] **T0.3** Enable nullable reference types + `TreatWarningsAsErrors`; verify clean build (`dotnet build`).
- [x] **T0.4** Create folder skeleton per `design.md` §5 (Domain, Rules, Application, Tokenization, Infrastructure).
- [x] **T0.5** Commit initial scaffold.

**Exit criteria:** `dotnet test` runs (0 tests), zero warnings.

---

## Phase 1 — Domain model & reference data
- [x] **T1.1** Implement `ParsedAddress` (nullable string properties, canonical member order).
- [x] **T1.2** Implement `ReferenceData`: cities (13), states (11), street prefixes (4) as read-only, case-insensitive lookups.
- [x] **T1.3** Unit tests: every city/state in the spec resolves; unknown value does not.
- [x] **T1.4** Implement `TokenSpan` (candidate text + consume/remainder mechanics).

**Exit criteria:** reference data matches `spec.md` §5 exactly; tests green.

---

## Phase 2 — Rule chain (Chain of Responsibility)
- [x] **T2.1** Define `IParserRule` (`ComponentName`, `TryParse(TokenSpan, ParsedAddress)`).
- [x] **T2.2** `AptRule` — `No` + digits (`FR-2.1`); tests: `No 11` ✓, `No abc` ✗, `No11` ✗.
- [x] **T2.3** `PostcodeRule` — **exactly 5 digits**, value range 01000–98859 (`FR-2.4`); boundary tests: `01000` ✓, `98859` ✓, `00999` ✗, `98860` ✗, `99999` ✗, `1000` ✗ (4 digits), `000001` ✗ (6 digits).
- [x] **T2.4** `CityRule` — exact case-insensitive match (`FR-2.2`); tests incl. multi-word `Kuala Terengganu`, `Petaling Jaya`.
- [x] **T2.5** `StateRule` — exact case-insensitive match (`FR-2.3`); tests incl. `Pulau Pinang`.
- [x] **T2.6** `StreetRule` — prefixes `Jalan `, `Jln `, `Lorong `, `Persiaran` (`FR-2.5`); tests: case variants, `Persiaran` alone ✓, `Persiaran Utama` ✓, `PersiaranFoo` ✗.
- [x] **T2.7** `SectionRule` — catch-all remainder, single-space join (`FR-2.6`).
- [x] **T2.8** `ParserRuleFactory.BuildChain()` — encodes precedence `apt > postcode > city > state > street > section` (`FR-3.1`).

**Exit criteria:** every rule has match + no-match + edge tests; chain order verified.

---

## Phase 3 — Tokenization (with/without commas)
- [ ] **T3.1** Define `ITokenizer` strategy interface.
- [ ] **T3.2** `CommaTokenizer` — split on `,` / `;` / newline into segments (hard boundaries), trim, drop empties; **each segment is then handed to the shared longest-match scanner** (so segment `21080 Kuala Terengganu` yields two tokens: `21080`, `Kuala Terengganu`).
- [ ] **T3.3** Longest-match scanner (used in **both** modes, within every segment): try units in order — apt `/^No\s+\d+/`, 5-digit postcode, street (prefix + words until next protected unit or segment end), city/state full name — else one word (`FR-1.4`, design §3.3).
- [ ] **T3.4** `TokenizerNormalizer` / selector — detect delimiters; choose strategy; collapse whitespace; **strip trailing punctuation (`.`, `,`, `;`, `:`) from every token in both modes** (`FR-1.5`). Tests: `Terengganu.` → `Terengganu`; `Jalan Chendering 21080` → street stops at `21080`.
- [ ] **T3.5** Tests: comma vs. no-comma variants of the same address produce the same token sequence; AS-1 segmentizes to `["No 11", "Chendering", "21080", "Kuala Terengganu", "Terengganu"]`; AS-3 tokenizes to `["No 11", "Jalan Chendering", "21080", "Kuala Terengganu", "Terengganu"]`.

**Exit criteria:** AS-1 and AS-3 inputs tokenize identically.

---

## Phase 4 — Orchestration
- [ ] **T4.1** `AddressTokenizer` — constructor-injected chain + tokenizer (`DIP`); guards null/empty input → empty result (`FR-5.1`).
- [ ] **T4.2** Pipeline: normalize → tokenize → apply rules in order; **each rule scans all remaining unconsumed tokens, not just the head** (`FR-3.2a`); leftover to section.
- [ ] **T4.3** Singleton enforcement: first match wins for apt/postcode/city/state/street (`FR-3.3`); duplicates fall to section.
- [ ] **T4.4** Integration tests: acceptance scenarios **AS-1 … AS-5** from `spec.md` §6 (exact JSON equality).

**Exit criteria:** all acceptance scenarios pass.

---

## Phase 5 — JSON output
- [ ] **T5.1** `AddressJsonSerializer` — `System.Text.Json`, ignore nulls, 2-space indent, canonical key order (`FR-4.1…4.4`).
- [ ] **T5.2** Tests: empty address → `{}`; partial address omits missing keys.

**Exit criteria:** outputs byte-match spec examples.

---

## Phase 6 — Console CLI
- [ ] **T6.1** `Program.cs` composition root: build tokenizer via factory/DI; read a line from stdin — if given a CLI arg, parse that single input and exit; otherwise loop, printing JSON per line, until **EOF or an empty line** terminates the loop.
- [ ] **T6.2** Print JSON to stdout; unexpected exceptions → message on stderr + exit code 1 (`FR-5.2`).
- [ ] **T6.3** Optional: read address from CLI arg if provided, else interactive prompt.
- [ ] **T6.4** Manual smoke test: paste AS-1 and AS-2 inputs, confirm output.

**Exit criteria:** `dotnet run --project AddressTokenizer.Cli` handles all spec examples gracefully.

---

## Phase 7 — Hardening & robustness
- [ ] **T7.1** Fuzz-ish tests: empty, whitespace-only, punctuation-only (`,,,.`), 500-char input, emoji, control chars — no unhandled exceptions.
- [ ] **T7.2** Malformed tokens (`No abc`, `999999`, `1234567890`) fall through to section (`FR-5.3`).
- [ ] **T7.3** Idempotence test: same input twice → identical output (NFR-1).
- [ ] **T7.4** Review that no rule throws: audit `TryParse` implementations.

**Exit criteria:** full test suite green; no unhandled exceptions under fuzz cases.

---

## Phase 8 — Documentation & delivery
- [ ] **T8.1** Write `README.md`: purpose, build/run instructions, example session, architecture summary.
- [ ] **T8.2** Cross-check `spec.md` ↔ implementation: every FR covered by ≥1 test.
- [ ] **T8.3** `dotnet format` + final warning-free build; run full test suite.
- [ ] **T8.4** Tag release `v1.0`; final commit.

**Exit criteria:** README enables a reviewer to build and run in < 2 minutes.

---

## Dependency graph
```
Phase 0 ──► Phase 1 ──► Phase 2 ──┐
                │                 ├──► Phase 4 ──► Phase 5 ──► Phase 6 ──► Phase 7 ──► Phase 8
                └──► Phase 3 ─────┘
```

## Effort estimate
| Phase | Est. |
|---|---|
| 0 Setup | 0.5 h |
| 1 Domain | 1 h |
| 2 Rules | 2 h |
| 3 Tokenizer | 2 h |
| 4 Orchestration | 1 h |
| 5 JSON | 0.5 h |
| 6 CLI | 0.5 h |
| 7 Hardening | 1 h |
| 8 Docs | 1 h |
| **Total** | **~9.5 h** |
