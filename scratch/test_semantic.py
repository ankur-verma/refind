import urllib.request
import json
import ssl

ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

req = urllib.request.Request("http://localhost:5183/api/v1/quickboost/watch-plan?userId=a3f17656-4498-44fe-93e7-948f3cc95e1e&topic=AI%20Model%20Interoperability%20and%20Protocols")
try:
    with urllib.request.urlopen(req, context=ctx) as response:
        data = json.loads(response.read().decode())
        print(json.dumps(data, indent=2))
except Exception as e:
    print(e)
