FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0 AS build-env

COPY . /root/build

ARG TARGETARCH

WORKDIR /root/build

RUN dotnet publish src/HuaJiBot.NET.CLI \
        -c Release \
        -a $TARGETARCH \
        --no-self-contained \
        -p:PublishSingleFile=true \
        --framework net10.0 \
        -o /root/out

RUN dotnet fsi build_plugins.fsx

RUN dotnet publish src/HuaJiBot.NET.Mcp.WebSearch \
        -c Release \
        -a $TARGETARCH \
        --no-self-contained \
        --framework net10.0 \
        -o /root/web-search-mcp

FROM mcr.microsoft.com/dotnet/runtime:10.0-azurelinux3.0-distroless

ENV TZ=Asia/Shanghai
ENV HUAJIBOT_PLUGIN_DIR=/app/plugins

COPY --from=build-env /root/out /app/bin
COPY --from=build-env /root/build/bin/plugins /app/plugins
COPY --from=build-env /root/web-search-mcp /app/mcp/web-search

USER app
WORKDIR /app/data
ENTRYPOINT ["/app/bin/HuaJiBot.NET.CLI"]
