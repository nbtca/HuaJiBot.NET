# Flue / Cloudflare Queue bridge (issue #64)

HuaJiBot.NET forwards messages to the separate `huaji-agent` Worker over signed HTTPS and pulls replies from Cloudflare Queue. The bridge contains no model or Flue agent implementation. OneBot, Satori, and Telegram keep their existing adapter APIs. ServerlessMQ is unchanged.

The Worker and primary Queue/DLQ were deployed to `nbtca@protonmail.com` on 2026-10-02. The bridge is enabled for robot `3623498320`, group `466691612`; that group was removed from the old AIChat plugin. The local DailySummary runtime is now disabled; its original database/configuration and other services are preserved. A signed synthetic request produced a real model reply through Queue; group delivery and reply-thread acceptance await user testing. See `deployment.json` for the deployed resources and validation boundaries.

Agnes 3.0 Flash is primary. Text fallback is Cloudflare Llama 3.3; image threads use Cloudflare Llama 4 Scout. Rate limits and explicit exhausted allowances advance through configured Agnes keys, then Cloudflare.

On the current Windows machine, .NET SDK 10.0.401 was installed at `%USERPROFILE%\.codex\dependencies\dotnet`. If an existing terminal still resolves the older system runtime, prepend that directory to its `PATH` (or invoke its `dotnet.exe` directly) before building. The generated shared HMAC secret is stored in the user's `HUAJIBOT_BRIDGE_HMAC_SECRET` environment variable and Cloudflare's Worker secret store; inject it into the bot's deployment environment without putting it in configuration/Git.

## Worker feature migration (2026-10-03)

The Worker owns D1 archiving, UTC+8 daily scheduling, durable summary generation, WebSearch MCP, image downloads and reply-thread vision. The bot retains platform adapters, message receipts, scheduled-summary anchors and a durable forwarding outbox. Local AIChat and DailySummary are disabled; the old MCP/SearXNG artifacts are retained for rollback and are no longer required by the Agent.

`总结` / `summary` accepts 今天、昨天、前天 (default yesterday). The Worker preserves the ten-message threshold, short fact-based Chinese brief, ten-second sender cooldown and per-group concurrency guard. Cron starts yesterday's summary at 00:00 Beijing time. `kind: summary` jobs use a validated local date anchor for scheduled posts, without asking the platform to reply to a nonexistent message ID.

`activate-features.sh` builds an overlay and disables only local DailySummary/AIChat. Its first migration backup is `/home/yunacelisse/stacks/huajibot/backups/worker-features-20261003T052735Z`; the final image update backup is `worker-features-20261003T054443Z`. `history-export` reads the backed-up LiteDB in read-only mode. 494 recent records were imported idempotently into the Worker's D1 database. Private exports remain outside Git with mode 0600 on the server.

Rollback the whole migration by disabling `SUMMARY_ENABLED` and revoking the Worker destination before restoring config/compose from the first migration backup. Preserve the original LiteDB files, bridge SQLite receipts and D1 audit data. Restore the old Worker version if needed; do not rewrite deployed migrations.

Verification passed: 12 bridge tests, 23 Worker tests, real MCP search, native image recognition and image follow-up, D1 archival/import, Workflow summary execution and Queue replies. Tests used an isolated destination and sent no synthetic production-group message. User acceptance should include `总结 今天`, an image mention, an image follow-up and a live search question in group 466691612.

## Configuration and secrets

Merge `config.example.json` into the bot's `Plugins` configuration. Enable only after configuring the independent Worker, its primary Queue and DLQ, and a canary destination. `Destinations` is an exact robot/group allowlist. Telegram `chatId:topicId` is an opaque group value.

Keep the canary group out of AIChat's `GroupIds`. The bridge refuses startup if an enabled AIChat configuration overlaps. Stop and restart the bot after changing routing or secrets; live panel edits do not reinitialize this plugin.

Supply these environment variables to the bot container/process:

- `HUAJIBOT_BRIDGE_HMAC_SECRET`: the same secret configured on the Worker.
- `HUAJIBOT_QUEUE_API_TOKEN`: an account-scoped Cloudflare token with Queue read/write permission, limited to the intended account.

Do not put either secret in plugin configuration or Git. The Worker must independently allowlist the same bridge instance, robot and group. The bot needs outbound HTTPS only; no public listener or Tunnel is used.

## Version 1 wire contract

Bot records in `src/HuaJiBot.NET.Plugin.FlueBridge/Contracts.cs` and Worker schemas in `huaji-agent/src/contracts.ts` use the ingress and reply shapes from issue #64. All property names on the wire are camelCase. Optional reply context is represented by `replyTo: null`; available sender/content fields may also be null.

Both sides use RFC3986 component encoding when forming identifiers. This avoids delimiter collisions with Telegram topics. For example:

```text
conversationId = huajibot:main:bot:-100%3A42:root
eventId = main:bot:-100%3A42:root
```

