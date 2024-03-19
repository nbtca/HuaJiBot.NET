# docker buildx build -t huajibot --platform linux/arm64 -t huajibot_arm64 -o bin .
docker buildx build -t huajibot --platform linux/arm64 --load .
docker save -o huajibot.tar huajibot
