"""Gerçek API + PostgreSQL entegrasyon testi. API çalışırken: python3 tests/smoke.py"""
import concurrent.futures
import json
import os
import urllib.error
import urllib.request
import uuid

base = os.environ.get('API_URL', 'http://127.0.0.1:5080')

def call(method, path, body=None, key=None):
    headers = {'Content-Type': 'application/json'}
    if key is not None:
        headers['Idempotency-Key'] = key
    req = urllib.request.Request(base + path, data=json.dumps(body).encode() if body is not None else None,
                                 headers=headers, method=method)
    try:
        response = urllib.request.urlopen(req, timeout=30)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, response.read(), response.headers

payload = {'name': 'Kalem', 'price': 10}
key = str(uuid.uuid4())
assert call('GET', '/health')[0] == 200
assert call('POST', '/products', payload)[0] == 400
assert call('POST', '/products', payload, ' ')[0] == 400
assert call('POST', '/products', {'name': '', 'price': -1}, key)[0] == 400
first = call('POST', '/products', payload, key)
assert first[0] == 201 and first[2]['Idempotency-Replayed'] == 'false'
repeat = call('POST', '/products', {'name': 'Kalem', 'price': 10.00}, key)
assert repeat[0] == 201 and repeat[1] == first[1]
assert repeat[2]['Location'] == first[2]['Location'] and repeat[2]['Idempotency-Replayed'] == 'true'
read = call('GET', first[2]['Location'])
assert read[0] == 200 and json.loads(read[1]) == json.loads(first[1])
assert call('POST', '/products', {'name': 'Defter', 'price': 10}, key)[0] == 409
fresh = call('POST', '/products', payload, str(uuid.uuid4()))
assert fresh[0] == 201 and fresh[1] != first[1]
parallel_key = str(uuid.uuid4())
with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
    results = list(pool.map(lambda _: call('POST', '/products', payload, parallel_key), range(12)))
assert all(r[0] == 201 for r in results)
assert len({r[1] for r in results}) == 1
assert sum(r[2]['Idempotency-Replayed'] == 'false' for r in results) == 1
# Aynı anahtar için yarışan farklı payload'lardan yalnızca biri kabul edilmeli.
conflict_key = str(uuid.uuid4())
with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
    races = list(pool.map(lambda name: call('POST', '/products', {'name': name, 'price': 10}, conflict_key), ['A', 'B']))
assert sorted(r[0] for r in races) == [201, 409]
assert call('GET', '/products/' + str(uuid.uuid4()))[0] == 404
print('PASS: sağlık, doğrulama, oluşturma, tekrar, semantik gövde, GET, çakışma, yeni anahtar, 12 eşzamanlı tekrar ve farklı gövde yarışı')
