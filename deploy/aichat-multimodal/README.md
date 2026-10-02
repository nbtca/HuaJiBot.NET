# AIChat image input

The OneBot adapter exposes image attachments through `GroupMessageEventArgs.ImageUrls`.
Typed URLs remain text. AIChat downloads attachments and sends validated image bytes as
Chat Completions image blocks. This first implementation covers OneBot/QQ; other adapters
can populate the same optional event field. PR #65 and its KOOK adapter remain separate.

Under `Plugins.AIChat`, set `SupportsVision: true` only for a model with verified image
input support. `agnes-3.0-flash` and `agnes-2.5-flash` document image understanding;
`agnes-image-2.5-flash` is an image generation endpoint, not a chat model.
Defaults: `MaxImagesPerRequest: 4`, `MaxImageBytes: 8388608` (per image).
Requests include referenced historical images in the count. Old history remains readable.
Visual requests use reconstructed reply history and short-lived agent sessions, so image
bytes do not accumulate in the per-group session cache.

Users can send `@bot <question> + image`, `@bot + image` (default image description),
or reply to a bot response with a follow-up question. Images from earlier AI exchanges
are restored from history. Replying to an arbitrary image that never reached AIChat is
not supported. Expired image URLs require resending the image.

Only public HTTP(S) attachment addresses are downloaded. Redirects and private-network
destinations are rejected. Download time is capped at 20 seconds and the complete request
at 90 seconds. PNG, JPEG, WebP and GIF are supported. Downloads are held in memory;
history stores source URLs, not Base64. AI request logs report counts only, and SDK
content logging is disabled when vision is enabled. An unsupported model, failed image
download, invalid format or exceeded limit produces an explicit user-facing message.

## Validation

```sh
dotnet test src/HuaJiBot.NET.UnitTest --filter FullyQualifiedName~VisionInputTest
```

Tests cover OneBot parsing, real image content and OpenAI-compatible serialization,
text-only behavior, capability gating, historical image limits, legacy history, invalid
URLs, network address checks, download failure and content validation. Real acceptance
requires a non-bot account to send a picture and mention in the configured QQ group,
then confirm the response describes details visible only in the picture and that a
reply-based follow-up retains the image context.

## Production overlay deployment

Following Issue #38, stop the bot before changing configuration. Back up `compose.yaml`,
`config.json` and plugin data; tag the current image for rollback. Do not restart other
Compose services. Build a Linux CLI and the Release AIChat plugin from the same commit:

```sh
dotnet publish src/HuaJiBot.NET.CLI -c Release -f net10.0 -r linux-x64 \
  --no-self-contained -p:PublishSingleFile=true -o /tmp/huajibot-vision/cli
dotnet build src/HuaJiBot.NET.Plugin.AIChat -c Release -o /tmp/huajibot-vision/aichat
docker build --build-arg BASE_IMAGE=<current-production-image> \
  -f deploy/aichat-multimodal/Dockerfile -t <new-image> /tmp/huajibot-vision
```

The overlay assumes production already includes this branch's AIChat dependencies
(Microsoft.Agents.AI 1.1.0 and Microsoft.Extensions.AI.OpenAI 10.4.1). Verify versions
before using it; if dependencies differ, build the full image or update dependencies
explicitly. It preserves the current PushPanel and other plugin binaries.

With the bot stopped, enable vision, change only its Compose image and start:
`docker compose up -d --no-deps huaji-bot-dotnet`. Verify startup, OneBot connection,
plugin loading, real image response, historical follow-up, text-only response and
existing panel behavior. Compare configuration against backup excluding the new vision
fields. Roll back by restoring the backed-up Compose file and config while stopped,
then start only the bot. Preserve the pre-deployment plugin data backup for recovery;
do not overwrite new chat history unless recovery specifically requires it.
