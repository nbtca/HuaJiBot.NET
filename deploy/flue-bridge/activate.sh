#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

STACK=/home/yunacelisse/stacks/huajibot
STAGE="$STACK/deploy/flue-canary"
IMAGE=huajibot-local:flue-canary-onebot-fix
STATUS="$STAGE/activation-status.json"
BACKUP="$STACK/backups/flue-canary-$(date -u +%Y%m%dT%H%M%SZ)"
STOPPED=0
SWITCHED=0

if [[ $EUID -ne 0 ]]; then
    echo 'Run this script with sudo in your own terminal.' >&2
    exit 1
fi

write_status() {
    python3 - "$STATUS" "$1" "$BACKUP" "$IMAGE" <<'PY'
import datetime,json,os,sys
path,status,backup,image=sys.argv[1:]
with open(path+'.tmp','w') as f:
    json.dump({'status':status,'backup':backup,'image':image,
               'updatedAt':datetime.datetime.now(datetime.timezone.utc).isoformat()},f)
os.chmod(path+'.tmp',0o600)
os.chown(path+'.tmp',1000,1000)
os.replace(path+'.tmp',path)
PY
}

rollback() {
    local result=$?
    trap - ERR
    if [[ $STOPPED -eq 1 ]]; then
        docker compose -f "$STACK/compose.yaml" stop huaji-bot-dotnet || true
        cp -a "$BACKUP/config.json" "$STACK/config.json"
        cp -a "$BACKUP/compose.yaml" "$STACK/compose.yaml"
        docker compose -f "$STACK/compose.yaml" up -d --no-deps huaji-bot-dotnet || true
        write_status rolled_back
        echo "Activation failed; previous configuration/image restored. Backup: $BACKUP" >&2
    else
        write_status failed_before_switch
        echo 'Activation failed before touching the running bot.' >&2
    fi
    exit "$result"
}
trap rollback ERR

cd "$STACK"
test -s "$STAGE/bridge.env"
test "$(stat -c %a "$STAGE/bridge.env")" = 600
test -s "$STAGE/cli/HuaJiBot.NET.CLI"
test -s "$STAGE/bridge/HuaJiBot.NET.Plugin.FlueBridge.dll"
test -s "$STAGE/dependencies/runtimes/linux-x64/native/libe_sqlite3.so"
(cd "$STAGE" && sha256sum -c SHA256SUMS)
write_status building

BASE_ID=$(docker inspect huajibot --format '{{.Image}}')
BASE=$(python3 - "$STACK/compose.yaml" <<'PY'
import sys,yaml
print(yaml.safe_load(open(sys.argv[1]))['services']['huaji-bot-dotnet']['image'])
PY
)
test "$(docker image inspect "$BASE" --format '{{.Id}}')" = "$BASE_ID"
docker build --build-arg "BASE_IMAGE=$BASE" -t "$IMAGE" "$STAGE" > "$STAGE/image-build.log" 2>&1
chown 1000:1000 "$STAGE/image-build.log"

mkdir -p "$BACKUP"
cp -a compose.yaml "$BACKUP/compose.yaml"
docker tag "$BASE_ID" "huajibot-local:rollback-flue-$(date -u +%Y%m%dT%H%M%SZ)"
docker inspect huajibot --format '{{.Image}}' > "$BACKUP/previous-image-id.txt"
docker compose stop huaji-bot-dotnet
STOPPED=1
cp -a config.json "$BACKUP/config.json"
if [[ -d plugins/data ]]; then cp -a plugins/data "$BACKUP/plugin-data"; fi
write_status switching

python3 - "$STACK" "$STAGE" "$IMAGE" <<'PY'
import copy,json,os,sys,yaml
from pathlib import Path
stack,stage,image=map(Path,sys.argv[1:])
config_path=stack/'config.json'
config=json.loads(config_path.read_text())
ai=config.setdefault('Plugins',{}).get('AIChat')
if ai:
    ai['GroupIds']=[g for g in ai.get('GroupIds',[]) if str(g)!='466691612']
    if not ai['GroupIds']: ai['Enabled']=False
config['Plugins']['Flue桥接']={
    'Enabled':True,'BridgeInstance':'main',
    'IngressUrl':'https://huaji-agent.nbtca.workers.dev/channels/huajibot/events',
    'AccountId':'a415a3ae7a197093d02233755d38800b','QueueId':'3181cdb9b0054a4a8f0d0bc70b8a48c5',
    'Destinations':[{'RobotId':'3623498320','GroupId':'466691612'}],
    'MaxMessageBytes':32768,'RetentionDays':30,'ActiveWindowSeconds':120,
    'IdlePollSeconds':15,'RetryDelaySeconds':10,'VisibilityTimeoutSeconds':120}

def atomic(path,text):
    original=path.stat()
    temp=path.with_name(path.name+'.flue-tmp')
    temp.write_text(text)
    os.chmod(temp,original.st_mode & 0o777)
    os.chown(temp,original.st_uid,original.st_gid)
    os.replace(temp,path)

atomic(config_path,json.dumps(config,ensure_ascii=False,indent=2)+'\n')
os.chmod(config_path,0o600)
compose_path=stack/'compose.yaml'
compose=yaml.safe_load(compose_path.read_text())
service=compose['services']['huaji-bot-dotnet']
service['image']=str(image)
env=service.setdefault('env_file',[])
if isinstance(env,str): env=[env]
if './deploy/flue-canary/bridge.env' not in env: env.append('./deploy/flue-canary/bridge.env')
service['env_file']=env
atomic(compose_path,yaml.safe_dump(compose,sort_keys=False,allow_unicode=True))
PY

# Check exact intended edits before starting the new container; no secrets printed.
python3 - "$STACK" "$BACKUP" <<'PY'
import json,sys,yaml
from pathlib import Path
stack,backup=map(Path,sys.argv[1:])
old=json.loads((backup/'config.json').read_text()); new=json.loads((stack/'config.json').read_text())
for c in (old,new):
    c.get('Plugins',{}).pop('AIChat',None); c.get('Plugins',{}).pop('Flue桥接',None)
assert old==new,'Unexpected bot config changes'
old=yaml.safe_load((backup/'compose.yaml').read_text()); new=yaml.safe_load((stack/'compose.yaml').read_text())
for c in (old,new):
    s=c['services']['huaji-bot-dotnet']; s.pop('image',None); s.pop('env_file',None)
assert old==new,'Unexpected compose changes'
print('Adapter credentials, DailySummary and other services preserved.')
PY

docker compose up -d --no-deps huaji-bot-dotnet
SWITCHED=1
for attempt in $(seq 1 30); do
    if docker logs --since 3m huajibot 2>&1 | grep -q 'Flue桥接已启动'; then break; fi
    sleep 2
done
docker logs --since 3m huajibot > "$STAGE/runtime.log" 2>&1
chown 1000:1000 "$STAGE/runtime.log"
grep -q 'Flue桥接已启动' "$STAGE/runtime.log"
test "$(docker inspect huajibot --format '{{.State.Status}}')" = running
test "$(docker inspect huajibot --format '{{.RestartCount}}')" = 0
write_status enabled_pending_validation
chown -R 1000:1000 "$BACKUP"
echo "Flue bridge is enabled for group 466691612. Backup: $BACKUP"
echo 'Wait for Codex to verify readiness before testing in the group.'
