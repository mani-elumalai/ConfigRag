import express from "express";
import bodyParser from "body-parser";
import { OllamaService } from "./services/ollama.js";
import { QdrantService } from "./services/qdrant.js";

const app = express();
app.use(bodyParser.json());

const ollama = new OllamaService();
const qdrant = new QdrantService("tenants");

// Ensure collection at startup
qdrant.ensureCollection().then(() => console.log("Qdrant ready"));

app.post("/api/tenants/upsert", async (req, res) => {
  try {
    const { tenantId, config } = req.body;

    const textConfig = JSON.stringify(config);
    const embedding = await ollama.embed(textConfig);

    await qdrant.upsert(tenantId, embedding, { tenantId, config });

    res.json({ message: "Tenant config upserted" });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

app.post("/api/tenants/ask", async (req, res) => {
  try {
    const { question } = req.body;

    const qVector = await ollama.embed(question);
    const matches = await qdrant.search(qVector, 3);

    const context = matches
      .map(m => `Tenant ${m.payload.tenantId}: ${JSON.stringify(m.payload.config)}`)
      .join("\n");

    const answer = await ollama.chat(context, question);

    res.json({
      question,
      answer,
      matches
    });
  } catch (err) {
    console.error(err);
    res.status(500).json({ error: err.message });
  }
});

app.listen(5001, () => console.log("Node API running at http://localhost:5001"));
