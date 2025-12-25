import time
from pathlib import Path


def main() -> None:
    watch_dir = Path("Cloud/Requests")
    watch_dir.mkdir(parents=True, exist_ok=True)

    print("WeirdToo Server - File Watcher")
    print(f"Watching: {watch_dir.as_posix()}")
    print("Press Ctrl+C to stop...")
    print()

    try:
        while True:
            files = list(watch_dir.glob("*") )
            # Simple heartbeat showing number of files detected
            print(f"Requests detected: {len(files):3d}", end="\r", flush=True)
            time.sleep(2)
    except KeyboardInterrupt:
        print("\nServer stopped")


if __name__ == "__main__":
    main()
