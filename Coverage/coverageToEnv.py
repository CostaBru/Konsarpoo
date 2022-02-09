import os
import sys

def index_withoutexception(s, subs, start = 0):
    try:
        return s.index(subs, start)
    except:
        return -1

if len(sys.argv) == 1:
    print("No arguments provided")

coverageReport = sys.argv[1] #"C:\\Users\\koolt\\Downloads\\test-coverage (1)\\Konsarpoo.CoverageReport.html" 

print(f"Coverage file name " + str(coverageReport))

coverage = 0

with open(coverageReport) as file:
    lines = file.readlines()

    for l in lines:

        indexOf = index_withoutexception(l, "block0 = [[\"Total\"")

        if indexOf >= 0:
            next1 = index_withoutexception(l, ",", indexOf)
            next2 = index_withoutexception(l, ",", next1 + 1)

            coverage = float(l[next1 + 1 : next2])

            print(f"Coverage value " + str(coverage))            


env_file = os.getenv('GITHUB_ENV')

with open(env_file, "a") as myfile:
    myfile.write("COVERAGE=" + str(coverage))