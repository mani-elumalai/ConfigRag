import axios from "axios";

const OLLAMA_URL = "http://localhost:11434";

export class OllamaService {
  async embed(text) {
    const res = await axios.post(`${OLLAMA_URL}/api/embed`, {
      model: "nomic-embed-text",
      input: text
    });
    return res.data.embeddings[0]; // embedding vector
  }

  async chat(context, question) {
    const res = await axios.post(`${OLLAMA_URL}/api/chat`, {
      model: "llama3",
      messages: [
        { role: "system", content: `Use this tenant context:\n${context}` },
        { role: "user", content: question }
      ]
    });

    return res.data.message?.content || "";
  }
}
