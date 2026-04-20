# HuaJiBot.NET.Adapter.Telegram

This adapter allows HuaJiBot.NET to connect to Telegram using the official Telegram Bot API.

## Features

- Send and receive text messages
- Send images
- Reply to messages with proper threading
- Mention users with proper Telegram user links
- Support for both group chats and private messages
- Handle various message types (photos, documents, stickers, etc.)
- Delete messages (recall functionality)
- Set group chat titles
- Get user member permissions
- HTML formatting support for rich text
- **Automatic bot command registration** - Commands from plugins are automatically registered with Telegram Bot API

## Setup

1. Create a new bot using [@BotFather](https://t.me/BotFather) on Telegram
2. Get your bot token from BotFather
3. Initialize the adapter in your HuaJiBot.NET application:

```csharp
using HuaJiBot.NET.Adapter.Telegram;

// Create the Telegram adapter with your bot token
var telegramAdapter = new TelegramAdapter("YOUR_BOT_TOKEN_HERE")
{
    Logger = yourLoggerInstance
};

// Setup and start the service
await telegramAdapter.SetupServiceAsync();
```

## Configuration

The TelegramAdapter requires only a bot token to function. The bot token is obtained from [@BotFather](https://t.me/BotFather) when you create a new bot.

### Automatic Command Registration

When plugins are loaded, the adapter automatically collects all commands and registers them with the Telegram Bot API using the `setMyCommands` method. This makes commands visible to users in the Telegram UI.

**Command Requirements:**
- Command names must contain only lowercase letters (a-z), digits (0-9), and underscores (_)
- Maximum length: 32 characters
- Commands with Chinese or other non-ASCII characters in names will be skipped
- Command descriptions are limited to 256 characters

The adapter will log debug messages for any commands that are skipped due to invalid names.

## Message Types

- **TextMessage**: Sent as regular text with HTML formatting support
- **ImageMessage**: Sent as photos
- **AtMessage**: Converted to proper Telegram user mentions using `tg://user?id=` format
- **ReplyMessage**: Uses Telegram's reply-to-message functionality

## Limitations

- Some message types (like voice messages, files) are displayed as text descriptions
- User nickname retrieval is simplified due to Telegram API limitations
- Group member management is limited to basic permission checking

## Dependencies

- `Telegram.Bot` - Official Telegram Bot API client
- `HuaJiBot.NET` - Core bot framework