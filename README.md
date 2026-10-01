# AI Resume Analyzer

A simple ASP.NET Core Web API that compares a resume against a job description and produces a skill-match percentage, matched skills, missing skills, and improvement suggestions.

## Tech Stack

- C#
- ASP.NET Core 8 Web API
- Entity Framework Core
- SQLite
- REST APIs
- Swagger / OpenAPI
- LINQ
- Dependency Injection

## Features

- Resume vs job-description analysis
- Skill matching
- Match percentage calculation
- Missing-skill detection
- Resume improvement suggestions
- Analysis history
- Get analysis by ID
- Delete analysis
- Swagger API documentation
- SQLite persistence

## Run the project

### 1. Install

Install the .NET 8 SDK.

### 2. Open the project

```bash
cd AIResumeAnalyzer
```

### 3. Restore packages

```bash
dotnet restore
```

### 4. Run

```bash
dotnet run
```

The terminal will show a local URL. Open the `/swagger` URL in your browser.

Example:

```text
https://localhost:7xxx/swagger
```

## Test POST /api/Resume/analyze

Use this JSON:

```json
{
  "resumeText": "B.Tech student with experience in Python, Java, SQL, React, Git, REST APIs and Machine Learning. Built projects using FastAPI and MongoDB.",
  "jobDescription": "We are looking for a developer with C#, .NET, ASP.NET Core, SQL, REST API, Git, React and Docker experience."
}
```

Expected result will contain fields similar to:

```json
{
  "id": 1,
  "matchPercentage": 43,
  "matchedSkills": [
    "sql",
    "react",
    "git",
    "rest api"
  ],
  "missingSkills": [
    "c#",
    ".net",
    "asp.net core",
    "docker"
  ],
  "suggestions": [
    "Consider highlighting experience with: c#, .net, asp.net core, docker."
  ]
}
```

## API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/Resume/analyze` | Analyze resume against job description |
| GET | `/api/Resume/history` | Get previous analyses |
| GET | `/api/Resume/{id}` | Get one analysis |
| DELETE | `/api/Resume/{id}` | Delete an analysis |

## Architecture

```text
Client / Swagger
       |
       v
ResumeController
       |
       v
IResumeAnalyzerService
       |
       v
ResumeAnalyzerService
       |
       v
Entity Framework Core
       |
       v
SQLite Database
```

## How to make it more impressive later

For a second version, replace the rule-based `SkillDictionary` matching with an LLM such as Azure OpenAI or Gemini.

Possible additions:

- JWT authentication
- React frontend
- PDF resume upload
- Azure OpenAI integration
- PostgreSQL / SQL Server
- Skill extraction using embeddings
- ATS keyword scoring
- Docker deployment
- Azure App Service deployment

## Resume bullet

**AI Resume Analyzer | C#, ASP.NET Core, Entity Framework Core, SQLite, REST API**

- Developed an ASP.NET Core Web API that analyzes resumes against job descriptions using automated skill extraction and keyword matching.
- Implemented RESTful APIs, dependency injection, LINQ-based analysis, Entity Framework Core persistence, CRUD operations, and Swagger/OpenAPI documentation.
- Generated match scores, missing-skill analysis, and actionable resume improvement suggestions with persistent analysis history.
