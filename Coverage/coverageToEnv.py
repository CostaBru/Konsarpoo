import os
import sys
import re
import xml.etree.ElementTree as ET

def index_withoutexception(s, subs, start = 0):
    try:
        return s.index(subs, start)
    except:
        return -1

if len(sys.argv) == 1:
    print("No arguments provided")
    sys.exit(1)

coverageReport = sys.argv[1]

print(f"Coverage file name {coverageReport}")

coverage = None

# Try to parse Cobertura XML first (more robust)
if coverageReport.lower().endswith('.xml'):
    try:
        tree = ET.parse(coverageReport)
        root = tree.getroot()
        # Cobertura root tag is usually 'coverage'
        # Prefer 'line-rate' attribute if present
        line_rate = root.attrib.get('line-rate')
        if line_rate is not None:
            coverage = float(line_rate) * 100.0
        else:
            # Fallback to lines-covered / lines-valid if available
            covered = root.attrib.get('lines-covered')
            valid = root.attrib.get('lines-valid')
            if covered is not None and valid is not None:
                c = float(covered)
                v = float(valid)
                coverage = 0.0 if v == 0 else (c / v) * 100.0
    except Exception as e:
        print(f"Failed to parse XML coverage: {e}")

# If not XML or parsing failed, try legacy HTML parsing (dotCover or ReportGenerator summary)
if coverage is None:
    try:
        with open(coverageReport, encoding='utf-8', errors='ignore') as file:
            content = file.read()
            # Legacy dotCover HTML format pattern
            idx = content.find('block0 = [["Total"')
            if idx >= 0:
                # Extract between first and second commas after the match
                tail = content[idx: idx + 500]
                parts = tail.split(',')
                if len(parts) >= 3:
                    coverage = float(parts[1].strip())
            if coverage is None:
                # ReportGenerator summary: try to find "Line coverage: XX%"
                m = re.search(r"Line coverage\s*[:=]\s*(\d+(?:\.\d+)?)%", content, re.IGNORECASE)
                if m:
                    coverage = float(m.group(1))
    except Exception as e:
        print(f"Failed to parse HTML coverage: {e}")

if coverage is None:
    print("Coverage value not found; defaulting to 0")
    coverage = 0.0

print(f"Coverage value {coverage}")

env_file = os.getenv('GITHUB_ENV')

if env_file:
    with open(env_file, "a") as myfile:
        myfile.write("COVERAGE=" + str(coverage))
else:
    # Local run: just echo the value so it is visible
    print("GITHUB_ENV is not set; skipped writing environment variable.")
