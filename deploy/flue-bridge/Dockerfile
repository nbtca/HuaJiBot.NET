# Overlay the current production image; preserve its other plugins and MCP service.
ARG BASE_IMAGE
FROM ${BASE_IMAGE}
COPY cli/ /app/bin/
COPY --chmod=755 cli/HuaJiBot.NET.CLI /app/bin/HuaJiBot.NET.CLI
COPY bridge/HuaJiBot.NET.Plugin.FlueBridge.dll /app/plugins/HuaJiBot.NET.Plugin.FlueBridge.dll
COPY dependencies/ /app/plugins/libs/
