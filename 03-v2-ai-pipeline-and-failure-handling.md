# Workflow 3: V2 AI Pipeline and Failure Handling

The roadmap version: PDF upload, LLM-based skill extraction, and graceful fallback to the existing rule-based matcher when the AI path fails. The rule-based engine stays as the safety net.

## End-to-end V2 flow

```mermaid
flowchart TD
    U["User uploads resume PDF + job description"] --> AUTH{"JWT valid?"}
    AUTH -- no --> E401["401 Unauthorized"]
    AUTH -- yes --> FV{"PDF valid, size and type OK?"}
    FV -- no --> E400["400: invalid file"]
    FV -- yes --> EXT["Extract text from PDF"]
    EXT --> TXT{"Text found?"}
    TXT -- no --> OCR["OCR fallback"]
    OCR --> OCRQ{"OCR text usable?"}
    OCRQ -- no --> E422["422: could not read resume, ask for text or a better file"]
    OCRQ -- yes --> PII
    TXT -- yes --> PII["Redact or minimize personal data before sending out"]

    PII --> CK{"Result cached for this input?"}
    CK -- yes --> RES["Return cached analysis"]
    CK -- no --> LLM["Azure OpenAI: extract skills as structured JSON"]

    LLM --> OK{"Valid JSON within timeout?"}
    OK -- yes --> ENR["Embeddings: map synonyms to canonical skills"]
    OK -- "429, timeout, bad JSON" --> RT["Retry with backoff, limited attempts"]
    RT --> RTOK{"Recovered?"}
    RTOK -- yes --> ENR
    RTOK -- no --> FB["Fallback: rule-based SkillDictionary extraction"]

    ENR --> SCORE["Match, ATS keyword score, suggestions"]
    FB --> SCORE
    SCORE --> SAVE["Save analysis + source: ai or rules"]
    SAVE --> RES
    RES --> OUT["200 OK with analysis and extractionMode"]
```

## Circuit breaker around the AI call

```mermaid
stateDiagram-v2
    [*] --> Closed
    Closed --> Open: failures exceed threshold
    Open --> HalfOpen: cool-down elapsed
    HalfOpen --> Closed: probe call succeeds
    HalfOpen --> Open: probe call fails
    Closed: Closed - use Azure OpenAI
    Open: Open - skip AI, use rule-based matcher
    HalfOpen: HalfOpen - allow one probe request
```

While the breaker is open, requests are served by the rule-based matcher immediately, so users get an answer instead of waiting on a failing dependency.

## Failure handling

```mermaid
flowchart LR
    F1["Azure OpenAI 429"] --> A1["Backoff + jitter, honor Retry-After"]
    F2["Timeout"] --> A2["Cancel call, retry once, then fallback"]
    F3["Malformed JSON from model"] --> A3["Validate against schema, one repair retry, then fallback"]
    F4["Content filter triggered"] --> A4["Fallback to rules, log event"]
    F5["Hallucinated skill not in text"] --> A5["Keep only skills whose evidence appears in the resume text"]
    F6["Prompt injection inside resume"] --> A6["Treat resume as data, strict system prompt, ignore embedded instructions"]
    F7["Token limit exceeded"] --> A7["Chunk long resumes, merge skill sets"]
    A1 --> R["Always return a result and say which mode produced it"]
    A2 --> R
    A3 --> R
    A4 --> R
    A5 --> R
    A6 --> R
    A7 --> R
```

## Background processing for large uploads

```mermaid
sequenceDiagram
    participant C as Client
    participant API as Web API
    participant Q as Queue
    participant W as Worker
    participant AI as Azure OpenAI
    participant DB as Database

    C->>API: Upload PDF
    API->>DB: Create analysis, status queued
    API->>Q: Enqueue job
    API-->>C: 202 Accepted + analysis id
    Q->>W: Deliver job
    W->>AI: Extract skills
    alt AI available
        AI-->>W: skills JSON
    else AI failing
        W->>W: Rule-based fallback
    end
    W->>DB: Save result, status completed
    C->>API: GET analysis by id
    API->>DB: Read
    API-->>C: 200 OK with result
```

## Design notes

| Concern | Approach |
|---|---|
| Availability | Rule-based matcher is always available as the fallback |
| Transparency | Response reports whether the result came from AI or rules |
| Cost | Cache by input hash, trim text before sending, skip the AI call for tiny inputs |
| Privacy | Minimize personal data sent to the model, no resume text in logs |
| Trust | Verify AI-extracted skills against the source text before scoring |
| Security | JWT on endpoints, upload size and type limits, secrets in Key Vault |
