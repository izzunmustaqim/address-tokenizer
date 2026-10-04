# Address Tokenizer — Technical Design

## 1. Overview
Language: **C# (.NET 10, current LTS)** — BCL-only at runtime (`System.Text.Json`); xUnit for tests (dev-time only).  
Architecture: layered console app with a **Chain of Responsibility** of parsing rules, plus **Strategy** for tokenization modes and **Factory** for rule composition.

```
┌─────────────────────────────────────────────────┐
│ Program (Composition Root / Presentation)       │
│   reads stdin → AddressTokenizer → prints JSON  │
├─────────────────────────────────────────────────┤
│ Application                                     │
│   AddressTokenizer  (orchestrates pipeline)     │
│   TokenizerNormalizer (comma & punctuation)     │
├─────────────────────────────────────────────────┤
│ Domain                                          │
│   ParsedAddress (result model)                  │
│   IParserRule + concrete rules (CoR chain)      │
│   ReferenceData (cities, states, prefixes)      │
├─────────────────────────────────────────────────┤
│ Infrastructure                                  │
│   AddressJsonSerializer                         │
└─────────────────────────────────────────────────┘
```

## 2. Design Patterns & Rationale

| Pattern | Where | Why (SOLID) |
|---|---|---|
| **Chain of Responsibility** | `IParserRule` chain: Apt → Postcode → City → State → Street → Section | **OCP**: add a new component by adding a rule, not editing existing ones. Each rule is a single, testable unit (**SRP**). |
| **Strategy** | `ITokenizer` — `CommaTokenizer` / `WhitespaceTokenizer` | Algorithm (how to split input) varies independently from the rules that consume tokens (**OCP**, **DIP**). |
| **Factory / Builder** | `ParserRuleFactory.BuildChain()` | Centralizes chain ordering/precedence; consumers depend on the factory abstraction, not concrete rule classes (**DIP**). |
| **Composition over inheritance** | Rules composed into a chain, not a rule subclass hierarchy | Avoids fragile base classes; rules are interchangeable (**LSP**). |
| **Dependency Injection** | `AddressTokenizer` receives rules + tokenizer via constructor | Testable, explicit dependencies (**DIP**). Wired once in `Program` (composition root). |

## 3. Core Abstractions

### 3.1 Rule interface (Chain of Responsibility)
```csharp
public interface IParserRule
{
    string ComponentName { get; }          // e.g. "apt"
    bool TryParse(TokenSpan span, ParsedAddress result);
}
```
- `TryParse` inspects the current candidate span; on success it writes into `ParsedAddress` and **consumes** the matched text (removing it from the candidate pool — FR-3.2).
- Each rule scans **all remaining unconsumed tokens** in the span, not just the head — so input order of components is irrelevant (FR-3.2a).
- Chain advances rule-by-rule over the same span until no rule matches; leftovers feed `SectionRule`.

### 3.2 Concrete rules
| Rule | Priority | Match |
|---|---|---|
| `AptRule` | 1 | `^No\s+\d+` |
| `PostcodeRule` | 2 | exactly 5 digits, value ∈ [1000, 98859] (i.e. `01000`–`98859`) |
| `CityRule` | 3 | exact (case-insensitive) lookup in `ReferenceData.Cities` |
| `StateRule` | 4 | exact (case-insensitive) lookup in `ReferenceData.States` |
| `StreetRule` | 5 | starts with `Jalan ` / `Jln ` / `Lorong ` / `Persiaran` (`Persiaran` also matches at end-of-token; `PersiaranFoo` does not match — FR-2.5) |
| `SectionRule` | 6 | catch-all: appends remaining unconsumed text |

