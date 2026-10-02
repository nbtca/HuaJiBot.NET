# Build context contains linux-x64 CLI publish output and the Release AIChat DLL.
# Overlay the current production image to preserve its other plugins and MCP services.
ARG BASE_IMAGE
FROM ${BASE_IMAGE}
COPY cli/ /app/bin/
COPY aichat/HuaJiBot.NET.Plugin.AIChat.dll /app/plugins/HuaJiBot.NET.Plugin.AIChat.dll
