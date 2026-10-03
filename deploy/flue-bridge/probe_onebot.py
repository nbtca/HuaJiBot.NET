"""Read the configured OneBot account/group without printing credentials or messages."""
import base64
import hashlib
import json
import os
import socket
import ssl
import struct
import sys
import urllib.parse
from pathlib import Path


def probe(config_path, group_id, history_output=None, since=0):
    config = json.loads(Path(config_path).read_text())
    endpoint = urllib.parse.urlsplit(config["OneBot"]["Url"])
    if endpoint.scheme not in ("ws", "wss"):
        raise RuntimeError("Expected a OneBot WebSocket endpoint")
    port = endpoint.port or (443 if endpoint.scheme == "wss" else 80)
    connection = socket.create_connection((endpoint.hostname, port), timeout=10)
    if endpoint.scheme == "wss":
        connection = ssl.create_default_context().wrap_socket(connection, server_hostname=endpoint.hostname)
    with connection:
        key = base64.b64encode(os.urandom(16)).decode()
        path = endpoint.path or "/"
        if endpoint.query:
            path += "?" + endpoint.query
        lines = [f"GET {path} HTTP/1.1", f"Host: {endpoint.hostname}:{port}", "Upgrade: websocket",
                 "Connection: Upgrade", f"Sec-WebSocket-Key: {key}", "Sec-WebSocket-Version: 13"]
        token = config["OneBot"].get("Token")
        if token:
            lines.append("Authorization: Bearer " + token)
        connection.sendall(("\r\n".join(lines) + "\r\n\r\n").encode())
        buffered = bytearray()
        while b"\r\n\r\n" not in buffered:
            chunk = connection.recv(4096)
            if not chunk:
                raise RuntimeError("OneBot handshake closed")
            buffered.extend(chunk)
            if len(buffered) > 65536:
                raise RuntimeError("OneBot handshake too large")
        raw_headers, remaining = bytes(buffered).split(b"\r\n\r\n", 1)
        expected = base64.b64encode(hashlib.sha1((key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").encode()).digest()).decode()
        if b" 101 " not in raw_headers.split(b"\r\n", 1)[0] or expected.lower().encode() not in raw_headers.lower():
            raise RuntimeError("OneBot rejected the authenticated handshake")
        buffered = bytearray(remaining)

        def read_exact(count):
            while len(buffered) < count:
                chunk = connection.recv(max(4096, count - len(buffered)))
                if not chunk:
                    raise RuntimeError("OneBot connection closed")
                buffered.extend(chunk)
            result = bytes(buffered[:count])
            del buffered[:count]
            return result

        def send(payload, opcode=1):
            mask = os.urandom(4)
            size = len(payload)
            header = bytes([0x80 | opcode, 0x80 | size]) if size < 126 else bytes([0x80 | opcode, 0xFE]) + struct.pack("!H", size)
            connection.sendall(header + mask + bytes(byte ^ mask[index % 4] for index, byte in enumerate(payload)))

        def call(action, params):
            echo = os.urandom(8).hex()
            send(json.dumps({"action": action, "params": params, "echo": echo}).encode())
            fragments = bytearray()
            for _ in range(200):
                first, second = read_exact(2)
                opcode, size = first & 15, second & 127
                if size == 126:
                    size = struct.unpack("!H", read_exact(2))[0]
                elif size == 127:
                    size = struct.unpack("!Q", read_exact(8))[0]
                if size > 1024 * 1024:
                    raise RuntimeError("OneBot frame too large")
                mask = read_exact(4) if second & 128 else None
                payload = read_exact(size)
                if mask:
                    payload = bytes(byte ^ mask[index % 4] for index, byte in enumerate(payload))
                if opcode == 9:
                    send(payload, 10)
                    continue
                if opcode == 8:
                    raise RuntimeError("OneBot closed the connection")
                if opcode not in (0, 1):
                    continue
                fragments.extend(payload)
                if not first & 128:
                    continue
                response = json.loads(fragments)
                fragments.clear()
                if response.get("echo") == echo:
                    if response.get("status") != "ok" or response.get("retcode", 0) != 0:
                        raise RuntimeError("OneBot query failed: " + action)
                    return response["data"]
            raise RuntimeError("No matching OneBot query response")

        login = call("get_login_info", {})
        robot_id = str(login["user_id"])
        group = call("get_group_info", {"group_id": int(group_id)})
        member = call("get_group_member_info", {"group_id": int(group_id), "user_id": int(robot_id)})
        if history_output:
            records = {}
            cursor = None
            covered = False
            for _ in range(10):
                params = {"group_id": int(group_id), "count": 100}
                if cursor is not None:
                    params["message_seq"] = cursor
                data = call("get_group_msg_history", params)
                messages = data.get("messages", [])
                if not messages:
                    covered = True
                    break
                covered = min(int(item.get("time", 0)) for item in messages) <= since
                for item in messages:
                    timestamp = int(item.get("time", 0))
                    if timestamp < since or str(item.get("user_id")) == robot_id:
                        continue
                    message_id = str(item["message_id"])
                    segments = item.get("message", [])
                    text = "".join(segment.get("data", {}).get("text", "") for segment in segments if isinstance(segment, dict) and segment.get("type") == "text") if isinstance(segments, list) else "（非文本消息）"
                    sender = item.get("sender", {})
                    components = ["main", robot_id, str(group_id), message_id]
                    event_id = ":".join(urllib.parse.quote(value, safe="") for value in components)
                    records[message_id] = {"version": 1, "eventId": event_id, "conversationId": "huajibot:" + event_id,
                        "kind": "archive", "destination": {"bridgeInstance": "main", "robotId": robot_id, "groupId": str(group_id)},
                        "message": {"messageId": message_id, "senderId": str(item.get("user_id")), "senderName": sender.get("card") or sender.get("nickname") or "成员",
                                    "text": text or "（非文本消息）", "images": [], "observedAt": timestamp * 1000}, "replyTo": None}
                if covered:
                    break
                sequences = [int(item["message_seq"]) for item in messages if "message_seq" in item]
                if not sequences or cursor == min(sequences) - 1:
                    break
                cursor = min(sequences) - 1
            descriptor = os.open(history_output, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
            with os.fdopen(descriptor, "w") as output:
                for record in sorted(records.values(), key=lambda item: item["message"]["observedAt"]):
                    output.write(json.dumps(record, ensure_ascii=False) + "\n")
            return {"historyRecords": len(records), "coversMigrationWindow": covered}
        return {"robotId": robot_id, "groupId": str(group["group_id"]), "role": member.get("role"),
                "muteUntil": member.get("shut_up_timestamp", 0)}


if __name__ == "__main__":
    try:
        print(json.dumps(probe(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else None, int(sys.argv[4]) if len(sys.argv) > 4 else 0)))
    except Exception as error:
        print("OneBot probe failed: " + type(error).__name__, file=sys.stderr)
        sys.exit(1)
