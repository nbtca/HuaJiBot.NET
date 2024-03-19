dotnet publish src/HuaJiBot.NET.CLI \
        -c Release \
        --no-self-contained \
        -p:PublishSingleFile=true \
        --framework net8.0 \
        -o /root/out