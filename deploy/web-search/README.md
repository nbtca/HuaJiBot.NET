# AIChat web search MCP

The bot image contains a stdio MCP server at
`/app/mcp/web-search/HuaJiBot.NET.Mcp.WebSearch`. It exposes `web_search`
(SearXNG results) and `get_weather` (Open-Meteo geocoding and forecast).

Run SearXNG on the same Compose network as the bot. Mount `settings.yml` at
`/etc/searxng/settings.yml`, set a random `SEARXNG_SECRET`, and keep port 8080
internal to Compose. An example service is:

```yaml
  searxng:
    image: ghcr.io/searxng/searxng:2026.9.11-61d660276
    restart: unless-stopped
    environment:
      SEARXNG_SECRET: ${SEARXNG_SECRET}
    volumes:
      - ./searxng/settings.yml:/etc/searxng/settings.yml:ro
```

Set `SEARXNG_URL=http://searxng:8080` on the bot service and configure its
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
queries use the public Open-Meteo APIs and need no key. Search results include
original URLs. If SearXNG or its upstream engines fail, the tool reports no
results rather than inventing an answer.
