import re
from datetime import datetime

with open(r'E:\CamCapture\SdkLog\SdkLog_1_W.log', encoding='utf-8') as f:
    lines = f.readlines()

# COM_CaptureJPEGPicture_NEW = 发起抓图
# 紧随其后的 Private connect = 建立连接（网络请求开始）
# 下一条 COM_CaptureJPEGPicture_NEW 发起 = 上一次抓图已返回

cap_pattern  = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d+)\]\[DBG\] COM_CaptureJPEGPicture_NEW')
conn_pattern = re.compile(r'\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d+)\]\[INF\] Private connect')

# 按行号收集所有事件
events = []
for i, line in enumerate(lines):
    m = cap_pattern.search(line)
    if m:
        t = datetime.strptime(m.group(1), '%Y-%m-%d %H:%M:%S.%f')
        events.append(('CAP', t, i+1))
        continue
    m2 = conn_pattern.search(line)
    if m2:
        t = datetime.strptime(m2.group(1), '%Y-%m-%d %H:%M:%S.%f')
        events.append(('CONN', t, i+1))

print("=== 时间线（抓图调用 + 连接建立）===")
for ev in events:
    print(f"  行{ev[2]:<4} [{ev[0]}] {ev[1].strftime('%H:%M:%S.%f')[:-3]}")

# 配对：每个 CAP 后紧跟的第一个 CONN 就是该次抓图建立连接的时间
print("\n=== 单次抓图耗时（CAP → 紧随的 CONN）===")
print(f"{'#':<4} {'抓图发起时间':<20} {'连接建立时间':<20} {'发起→连接耗时'}")
print("-" * 70)

pairs = []
cap_idx = 0
i = 0
cap_count = 0
while i < len(events):
    if events[i][0] == 'CAP':
        cap_t = events[i][1]
        cap_line = events[i][2]
        # 找紧随其后的第一个 CONN
        j = i + 1
        while j < len(events) and events[j][0] != 'CONN':
            j += 1
        if j < len(events) and events[j][0] == 'CONN':
            conn_t = events[j][1]
            delta_ms = (conn_t - cap_t).total_seconds() * 1000
            cap_count += 1
            pairs.append(delta_ms)
            print(f"{cap_count:<4} {cap_t.strftime('%H:%M:%S.%f')[:-3]:<20} {conn_t.strftime('%H:%M:%S.%f')[:-3]:<20} {delta_ms:.1f} ms")
        else:
            cap_count += 1
            print(f"{cap_count:<4} {cap_t.strftime('%H:%M:%S.%f')[:-3]:<20} {'(无后续连接)':<20} -")
    i += 1

# 抓图间隔统计
caps_only = [ev for ev in events if ev[0] == 'CAP']
print("\n=== 抓图间隔（相邻两次抓图发起的间距）===")
print(f"{'#':<4} {'本次抓图':<20} {'上次抓图':<20} {'间隔'}")
print("-" * 70)
intervals = []
for i in range(1, len(caps_only)):
    delta = (caps_only[i][1] - caps_only[i-1][1]).total_seconds() * 1000
    intervals.append(delta)
    print(f"{i:<4} {caps_only[i][1].strftime('%H:%M:%S.%f')[:-3]:<20} {caps_only[i-1][1].strftime('%H:%M:%S.%f')[:-3]:<20} {delta:.0f} ms")

if pairs:
    print(f"\n=== 汇总统计 ===")
    print(f"总抓图次数     : {len(caps_only)}")
    print(f"\n[CAP→CONN 耗时]  最小: {min(pairs):.1f} ms  最大: {max(pairs):.1f} ms  均值: {sum(pairs)/len(pairs):.1f} ms")
    if intervals:
        print(f"[抓图间隔]       最小: {min(intervals):.0f} ms  最大: {max(intervals):.0f} ms  均值: {sum(intervals)/len(intervals):.0f} ms")
