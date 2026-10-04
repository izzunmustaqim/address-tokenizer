# Address Tokenizer

A console application that parses a free-form **Malaysian address string** and tokenizes it into structured, named address components, emitted as JSON.

```
Input:  No 11, Chendering, 21080 Kuala Terengganu, Terengganu.
Output: { "apt": "No 11", "section": "Chendering", "postcode": "21080", "city": "Kuala Terengganu", "state": "Terengganu" }
```

- Components may appear in **any order**; every component is optional.
- Works **with or without commas** (bonus: comma-less input).
- Missing components are **omitted** from the JSON (never `null`), so empty input yields `{}`.
- Deterministic: identical input always produces identical output.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`dotnet --version` → 10.0.x)
- No other runtime dependencies — BCL only (`System.Text.Json`); xUnit is a dev-time dependency for tests.

## Build & run

```bash
dotnet build          # zero warnings required (TreatWarningsAsErrors)
dotnet test           # full test suite (96 tests)
```

Run a single address from the command line:

```bash
dotnet run --project AddressTokenizer.Cli -- "No 11, Chendering, 21080 Kuala Terengganu, Terengganu."
```

Or run interactively (one address per line; an empty line or EOF exits):

```bash
dotnet run --project AddressTokenizer.Cli
```

Run a subset of tests:

```bash
dotnet test --filter "FullyQualifiedName~PostcodeRule"   # a single rule
dotnet test --filter "FullyQualifiedName~Acceptance"     # acceptance scenarios AS-1..AS-5
```

## Example session

```
> No 11, Chendering, 21080 Kuala Terengganu, Terengganu.
{
  "apt": "No 11",
  "section": "Chendering",
  "postcode": "21080",
  "city": "Kuala Terengganu",
  "state": "Terengganu"
}
> No 11 Jalan Chendering 21080 Kuala Terengganu Terengganu
{
  "apt": "No 11",
  "street": "Jalan Chendering",
  "postcode": "21080",
  "city": "Kuala Terengganu",
  "state": "Terengganu"
}
> Somewhere Nice
{
  "section": "Somewhere Nice"
}
>
```

## Output format

- JSON object with **only the components that were found** (absent keys for missing ones).
- Keys in canonical order: `apt`, `section`, `postcode`, `city`, `state`, `street`.
- All values are strings, pretty-printed with 2-space indentation.
- Empty/whitespace-only input → `{}` (not an error); unexpected errors go to **stderr** with exit code 1.

### Extraction rules (summary)

| Component | Rule |
|---|---|
| `apt` | Token starting with `No ` followed by digits (e.g. `No 11`) |
| `postcode` | Exactly 5 digits, value in `01000`–`98859`, kept as the original 5-char string |
| `city` | Case-insensitive exact match against the fixed city list ([spec.md §5.1](spec.md)) |
| `state` | Case-insensitive exact match against the fixed state list ([spec.md §5.2](spec.md)) |
| `street` | Starts with `Jalan `, `Jln `, `Lorong `, or `Persiaran` (`Persiaran` also matches at end-of-token) |
| `section` | Catch-all: all remaining unparsed text, joined with single spaces |

**Precedence:** `apt > postcode > city > state > street > section`. Each rule scans *all* remaining unconsumed tokens (so input order doesn't matter), and a matched value is consumed out of the pool so `section` cannot re-capture it. Only the first occurrence of each singleton component is kept.

## Architecture

C# / .NET 10, layered, built around a **Chain of Responsibility** of parsing rules:

```
Program (composition root)  — reads stdin → AddressTokenizer → prints JSON
├─ Application              — AddressTokenizer (orchestrates pipeline),
│                             ITokenizer Strategy (Comma / Whitespace),
│                             ParserRuleFactory (builds the chain)
├─ Domain                   — ParsedAddress, IParserRule + one rule per component,
│                             ReferenceData (cities, states, prefixes)
└─ Infrastructure           — AddressJsonSerializer (System.Text.Json)
```

| Pattern | Where | Why |
|---|---|---|
| **Chain of Responsibility** | `Apt → Postcode → City → State → Street → Section` | New component = new rule, no edits to existing ones (OCP); each rule is a single testable unit (SRP) |
| **Strategy** | `ITokenizer` — `CommaTokenizer` / `WhitespaceTokenizer` | Splitting algorithm varies independently of the rules that consume tokens |
| **Factory** | `ParserRuleFactory.BuildChain()` | Encodes rule precedence in one place |
| **Dependency Injection** | `AddressTokenizer` receives chain + tokenizer via constructor | Testable, explicit dependencies; wired once in `Program` |

Tokenization runs in both modes with the same longest-match scanner: input is split into segments on `,` `;` newline (comma-less input = one segment), then each segment is scanned left-to-right trying units in order — apt, 5-digit postcode, street, city/state full name — else one word. Trailing punctuation (`. , ; :`) is stripped before rule matching, so `Terengganu.` matches the state rule.

## Project structure

```
├─ AddressTokenizer.Spec/       # class library — Domain + Application
│  ├─ Domain/  (ParsedAddress, IParserRule, TokenSpan, ReferenceData, Rules/)
│  ├─ Application/ (AddressTokenizer, ParserRuleFactory, Tokenization/)
│  └─ Infrastructure/ (AddressJsonSerializer)
├─ AddressTokenizer.Cli/        # console — composition root
├─ AddressTokenizer.Tests/      # xUnit test suite
├─ spec.md                      # normative requirements (FR-x / NFR-x)
├─ design.md                    # technical design & pattern rationale
├─ tasks.md                     # phased task checklist
└─ address_tokenizer_specification.md  # original assignment brief (+ PDF)
```

## Testing

- **Unit:** one test class per rule — match / no-match / boundaries (postcode `01000`, `98859`, `00999`, `98860`, …).
- **Integration:** acceptance scenarios AS-1…AS-5 ([spec.md §6](spec.md)) asserted with exact JSON equality.
- **Property-style:** an address with commas produces identical JSON to the same address without them.

```bash
dotnet test           # 96 tests, all green
```

## Documentation

- [spec.md](spec.md) — product specification: functional/non-functional requirements and acceptance scenarios
- [design.md](design.md) — technical design: patterns, abstractions, sequence, trade-offs
- [tasks.md](tasks.md) — phased implementation checklist and status
- [CLAUDE.md](CLAUDE.md) — working guidance for AI-assisted development on this repo

## Status

Phases 0–2 complete (solution scaffold, domain model & reference data, rule chain with tests — 96 passing). Remaining phases (tokenization, orchestration, JSON output, CLI, hardening, docs) are tracked in [tasks.md](tasks.md).

## License

Not specified — see the original assignment brief for terms.
