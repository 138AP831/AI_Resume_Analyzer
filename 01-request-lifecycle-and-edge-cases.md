# Workflow 1: Request Lifecycle and Edge Cases

How `POST /api/Resume/analyze` should behave from the first byte in to the saved result, including the awkward inputs a rule-based skill matcher has to survive.

> These diagrams describe the recommended behaviour. Check each branch against your controller and `ResumeAnalyzerService` and adjust where the code differs.

## Analyze request flow

```mermaid
flowchart TD
    REQ["POST /api/Resume/analyze"] --> BIND{"Valid JSON body?"}
    BIND -- no --> E400A["400 Bad Request: malformed body"]
    BIND -- yes --> V1{"resumeText empty or whitespace?"}
    V1 -- yes --> E400B["400: resumeText is required"]
    V1 -- no --> V2{"jobDescription empty or whitespace?"}
    V2 -- yes --> E400C["400: jobDescription is required"]
    V2 -- no --> V3{"Either text over max length?"}
    V3 -- yes --> E413["400 or 413: input too large"]
    V3 -- no --> NORM["Normalize: lowercase, trim, collapse whitespace"]

    NORM --> EXJ["Extract skills from job description"]
    EXJ --> JD0{"Any skills found in job description?"}
    JD0 -- no --> E422["422 or 200 with note: no recognizable skills in job description"]
    JD0 -- yes --> EXR["Extract skills from resume"]

    EXR --> SET["Build distinct skill sets"]
    SET --> MATCH["matched = JD skills in resume"]
    MATCH --> MISS["missing = JD skills not in resume"]
    MISS --> PCT["percentage = matched count / JD skill count"]
    PCT --> SUG["Generate suggestions from missing skills"]
    SUG --> SAVE["Save analysis via EF Core"]
    SAVE --> DBOK{"Save succeeded?"}
    DBOK -- no --> E500["500 with problem details, no partial row"]
    DBOK -- yes --> OK["200 OK: id, percentage, matched, missing, suggestions"]
```

## Matching pitfalls and how to handle them

```mermaid
flowchart LR
    T["Raw text"] --> TOK["Tokenize with skill-aware rules"]
    TOK --> P1{"Substring trap?"}
    P1 -- "java inside javascript" --> F1["Match on word boundaries"]
    TOK --> P2{"Symbols in skill names?"}
    P2 -- "c#, .net, asp.net" --> F2["Do not strip # or . before matching"]
    TOK --> P3{"Plural or variant?"}
    P3 -- "REST APIs vs REST API" --> F3["Alias map: rest api, rest apis, restful"]
    TOK --> P4{"Short ambiguous word?"}
    P4 -- "go, r, c" --> F4["Require context or exact casing"]
    TOK --> P5{"Repeated skill?"}
    P5 -- "SQL x5" --> F5["Use distinct set, count once"]
    F1 --> OUT["Clean skill set"]
    F2 --> OUT
    F3 --> OUT
    F4 --> OUT
    F5 --> OUT
```

## Other endpoints

```mermaid
flowchart TD
    G["GET /api/Resume/id"] --> F{"Row exists?"}
    F -- yes --> R200["200 OK with analysis"]
    F -- no --> R404["404 Not Found"]

    D["DELETE /api/Resume/id"] --> DF{"Row exists?"}
    DF -- yes --> DEL["Delete and save"]
    DEL --> R204["204 No Content"]
    DF -- no --> D404["404 Not Found"]

    H["GET /api/Resume/history"] --> PAGE["Order by newest, apply paging"]
    PAGE --> EMPTY{"Any rows?"}
    EMPTY -- yes --> H200["200 OK with list"]
    EMPTY -- no --> H200E["200 OK with empty list"]
```

## Concurrency note

```mermaid
sequenceDiagram
    participant A as Request A
    participant B as Request B
    participant API as Web API
    participant DB as SQLite

    A->>API: analyze
    B->>API: analyze
    API->>DB: INSERT (A)
    API->>DB: INSERT (B)
    Note over DB: SQLite allows one writer at a time
    DB-->>API: A committed
    DB-->>API: B waits, then commits
    API-->>A: 200 OK id=1
    API-->>B: 200 OK id=2
```

Each request gets its own scoped `DbContext`, so requests never share tracked entities. Brief write contention on SQLite is handled by its busy timeout; sustained write load is the signal to move to a server database (see workflow 2).

## Behaviour summary

| Situation | Expected behaviour |
|---|---|
| Missing or blank resume or job description | 400 with a clear message |
| Job description with no known skills | Explicit message rather than a misleading 0% or a divide-by-zero |
| `java` inside `javascript` | No false match, use word-boundary matching |
| `c#`, `.net`, `asp.net core` | Symbols preserved during tokenizing |
| Same skill repeated | Counted once |
| Unknown id on GET or DELETE | 404 |
| Empty history | 200 with an empty list |
| Database failure | 500, nothing half-saved |
