import os

LLAMA_SERVER_URL = os.getenv("LLAMA_SERVER_URL", "http://localhost:8080")
LLM_MODEL_PATH = os.getenv(
    "LLM_MODEL_PATH",
    "/home/qzx/models/Qwen_Qwen3-8B-GGUF_Qwen3-8B-Q4_K_M.gguf"
)
EMBEDDING_MODEL_PATH = os.getenv(
    "EMBEDDING_MODEL_PATH",
    "/home/qzx/models/Qwen_Qwen3-Embedding-0.6B-GGUF_Qwen3-Embedding-0.6B-Q8_0.gguf"
)
BRAIN_DB = os.getenv("BRAIN_DB", "./brain.db")
BRAIN_PORT = int(os.getenv("BRAIN_PORT", "8000"))
