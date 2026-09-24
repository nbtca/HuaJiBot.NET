# AIChat web search MCP

The bot image includes the stdio MCP server
`/app/mcp/web-search/HuaJiBot.NET.Mcp.WebSearch`. It exposes one tool,
`web_search`, which searches SearXNG and returns titles, snippets, original URLs,
search engines, and retrieval time.

The current deployment runs the bot with `network_mode: host`. Run SearXNG in
host networking too, bound only to loopback port 8088. Mount `settings.yml` at
`/etc/searxng/settings.yml` and set a random `SEARXNG_SECRET`:

```yaml
  searxng:
    image: ghcr.io/searxng/searxng:2026.9.11-61d660276
    restart: unless-stopped
    network_mode: host
    environment:
      SEARXNG_SECRET: ${SEARXNG_SECRET}
      GRANIAN_HOST: 127.0.0.1
      GRANIAN_PORT: "8088"
    volumes:
      - ./searxng/settings.yml:/etc/searxng/settings.yml:ro
```

Set `SEARXNG_URL=http://127.0.0.1:8088` on the bot service and configure the
AIChat plugin with this `McpServers` entry:

```json
{
  "Name": "WebSearch",
  "TransportType": 0,
  "Command": "/app/mcp/web-search/HuaJiBot.NET.Mcp.WebSearch",
  "Enabled": true,
  "Args": []
}
```

The MCP process inherits the bot container's network and environment. With the
tool connected, the AIChat agent decides whether it has enough reliable
knowledge to answer. It calls `web_search` for uncertain, obscure, or
potentially outdated information, including current facts and specific links.
Questions it can answer from stable knowledge do not need a search. No question
type bypasses the model to invoke search directly.

`Plugins.AIChat.DefaultWeatherCity` can supply a default location when a user
asks about local conditions without naming one. It is only context for the
model, not a separate weather tool.

The deployment asks SearXNG for Brave, Sogou, and Google CSE only. The supplied
settings load just those three engines, and each engine times out within 4
seconds. `web_search` sends one request and gives up after 6 seconds. Results
show the retrieval time and engine. Identifier queries omit pages that do not
contain the identifier.

When the search has no reliable result or fails, the agent should say that it
cannot verify the answer. Model tool choice and upstream search coverage are
probabilistic; check tool call logs when investigating a missed search. AI
requests time out after 90 seconds, and a concurrent mention gets a busy reply.
