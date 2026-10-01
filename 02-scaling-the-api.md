# Workflow 2: Scaling the API

How to take the single-process API with SQLite and grow it into something that handles real traffic. Each step is independent, so adopt them as the load demands.

## Target architecture

```mermaid
flowchart TD
    C["Clients"] --> GW["API gateway / load balancer"]
    GW --> RL{"Within rate limit?"}
    RL -- no --> E429["429 Too Many Requests, Retry-After"]
    RL -- yes --> A1["API instance 1"]
    RL -- yes --> A2["API instance 2"]
    RL -- yes --> AN["API instance N"]

    subgraph Instance["Each instance is stateless"]
        direction TB
        CT["ResumeController"] --> SV["ResumeAnalyzerService"]
        SV --> SK["SkillDictionary, singleton in memory"]
        SV --> CTX["Scoped DbContext"]
    end

    A1 --> Instance
    A2 --> Instance
    AN --> Instance

    CTX --> POOL["Connection pool"]
    POOL --> PRIMARY[("PostgreSQL / SQL Server primary")]
    PRIMARY --> REPLICA[("Read replica")]
    CTX -. "history and get by id" .-> REPLICA

    SV --> CACHE[("Redis cache")]
    CACHE -. "identical resume + JD hash" .-> SV

    subgraph Obs["Observability"]
        direction LR
        LOG["Structured logs"] --- MET["Metrics"] --- TRC["Tracing"]
    end
    Instance -.-> Obs
```

## Migration path off SQLite

```mermaid
flowchart LR
    S0["SQLite file<br/>single writer"] --> S1["Swap EF Core provider<br/>Npgsql or SqlServer"]
    S1 --> S2["Connection string from<br/>environment or Key Vault"]
    S2 --> S3["Run EF migrations<br/>in CI/CD"]
    S3 --> S4["Add indexes<br/>CreatedAt, hash"]
    S4 --> S5["Add paging to history"]
```

## Cache key for repeat analyses

```mermaid
flowchart LR
    R["Normalized resume text"] --> H["SHA-256 of resume + job description"]
    J["Normalized job description"] --> H
    H --> K["cache key + skill dictionary version"]
    K --> RC[("Redis, TTL")]
    DICT["SkillDictionary updated"] -->|"bumps version"| K
```

Including the dictionary version in the key means a dictionary change invalidates old results without clearing the cache by hand.

## Request path under load

```mermaid
sequenceDiagram
    participant C as Client
    participant G as Gateway
    participant A as API instance
    participant R as Redis
    participant D as Database

    C->>G: POST analyze
    G->>A: forward (rate limit passed)
    A->>R: GET key
    alt cache hit
        R-->>A: cached result
        A-->>C: 200 OK
    else cache miss
        A->>A: extract, match, score
        A->>D: INSERT analysis
        A->>R: SET key with TTL
        A-->>C: 200 OK
    end
```

## Scaling levers

| Bottleneck | Lever |
|---|---|
| SQLite single writer | Move to PostgreSQL or SQL Server, keep the EF Core code |
| Repeated identical requests | Redis cache keyed by input hash and dictionary version |
| Skill lookup cost | Load `SkillDictionary` once as a singleton and use a `HashSet` for lookups |
| Large history lists | Paging, index on creation time, read replica |
| CPU-heavy analysis | Scale instances horizontally, they hold no session state |
| Abusive clients | Rate limiting middleware or gateway policy |
| Slow requests | Async EF Core calls, request timeouts, cancellation tokens |
| Debugging at scale | Structured logging, metrics, health checks, tracing |
| Deployment | Docker image, Azure App Service or container platform, auto-scale on CPU or queue depth |
