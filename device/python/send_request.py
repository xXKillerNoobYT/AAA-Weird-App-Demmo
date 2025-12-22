import json
import os
import time
import uuid
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]
CLOUD_ROOT = REPO_ROOT / "Cloud"
REQUESTS_ROOT = CLOUD_ROOT / "Requests"
RESPONSES_ROOT = CLOUD_ROOT / "Responses"
DEVICE_ID = "truck-001"

os.makedirs(REQUESTS_ROOT / DEVICE_ID, exist_ok=True)
os.makedirs(RESPONSES_ROOT / DEVICE_ID, exist_ok=True)

request_id = f"req-{uuid.uuid4()}"
req_path = REQUESTS_ROOT / DEVICE_ID / f"{request_id}.json"
resp_path = RESPONSES_ROOT / DEVICE_ID / f"{request_id}.json"

request = {
    "request_type": "ping",
    "request_id": request_id,
    "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
    "device_id": DEVICE_ID,
}

with open(req_path, "w", encoding="utf-8") as f:
    json.dump(request, f, indent=2)

print(f"Wrote request: {req_path}")

# Poll for response
for _ in range(60):  # up to ~60 seconds
    if resp_path.exists():
        with open(resp_path, "r", encoding="utf-8") as f:
            response = json.load(f)
        print("Received response:")
        print(json.dumps(response, indent=2))
        break
    time.sleep(1)
else:
    print("No response received within timeout.")
