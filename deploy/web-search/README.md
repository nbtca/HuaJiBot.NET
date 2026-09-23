# AIChat web search MCP

The bot image contains a stdio MCP server at
`/app/mcp/web-search/HuaJiBot.NET.Mcp.WebSearch`. It exposes `web_search`
(SearXNG results) and `get_weather` (Open-Meteo geocoding and forecast).

The current deployment runs the bot with `network_mode: host`. Run SearXNG in
host networking too, bound only to loopback port 8088. The server's default
bridge networking could not reach upstream search engines during deployment.
Mount `settings.yml` at `/etc/searxng/settings.yml` and set a random
`SEARXNG_SECRET`. An example service is:

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

Set `SEARXNG_URL=http://127.0.0.1:8088` on the bot service and configure its
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

The MCP process inherits the bot container's network and environment. Weather
queries use the public Open-Meteo APIs and need no key. Explicit GitHub
repository searches and standalone project names such as `HuaJiBot.NET` use
GitHub's public repository search API first; if that request fails or finds no
matching repository, they fall back to SearXNG.
Other web searches use SearXNG. Search results include original URLs. If all
applicable sources fail, the tool reports no results rather than inventing an
answer.

In an enabled group, messages starting with `@Bot 联网搜索` call the MCP
`web_search` tool directly and send its results without an LLM step. Other
questions can still use the MCP tools through AIChat's agent.
