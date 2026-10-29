"""Exercise the API over HTTP with an installed .NET 8 SDK."""
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import urllib.error
import urllib.request

project = Path(__file__).resolve().parent
sdk = Path(sys.argv[1]).resolve()
env = os.environ.copy()
env.update(DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', ASPNETCORE_URLS='http://127.0.0.1:5189')
server = subprocess.Popen([str(sdk), 'run', '--project', str(project)], cwd=project, env=env,
                          stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

def request(method, path, body=None):
    data = None if body is None else json.dumps(body).encode()
    req = urllib.request.Request('http://127.0.0.1:5189' + path, data=data,
                                 headers={'Content-Type': 'application/json'}, method=method)
    try:
        with urllib.request.urlopen(req, timeout=2) as response:
            return response.status, response.read()
    except urllib.error.HTTPError as exc:
        return exc.code, exc.read()

try:
    for _ in range(90):
        if server.poll() is not None:
            raise RuntimeError('API process exited before becoming ready')
        try:
            status, body = request('GET', '/users')
            break
        except (urllib.error.URLError, TimeoutError):
            time.sleep(1)
    else:
        raise TimeoutError('API did not start')
    assert status == 200 and json.loads(body) == []
    assert request('POST', '/users', {'name': '', 'email': 'not-an-email'})[0] == 400
    assert request('POST', '/users', {'name': '   ', 'email': 'test@example.com'})[0] == 400
    status, body = request('POST', '/users', {'name': 'Alex', 'email': 'alex@example.com'})
    assert status == 201
    user = json.loads(body)
    uid = user['id']
    assert request('POST', '/users', {'name': 'Other', 'email': 'ALEX@example.com'})[0] == 400
    status, body = request('POST', '/users', {'name': 'Other', 'email': 'other@example.com'})
    assert status == 201
    other_id = json.loads(body)['id']
    assert request('PUT', f'/users/{other_id}', {'name': 'Other', 'email': 'ALEX@example.com'})[0] == 400
    assert request('GET', f'/users/{uid}')[0] == 200
    status, body = request('PUT', f'/users/{uid}', {'name': 'Alex Smith', 'email': 'alex@example.com'})
    assert status == 200 and json.loads(body)['name'] == 'Alex Smith'
    assert request('DELETE', f'/users/{uid}')[0] == 204
    assert request('GET', f'/users/{uid}')[0] == 404
    assert request('DELETE', f'/users/{uid}')[0] == 404
    print('Passed: list, validation, email uniqueness, create, read, update, delete, and missing-user checks')
finally:
    server.terminate()
    try:
        server.wait(timeout=10)
    except subprocess.TimeoutExpired:
        server.kill()
        server.wait()
