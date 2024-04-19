FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.18 AS build-env

COPY . /root/build

ARG TARGETARCH

WORKDIR /root/build
RUN dotnet publish src/HuaJiBot.NET.CLI \
        -c Release \
        -a $TARGETARCH \
        --no-self-contained \
        -p:PublishSingleFile=true \
        --framework net8.0 \
        -o /root/out

FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine3.18

ENV TZ=Asia/Shanghai

COPY --from=build-env /root/out/HuaJiBot.NET.CLI /app/bin/HuaJiBot.NET.CLI

RUN mkdir /app/data \
 && adduser -D user \
 && chmod +x /app/bin/HuaJiBot.NET.CLI

WORKDIR /app/data
ENTRYPOINT ["/app/bin/HuaJiBot.NET.CLI"]