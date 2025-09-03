dotnet publish src/HuaJiBot.NET.CLI \
        -c Release \
        --no-self-contained \
        -p:PublishSingleFile=true \
        --framework net9.0 \
        -o /root/out