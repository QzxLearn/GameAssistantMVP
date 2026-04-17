import httpx
from config import LLAMA_SERVER_URL

SYSTEM_PROMPT = """You are a Slay the Spire game assistant.
Based on the current game state, provide the optimal card-playing advice.

The game state may include:
- Player HP / max HP / block / gold
- Current energy (current / max)
- Hand cards (name, cost, type)
- Enemy list (name, HP, intent)
- Current combat phase

Please respond in the following format:
Suggestion: <brief advice>
Reasoning: <why you recommend this>
"""


def build_prompt(game_state_json: str) -> str:
    return f"""Current game state:
{game_state_json}

Please provide card-playing advice:"""


class LLMClient:
    def __init__(self, base_url: str = LLAMA_SERVER_URL, timeout: float = 60.0):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout

    def get_suggestion(self, game_state_json: str) -> str:
        prompt = self._build_qwen3_prompt(game_state_json)
        payload = {
            "prompt": prompt,
            "n_predict": 512,
            "temperature": 0.7,
            "stop": ["</s>", "USER:", "ASSISTANT:"]
        }
        with httpx.Client(timeout=self.timeout) as client:
            response = client.post(f"{self.base_url}/completion", json=payload)
            response.raise_for_status()
            return response.json()["content"]

    def _build_qwen3_prompt(self, game_state_json: str) -> str:
        prompt = build_prompt(game_state_json)
        return (
            "<|im_start|>system\n"
            + SYSTEM_PROMPT
            + "<|im_end|>\n"
            + "<|im_start|>user\n"
            + prompt
            + "<|im_end|>\n"
            + "<|im_start|>assistant\n"
        )


class EmbeddingClient:
    """Phase 1 stub — full implementation in Phase 2."""

    def __init__(self, base_url: str = LLAMA_SERVER_URL, timeout: float = 60.0):
        self.base_url = base_url.rstrip("/")
        self.timeout = timeout

    def get_embedding(self, text: str) -> list[float]:
        # Phase 2 implementation
        raise NotImplementedError("Embedding is not yet available in Phase 1")
