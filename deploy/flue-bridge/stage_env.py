"""Accept bridge secrets on stdin and stage a private container env file."""
import json
import os
import sys
from pathlib import Path

path = Path(sys.argv[1])
values = json.load(sys.stdin)
names = ("HUAJIBOT_BRIDGE_HMAC_SECRET", "HUAJIBOT_QUEUE_API_TOKEN")
if set(values) != set(names) or any(not isinstance(values[name], str) or not values[name]
                                  or any(c in values[name] for c in "\r\n\x00") for name in names):
    raise SystemExit("Invalid secret envelope")
if path.exists():
    raise SystemExit("Secret file already exists; refusing to overwrite")
fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, "w") as output:
    for name in names:
        output.write(name + "=" + values[name] + "\n")
    output.flush()
    os.fsync(output.fileno())
print("Bridge environment staged with mode 0600; values hidden.")
