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

FROM mcr.microsoft.com/dotnet/runtime:10.0-azurelinux3.0-distroless

ENV TZ=Asia/Shanghai

COPY --from=build-env /root/out /app/bin
COPY --from=build-env /root/build/bin/plugins /app/data/plugins

USER app
WORKDIR /app/data
ENTRYPOINT ["/app/bin/HuaJiBot.NET.CLI"]