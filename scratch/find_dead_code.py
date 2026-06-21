import os
import re

csharp_files = []
for root, _, files in os.walk("src/Cortex"):
    for f in files:
        if f.endswith(".cs"):
            csharp_files.append(os.path.join(root, f))

# Find all defined classes, interfaces, methods
defined_symbols = {}
for path in csharp_files:
    with open(path, 'r', encoding='utf-8') as file:
        content = file.read()
        
        # class or interface
        matches = re.findall(r'(?:public|internal|private)\s+(?:class|interface|enum|record)\s+(\w+)', content)
        for m in matches:
            if m not in defined_symbols:
                defined_symbols[m] = []
            defined_symbols[m].append(path)

print(f"Total defined symbols: {len(defined_symbols)}")
# This is a bit too naive for C#, let's just look for specifically obvious duplicate services.
