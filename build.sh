dotnet publish src/HuaJiBot.NET.CLI \
        -c Release \
        --no-self-contained \
        -p:PublishSingleFile=true \
        --framework net10.0 \
        -o /root/out