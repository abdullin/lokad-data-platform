start Platform.Node/bin/Debug/Platform.Node.exe
@ping localhost -w 1000 -n 2 > nul
start "" Platform.TestClient/bin/Debug/Platform.TestClient.exe 127.0.0.1 8080 USAGE
@ping localhost -w 1000 -n 3 > nul
taskkill /IM Platform.Node.exe