`eventId` includes robot and group because platform message IDs can be scoped to a chat. Redelivery uses exactly the same ID and payload. A new mention creates a conversation; a reply to a mapped user or bot message continues it. An unknown reply without a direct mention is archived without invoking a model. Optional `kind` is chat/archive/summary; `message.images` contains native adapter image URLs and `message.observedAt` is UTC epoch milliseconds. The Worker downloads approved QQ images and stores native attachments in the conversation.

Ingress headers:

```text
X-HuaJiBot-Timestamp: <Unix seconds>
X-HuaJiBot-Signature: v1=<lowercase HMAC-SHA256 hex>
signed bytes = UTF8("v1:" + timestamp + ":") || exact HTTP body bytes
```

The Worker accepts timestamps within 300 seconds and uses `eventId` as Flue's durable dispatch idempotency key. Timestamp is included in the MAC to prevent changing the replay window. Signed requests do not follow redirects.

Replies are JSON strings published with Queue `contentType: 'text'`; `json`/`bytes` content would be Base64-encoded by HTTP pull. The consumer validates the version, destination allowlist, Markdown format, UTF-8 size, and the conversation mapping of `replyToMessageId` before sending. Invalid jobs are acknowledged as poison. Adapter failures explicitly retry with delay. Successful sends atomically save all returned message IDs and the sent ledger before acknowledgment.

Cloudflare HTTP pull's `visibility_timeout` is expressed in milliseconds; the plugin setting is in seconds. The plugin pulls one lease per batch to avoid expiring later leases while adapters render/send an earlier reply. It polls every second during the active window, gradually backs off to the idle interval, and wakes immediately after accepted ingress.

## Storage, build and rollback

The canary overlay image is `huajibot-local:worker-features-v2` on `/home/yunacelisse/stacks/huajibot`. `Dockerfile` preserves existing plugins while replacing the CLI and adding bridge dependencies. `activate.sh` checks artifact hashes, backs up config/compose/plugin data, stops only the bot, checks the intended configuration delta, and automatically restores the old config/image on startup failure. `bridge.env` is a private mode-0600 file. Supply sudo authentication through the operator's terminal; never save it in scripts.

The activation backup is `/home/yunacelisse/stacks/huajibot/backups/flue-canary-20261002T151502Z`. For a deployment rollback, stop only `huaji-bot-dotnet`, restore `config.json` and `compose.yaml` from that directory, and run `docker compose up -d --no-deps huaji-bot-dotnet`. Keep the bridge database and Queue. Revoke the Worker canary allowlist after the switch.

The plugin stores `flue_bridge.db` in its normal plugin data directory. Preserve that directory across restarts and container replacement. The default 30-day retention should exceed the Queue's message retention. Old mappings and sent rows are pruned hourly. SQLite uses WAL, FULL synchronization, parameterized SQL, and atomic mapping/ledger commits.

`dotnet fsi build_plugins.fsx` includes SQLite's `runtimes` native assets in `plugins/libs/runtimes`. The bridge resolves its SQLite native library there when loaded from a plugin archive/container. Keep this directory when manually installing a plugin.

Build and test:

```powershell
dotnet build HuaJiBot.NET.slnx
dotnet test src/HuaJiBot.NET.UnitTest --filter FullyQualifiedName~FlueBridgeTest
dotnet fsi build_plugins.fsx
```

Canary acceptance must use real adapter traffic: mention, reply to each returned bot message ID, separate threads, and Telegram topic routing. Stop the bot after a reply is queued, restart it, and verify delivery. Verify duplicate `jobId` acknowledgment, expired leases, send failure retries and eventual DLQ receipt. Unit tests simulate deduplication, restart, ack loss, adapter failures, poison jobs, protocol signing and topic IDs; they do not prove real Cloudflare DLQ behavior or platform delivery.

Roll back by disabling FlueBridge, removing the canary destination from the Worker's allowlist, and restoring that group to AIChat. Keep the SQLite database and Queue for audit/recovery. Do not delete Queue messages or rewrite Durable Object migration history during rollback.

## Reliability boundaries

Cloudflare delivery is at least once. A crash after adapter acceptance but before recording the returned receipt can produce a duplicate reply. If an adapter partially sends multiple messages and then fails, retrying can also duplicate the successful portion. There is no shared provider-side idempotency key.

Inbound events are committed to `ingress_outbox` before entering the bounded channel. The original payload/event ID survives restart and retries; rows are removed only after Worker admission. Messages the platform never delivers while the bot is offline cannot be recovered by this outbox. Queue replies survive downtime according to Queue retention.

References: [issue #64](https://github.com/nbtca/HuaJiBot.NET/issues/64), [Cloudflare HTTP pull](https://developers.cloudflare.com/queues/configuration/pull-consumers/), [Flue channels](https://flueframework.com/docs/guide/channels/).
