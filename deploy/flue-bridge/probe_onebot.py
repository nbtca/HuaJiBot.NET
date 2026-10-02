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


def probe(config_path, group_id):
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
        return {"robotId": robot_id, "groupId": str(group["group_id"]), "role": member.get("role"),
                "muteUntil": member.get("shut_up_timestamp", 0)}


if __name__ == "__main__":
    try:
        print(json.dumps(probe(sys.argv[1], sys.argv[2])))
    except Exception as error:
        print("OneBot probe failed: " + type(error).__name__, file=sys.stderr)
        sys.exit(1)