### 3.3 Input normalization (bonus: comma-less input)
```
raw input
  → trim, collapse whitespace
  → split into SEGMENTS on [,;\n] (hard boundaries).
    If no delimiters present, the whole input is one segment
    (comma-less / bonus mode).
  → scan EVERY segment left-to-right with longest-match units.
    At each position, try in order:
      1. apt:       /^No\s+\d+/          → "No 11"
      2. postcode:  /^\d{5}$/ + in range → "21080"
      3. street:    prefix (Jalan /Jln /Lorong /Persiaran)
                    + following words until the next position
                    that starts unit 1, 2 or 4 — or segment end
                                               → "Jalan Chendering"
      4. city/state: full name from ReferenceData
                                               → "Kuala Terengganu"
    else consume one whitespace-delimited word.
  → strip trailing punctuation (.,;:) from each token
    (both modes — so "Terengganu." matches StateRule, FR-1.5)
  → ordered token list
```

Key points:
- The longest-match scan runs in **both** modes — comma mode only *adds* hard segment boundaries first. So AS-1's segment `"21080 Kuala Terengganu"` still yields **two** units: postcode + city.
- Street names stop consuming words when the next protected unit begins (`Jalan Chendering 21080 …` → street = `Jalan Chendering`, not `Jalan Chendering 21080`).
- Comma-less mode (bonus) is the same scanner with no segment delimiters.

### 3.4 Result model
```csharp
public sealed class ParsedAddress
{
    public string? Apt      { get; set; }
    public string? Section  { get; set; }
    public string? Postcode { get; set; }
    public string? City     { get; set; }
    public string? State    { get; set; }
    public string? Street   { get; set; }
}
```
Null ⇒ component not present ⇒ key omitted from JSON (FR-4.1).

## 4. Sequence (single parse)

```
Program
  │  read line
  ▼
AddressTokenizer.TokenizeAsync(input)
  │  1. guard null/empty            → return empty ParsedAddress
  │  2. ITokenizer.Tokenize(input)  → tokens (normalized, punctuation-stripped)
  │  3. build ParsedAddress + TokenSpan
  │  4. foreach rule in chain: rule.TryParse(span, result)
  │     (each rule scans all remaining unconsumed tokens — FR-3.2a)
  │  5. SectionRule absorbs remainder
  ▼
AddressJsonSerializer.Serialize(result)
  │  System.Text.Json, indented, only non-null props
  ▼
Console.Out
```

## 5. Project Structure
```
TM/
├─ AddressTokenizer.Spec/          # class library (Domain + Application)
│  ├─ Domain/
│  │  ├─ ParsedAddress.cs
│  │  ├─ IParserRule.cs
│  │  ├─ Rules/ (Apt, Postcode, City, State, Street, Section)
│  │  └─ ReferenceData.cs
│  ├─ Application/
│  │  ├─ AddressTokenizer.cs
│  │  ├─ Tokenization/ (ITokenizer, CommaTokenizer, WhitespaceTokenizer)
│  │  └─ ParserRuleFactory.cs
│  └─ Infrastructure/
│     └─ AddressJsonSerializer.cs
├─ AddressTokenizer.Cli/           # console (composition root)
│  └─ Program.cs
├─ AddressTokenizer.Tests/         # xUnit
└─ spec.md · design.md · tasks.md
```

## 6. Error-handling strategy
| Failure | Handling |
|---|---|
| Null/empty input | Return empty `ParsedAddress` → `{}` |
| Unmatched tokens | Fall through to `section` |
| Malformed postcode (`999999`, `abc`) | Rule returns false; token → `section` |
| Unexpected exception in pipeline | `Program` top-level `try/catch` prints error to **stderr**, exit code 1 |
| Rule throws internally | Rule must never throw; defensive `TryParse` contract |

## 7. Testing strategy
- **Unit:** one test class per rule (match / no-match / boundary: postcode `01000`, `98859`, `00999`, `98860`).
- **Integration:** `AddressTokenizer` against all acceptance scenarios in `spec.md` §6.
- **Property-style:** input with commas vs. same input without commas → identical JSON.

## 8. Key trade-offs
- **Chain of Responsibility vs. single regex:** chain is more code but each rule is isolated, extensible, and maps 1:1 to the spec table — better signal on the rubric's OOP/SOLID criteria.
- **Longest-match tokenization** adds complexity but is required for comma-less inputs with multi-word city/state names (bonus marks).
