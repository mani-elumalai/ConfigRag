import { QdrantClient } from "@qdrant/js-client-rest";

export class QdrantService {
  constructor(collection = "tenants") {
    this.collection = collection;
    this.client = new QdrantClient({ url: "http://localhost:6333" });
  }

  async ensureCollection() {
    try {
      await this.client.createCollection(this.collection, {
        vectors: { size: 768, distance: "Cosine" }
      });
      console.log(`Collection "${this.collection}" created`);
    } catch (err) {
      if (err.message.includes("Conflict")) {
        console.log(`Collection "${this.collection}" already exists`);
      } else {
        throw err;
      }
    }
  }

  async upsert(tenantId, vector, payload) {
    await this.client.upsert(this.collection, {
      points: [
        {
          id: tenantId,
          vector,
          payload
        }
      ]
    });
  }

  async search(vector, limit = 3) {
    const res = await this.client.search(this.collection, {
      vector,
      limit
    });
    return res;
  }
}
