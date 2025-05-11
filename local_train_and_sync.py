import requests
import os
import sys
import json

SERVER_URL = "http://localhost:5000"
PLAYER_ID = int(os.environ.get("PLAYER_ID", sys.argv[1] if len(sys.argv) > 1 else 1))
CLIENT_ID = os.environ.get("CLIENT_ID", sys.argv[2] if len(sys.argv) > 2 else 1)
player_str = f"player{PLAYER_ID}"
client_str = f"client_{CLIENT_ID}"
EXPERIENCE_PATH = f"experience_{player_str}_{client_str}.json"

def upload_experience(experience_path, player_str, client_str):
    print(f"[INFO] Uploading experience for {player_str}_{client_str}...")
    try:
        with open(experience_path, "rb") as f:
            files = {"file": f}
            data = {"client_id": f"{player_str}_{client_str}"}
            r = requests.post(f"{SERVER_URL}/upload_experience", files=files, data=data)
            print("Upload experience response:", r.text)
    except FileNotFoundError:
        print(f"[INFO] No experience file found for {player_str}_{client_str}")
    except Exception as e:
        print(f"[ERROR] Failed to upload experience: {e}")

if __name__ == "__main__":
    print(f"[INFO] Using experience: {EXPERIENCE_PATH}")
    upload_experience(EXPERIENCE_PATH, player_str, client_str) 