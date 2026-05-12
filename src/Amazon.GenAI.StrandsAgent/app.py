"""
Strands Agent API — Hotel Reservation Agent backed by AgentCore Gateway MCP tools.
Run: python app.py
Endpoint: POST /chat  {"message": "..."}
"""
import os, json, requests
from flask import Flask, request, jsonify
from strands import Agent
from strands.tools.mcp import MCPClient
from mcp.client.streamable_http import streamablehttp_client

# --- Config ---
COGNITO_DOMAIN = os.environ.get("COGNITO_DOMAIN", "my-domain-j3jcehka")
COGNITO_CLIENT_ID = os.environ.get("COGNITO_CLIENT_ID", "chpnclmmcetk9qucmqknp8ncc")
COGNITO_CLIENT_SECRET = os.environ.get("COGNITO_CLIENT_SECRET", "27uokmc5uh3ngql21a4e3ipg9kcvrgo3nni6qcdq4o0c625165j")
COGNITO_SCOPE = os.environ.get("COGNITO_SCOPE", "HotelReservationGateway/genesis-gateway:invoke")
GATEWAY_URL = os.environ.get("GATEWAY_URL", "https://hotelreservationgateway-emlf34hk1m.gateway.bedrock-agentcore.us-east-1.amazonaws.com/mcp")
MODEL_ID = os.environ.get("MODEL_ID", "us.anthropic.claude-sonnet-4-20250514-v1:0")
PORT = int(os.environ.get("PORT", "5100"))

SYSTEM_PROMPT = """You are a hotel concierge for Octank Hotels with locations in Chicago, San Francisco, and London.
Help guests find special deals, check room availability, and book rooms.

RULES:
- The current year is 2026. Always assume the current year unless the guest specifies otherwise.
- If a guest says "next week" or "next month", calculate the actual dates yourself. Do not ask.
- If check-in date is given but not check-out, assume 1 night.
- If no location preference, show options across all 3 cities.
- If a guest says "cheapest" or "best deal", check deals first, then availability.
- Only ask for information you absolutely cannot infer. Minimize questions.
- When you have enough info to call a tool, call it immediately. Do not ask for confirmation before searching.
- Only confirm details before making a BOOKING (writing data), not before searching.
- Be concise, friendly, and professional.

CRITICAL — LOCATION HANDLING:
- When the guest specifies a location, ALL subsequent tool calls MUST use ONLY that location.
- Do NOT show results for other locations unless the guest explicitly asks to compare.
- If the guest says "Chicago", only call GetAvailableRooms with location="Chicago". Do NOT call it for San Francisco or London.
- If the guest says "book a room in London", only search and book in London.
- Treat the guest's location choice as a hard filter, not a suggestion.

CRITICAL — DATE HANDLING:
- Today is 2026-05-04. Use this to calculate all relative dates.
- Convert ALL natural language dates to YYYY-MM-DD format yourself. NEVER ask the user to provide dates in a specific format.
- "next Friday" → calculate the date and use it. "June 15" → "2026-06-15". "tomorrow" → "2026-05-05".
- "2 nights starting June 10" → checkIn="2026-06-10", checkOut="2026-06-12".
- If the guest already gave you dates, DO NOT ask for them again. Extract them from the conversation and convert to YYYY-MM-DD.
- If only check-in is given, assume 1 night and calculate check-out yourself."""

app = Flask(__name__)

_token_cache = {"token": None, "expires_at": 0}

def get_token():
    import time
    if _token_cache["token"] and time.time() < _token_cache["expires_at"] - 60:
        return _token_cache["token"]
    
    resp = requests.post(
        f"https://{COGNITO_DOMAIN}.auth.us-east-1.amazoncognito.com/oauth2/token",
        data={"grant_type": "client_credentials", "scope": COGNITO_SCOPE},
        auth=(COGNITO_CLIENT_ID, COGNITO_CLIENT_SECRET)
    )
    resp.raise_for_status()
    data = resp.json()
    _token_cache["token"] = data["access_token"]
    _token_cache["expires_at"] = time.time() + data.get("expires_in", 3600)
    return _token_cache["token"]


@app.route("/chat", methods=["POST"])
def chat():
    body = request.get_json()
    message = body.get("message", "")
    history = body.get("history", [])
    if not message:
        return jsonify({"error": "message is required"}), 400

    token = get_token()
    mcp_client = MCPClient(lambda: streamablehttp_client(
        url=GATEWAY_URL,
        headers={"Authorization": f"Bearer {token}"}
    ))

    with mcp_client:
        tools = mcp_client.list_tools_sync()
        agent = Agent(
            model=MODEL_ID,
            tools=tools,
            system_prompt=SYSTEM_PROMPT
        )
        # Load conversation history so the agent has context
        for msg in history:
            agent.messages.append({"role": msg["role"], "content": [{"type": "text", "text": msg["content"]}]})
        try:
            result = agent(message)
            return jsonify({"response": str(result)})
        except Exception as e:
            import traceback
            traceback.print_exc()
            return jsonify({"error": str(e), "type": type(e).__name__}), 500


@app.route("/tools", methods=["GET"])
def list_tools():
    token = get_token()
    mcp_client = MCPClient(lambda: streamablehttp_client(
        url=GATEWAY_URL,
        headers={"Authorization": f"Bearer {token}"}
    ))
    with mcp_client:
        tools = mcp_client.list_tools_sync()
        tool_list = [{"name": t.tool_name, "description": getattr(t, 'description', str(t))} for t in tools]
        return jsonify({"count": len(tool_list), "tools": tool_list})


@app.route("/health", methods=["GET"])
def health():
    return jsonify({"status": "ok"})


if __name__ == "__main__":
    app.run(host="0.0.0.0", port=PORT, debug=True)
