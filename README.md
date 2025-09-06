ConfigRag
=========

A **Retrieval-Augmented Generation (RAG)** system for tenant configurations.\
You can store tenant configs in **Qdrant**, embed them with **Ollama**, and ask natural language questions via an API.

Two implementations are provided:

-   **.NET 9 (ASP.NET Core)** → `dotnet/`

-   **Node.js (Express)** → `node/`

* * * * *

📦 Tech Stack
-------------

-   **Qdrant** (vector database)

-   **Ollama** (local LLM + embeddings)

-   **nomic-embed-text** for embeddings

-   **llama3** for chat

-   **.NET 9** or **Node.js 18+** for API

* * * * *

⚙️ Setup (Common)
-----------------

### 1\. Run Qdrant

`docker run -p 6333:6333 qdrant/qdrant`

Qdrant UI: <http://localhost:6333/dashboard>

### 2\. Run Ollama

`ollama pull nomic-embed-text
ollama pull llama3`

Ollama API: <http://localhost:11434>

* * * * *

🚀 API Endpoints
----------------

Both implementations expose the same API:

### **Upsert Tenant Config**

`POST /api/tenants/upsert
Content-Type: application/json

{
  "tenantId": "TenantA",
  "config": {
    "region": "us-east",
    "billing": "enterprise",
    "features": ["chat", "reporting"]
  }
}`

### **Ask a Question**

`POST /api/tenants/ask
Content-Type: application/json

{
  "question": "Which tenants use enterprise billing?"
}`

Response:

`{
  "question": "Which tenants use enterprise billing?",
  "answer": "TenantA uses enterprise billing.",
  "matches": [
    {
      "tenantId": "TenantA",
      "config": {
        "region": "us-east",
        "billing": "enterprise",
        "features": ["chat", "reporting"]
      },
      "score": 0.95
    }
  ]
}`

* * * * *

🏗 Project Structure
--------------------

`ConfigRag/
├── dotnet/              # ASP.NET Core implementation
│   ├── Controllers/
│   ├── Services/
│   └── Program.cs
├── node/                # Node.js implementation
│   ├── index.js
│   └── services/
│       ├── ollama.js
│       └── qdrant.js
└── README.md`

* * * * *

▶️ Running
----------

### .NET (ASP.NET Core)

`cd dotnet
dotnet run`

Runs on: `http://localhost:5000`

### Node.js (Express)

`cd node
node index.js`

Runs on: `http://localhost:5001`

* * * * *

⚡ How It Works
--------------

1.  **Upsert Tenant**

    -   Tenant config → embedded via Ollama → stored in Qdrant.

    -   Stored with deterministic GUID for stable IDs.

    -   Payload includes tenantId + config (JSON).

2.  **Ask Question**

    -   Question → embedded via Ollama.

    -   Qdrant searches configs by similarity.

    -   Top matches → passed as context to Ollama.

    -   Ollama answers based on tenant configs.

* * * * *

🔧 Notes
--------

-   Configs are stored in payload as JSON string (easy to serialize/deserialize).

-   Extra fields (region, billing) also stored as separate payload keys → useful for filtering/searching in Qdrant.

-   Ollama is local-first, but you can replace with OpenAI, Anthropic, etc. by swapping the embedding/chat service.

* * * * *

🛠️ Future Enhancements
-----------------------

-   Store features as structured arrays instead of JSON.

-   Add `/search` endpoint for raw vector lookups.

-   Add authentication (API keys or JWT).

-   Deploy Qdrant to cloud (Qdrant Cloud, AWS, Azure).