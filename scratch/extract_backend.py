import os
import re
import json

base_dir = "src/Cortex"

controllers = []
for root, _, files in os.walk(base_dir):
    for f in files:
        if f.endswith("Controller.cs"):
            path = os.path.join(root, f)
            with open(path) as file:
                content = file.read()
                matches = re.findall(r'\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch)(?:\("([^"]+)"\))?\]\s*(?:\[[^\]]+\]\s*)*public\s+(?:async\s+)?Task<[^>]+>\s+(\w+)', content)
                for method, route, func_name in matches:
                    controllers.append({
                        "file": f,
                        "method": method.replace("Http", "").upper(),
                        "route": route if route else "/",
                        "func": func_name
                    })

print("API Endpoints:")
for c in controllers:
    print(f"{c['method']} {c['route']} -> {c['file']}.{c['func']}")

print("\nDatabase Models:")
db_dir = os.path.join(base_dir, "Database/Entities")
if os.path.exists(db_dir):
    for f in os.listdir(db_dir):
        if f.endswith(".cs"):
            print(f)
else:
    for root, _, files in os.walk(base_dir):
        for f in files:
            if "Entity" in root or "Entities" in root:
                print(f)
