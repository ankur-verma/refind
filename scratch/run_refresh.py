import urllib.request
import urllib.error
import json

BASE_URL = "http://localhost:5183/api/v1"
EMAIL = "test@test.com"
PASSWORD = "Ankur@1211"

def make_request(url, method="GET", payload=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    
    data = None
    if payload:
        data = json.dumps(payload).encode("utf-8")
        
    req = urllib.request.Request(url, data=data, method=method)
    for k, v in headers.items():
        req.add_header(k, v)
        
    try:
        with urllib.request.urlopen(req) as response:
            res_data = response.read().decode("utf-8")
            return json.loads(res_data)
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8")
        print(f"HTTP Error {e.code}: {err_body}")
        try:
            return json.loads(err_body)
        except Exception:
            return {"success": False, "errors": [err_body]}
    except Exception as e:
        print(f"Connection Error: {e}")
        return {"success": False, "errors": [str(e)]}

def run_test():
    print("--- 1. Login ---")
    login_payload = {
        "email": EMAIL,
        "password": PASSWORD
    }
    login_res = make_request(f"{BASE_URL}/auth/login", method="POST", payload=login_payload)
    print("Login Status:", login_res.get("success"))
    
    if not login_res.get("success"):
        print("Login failed:", login_res)
        return
        
    token = login_res["data"]["accessToken"]
    
    print("\n--- 2. Force Profile Refresh synchronously ---")
    refresh_res = make_request(f"{BASE_URL}/interaction/profile/refresh", method="POST", token=token)
    print("Refresh Profile Response Success:", refresh_res.get("success"))
    if refresh_res.get("success"):
        print(json.dumps(refresh_res["data"], indent=2))
    else:
        print("Refresh failed:", refresh_res)

if __name__ == "__main__":
    run_test()
