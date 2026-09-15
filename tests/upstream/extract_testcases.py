import re, json, sys, os

base = os.path.dirname(os.path.abspath(__file__))
src_path = os.path.join(base, "Unishox2", "test_unishox2.c")

with open(src_path, encoding="utf-8") as f:
    src = f.read()

start = src.index("int run_unit_tests(")
end = src.index("\n}\n", start)
body = src[start:end]

pattern = re.compile(r'test_ushx_cd(?:_with_len)?\(\s*"((?:[^"\\]|\\.)*)"', re.DOTALL)
matches = pattern.findall(body)


def decode_c_string(s):
    out = []
    i = 0
    n = len(s)
    while i < n:
        c = s[i]
        if c == "\\" and i + 1 < n:
            nc = s[i + 1]
            if nc == "n":
                out.append("\n")
                i += 2
                continue
            if nc == "t":
                out.append("\t")
                i += 2
                continue
            if nc == "r":
                out.append("\r")
                i += 2
                continue
            if nc == '"':
                out.append('"')
                i += 2
                continue
            if nc == "\\":
                out.append("\\")
                i += 2
                continue
            if nc == "x":
                j = i + 2
                hexs = ""
                while j < n and len(hexs) < 2 and s[j] in "0123456789abcdefABCDEF":
                    hexs += s[j]
                    j += 1
                out.append(chr(int(hexs, 16)))
                i = j
                continue
            out.append(nc)
            i += 2
            continue
        out.append(c)
        i += 1
    return "".join(out)


decoded = [decode_c_string(m) for m in matches]
out_path = os.path.join(base, "testcases.json")
with open(out_path, "w", encoding="utf-8") as f:
    json.dump(decoded, f, ensure_ascii=False)

print("count:", len(decoded))
for d in decoded[:5]:
    print(repr(d))
