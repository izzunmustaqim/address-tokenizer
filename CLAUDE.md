# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project status

This repository is currently **design-only**: it contains the specification documents for an Address Tokenizer assignment but no code yet. The source of truth for what to build is the four Markdown documents below — read `spec.md` (normative requirements, identified `FR-x`/`NFR-x`) and `design.md` (architecture) before writing code, and follow `tasks.md` (phased task checklist) to build it incrementally. The original assignment brief is `address_tokenizer_specification.md` plus the PDF; where the brief and `spec.md` disagree, `spec.md` is stricter (it refines ambiguous rules such as the postcode being *exactly* 5 digits).

Do not invent requirements beyond these documents. When a spec detail is unclear, prefer the most specific statement of it (e.g. `spec.md` FR-2.4 over the brief's range-only postcode rule).

## Planned build/test commands

The .NET 10 SDK is installed (`dotnet --version` → 10.0.x). Per `tasks.md` T0.1, the solution will be:

```
AddressTokenizer.sln
├─ AddressTokenizer.Spec   (classlib — Domain + Application)
├─ AddressTokenizer.Cli     (console — composition root)
└─ AddressTokenizer.Tests   (xUnit)
```

Once scaffolded:

```bash
dotnet build                                        # zero warnings required (TreatWarningsAsErrors)
dotnet test                                         # full test suite
dotnet test --filter "FullyQualifiedName~PostcodeRule"   # run a single rule's tests
dotnet test --filter "FullyQualifiedName~Acceptance"     # acceptance scenarios AS-1..AS-5
dotnet run --project AddressTokenizer.Cli           # interactive: one address per line, empty line/EOF exits
dotnet run --project AddressTokenizer.Cli -- "No 11, Chendering, 21080 Kuala Terengganu, Terengganu."
dotnet format                                       # formatting before final build
```

Project references: `Cli → Spec`, `Tests → Spec`. Runtime must be BCL-only (`System.Text.Json`); xUnit is a dev-time dependency only (NFR-2).

## Architecture

C# / .NET 10, layered, built around a **Chain of Responsibility** of parsing rules:

- **Domain** — `ParsedAddress` (result model; a `null` property means *not found* → key omitted from JSON), `IParserRule` (`ComponentName`, `TryParse(TokenSpan, ParsedAddress)`), one concrete rule per component, and `ReferenceData` (the fixed city/state/prefix lists from `spec.md` §5).
- **Application** — `AddressTokenizer` orchestrates the pipeline; `ITokenizer` is a Strategy (`CommaTokenizer` / `WhitespaceTokenizer`); `ParserRuleFactory.BuildChain()` encodes precedence in one place.
- **Infrastructure** — `AddressJsonSerializer`.
- **Cli** — `Program.cs` is the composition root; DI wiring happens here and nowhere else.

Two invariants that are easy to get wrong and are called out repeatedly in the spec:

1. **Rule precedence:** `apt > postcode > city > state > street > section`. Each rule scans **all remaining unconsumed tokens**, not just the first (FR-3.2a) — component order in the input is irrelevant. A matched value is consumed out of the pool so `section` cannot re-capture it (FR-3.2). Only the **first** occurrence of each singleton component is kept; duplicates fall through to `section`, which is not a singleton and accumulates all leftovers (FR-3.3).
2. **Tokenization runs in both modes with the same longest-match scanner:** input is split into segments on `,` `;` newline (comma-less input = one segment), then every segment is scanned left-to-right trying units in order — apt, 5-digit postcode, street, city/state full name — else one word. This is why `21080 Kuala Terengganu` inside a comma segment still yields two tokens, and why street names stop at the next protected unit (`Jalan Chendering 21080` → street is `Jalan Chendering`). Trailing punctuation (`. , ; :`) is stripped from tokens **before** rule matching in both modes (FR-1.5), so `Terengganu.` matches the state rule.

### Behavioural contract (from spec.md)

- Components may appear in **any order**; every component is optional; empty/whitespace input → `{}`, never an error.
- Output JSON: only found components, canonical key order `apt, section, postcode, city, state, street`, all values strings, 2-space indent.
- Postcode: exactly 5 digits, integer value in `01000`–`98859`, preserved as the original 5-char string (`1000`, `99999`, `000001` do not match).
- Street prefixes `Jalan ` / `Jln ` / `Lorong ` require the trailing space; `Persiaran` also matches at end-of-token (`Persiaran Utama` ✓, `PersiaranFoo` ✗).
- Rules must never throw; unexpected exceptions go to **stderr** with exit code 1.
- Build must be warning-free: nullable reference types enabled, `TreatWarningsAsErrors` (NFR-4).

## Testing strategy (design.md §7)

One test class per rule (match / no-match / boundaries: postcode `01000`, `98859`, `00999`, `98860`), integration tests asserting **exact JSON equality** for acceptance scenarios AS-1…AS-5 in `spec.md` §6, and a property-style check that an address with commas produces identical JSON to the same address without them. Every FR should be covered by at least one test before delivery (T8.2).

## Workflow

- `tasks.md` is the working checklist — update task status (`[ ]` / `[~]` / `[x]`) as phases complete. Phases are ordered by the dependency graph at the bottom of that file.
- Evaluation criteria for this assignment are OOP/SOLID design patterns, robustness, and parsing accuracy — the design intentionally favours a CoR chain over a single regex for that reason (design.md §8). Preserve that structure rather than collapsing rules into one parser.
