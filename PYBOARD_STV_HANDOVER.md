# pyboard_LEDRingTestV0.1 — stv 状态快照协议移植交接文档

> 本文档供跨对话交接使用。pyboard 项目不在 Unity 工作区内，路径如下。
> 更新时间：2026-09-09

## 1. 项目与文件位置

| 项目 | 路径 | 状态 |
|---|---|---|
| 上位机（Unity C#） | `e:\Unity\LEDRingTest\Assets\Script\Moving.cs` | stv 协议闭环已完成（索取/保存/重放） |
| Arduino Due 固件 | `E:\Unity\Arduino\LEDRing_for_unity_byte_mode_NewVersion_V2_5\LEDRing_for_unity_byte_mode_NewVersion_V2_5.ino` | stv 协议 + 零堆串口链路已完成 |
| **pyboard 固件（待移植）** | `E:\Unity\Arduino\pyboard_LEDRingTestV0.1\`（main.py / comm.py / config.py） | **stv 协议缺失**，其余不涉及 |

pyboard 项目结构：
- `main.py`：硬件驱动（Pump/OG/MS 类、ExtInt）+ `state` dict + `command_parse` + `loop`
- `comm.py`：帧编解码（`encode_frame`/`push_logical`）、`SendBuffer`、握手、接收状态机（纯 bytes 层）
- `config.py`：`VERSION="V2.4"`、引脚映射、`SERIAL_PRINT_TYPE`、`STATE_ORDER`（变量序号 0~10 契约）、`ARRAY_TYPES` 与 `ARRAY_TYPE_LENGTH=[8,8,8]`
- `tools/deploy*.ps1`、`repl.ps1`：部署与 REPL 维护工具

## 2. stv 协议契约（两套固件通用，C# 端零改动）

- 帧：`0xAA + typeId + len + content + 0xDD + \r\n`，type=7 为 `cmd`
- 快照体：`stv:0=<lick_mode>;1=<trial>;2=<trial_set>;3=<now_pos>;4=<lick_rec_pos>;5=<INDEBUGMODE>;6=<OGActiveMills>;7=<miniscopeRecord>;8=<waterServeWhenLick>;10=<lightControl>;a0=<micros×8逗号分隔>;a1=<lick_count×8>;a2=<water_flush×8>`
- **不含 `9=waterServeManual`**（不做同步，避免重连后意外出水）
- 重放规则：跳过 `trial_set(2)` 与 `waterServeManual(9)`；`now_pos(3)` 保留（赋值前 `pre_pos = now_pos`）；`OGActiveMills(6)` 实际触发一次输出；`miniscopeRecord(7)` 设引脚；`lightControl(10)` 设 DAC；`water_flush` **直接置位**（不走翻转语义）
- C# 端流程：`StartTrial` 后 1s alarm 触发 `ArduinoContextRquest()` → `DataSend("statusValue")` → 固件回 `stv:` 帧 → case 7 存入 `arduinoStatusBackup`（L970）→ 断线重连后 `ArduinoContextPost()` 原样发回（调用点：串口线程 `SerialCommunicating` 与 `CommandVerify` 内）
- C# `compatibleVersion` 已含 `"V2.5"`；pyboard `config.VERSION="V2.4"` 在兼容列表内，可不改

## 3. 必要性对照表（本次 Arduino 修改 → pyboard）

| # | 修改项 | pyboard 现状 | 是否必要移植 | pyboard 落点 |
|---|---|---|---|---|
| 1 | `print_status_value()` 快照打包 | `command_parse` 收到 `statusValue` 静默忽略 | **必要** | main.py 新增函数 |
| 2 | `parse_status_value()` 快照重放 | `stv:...` 进 `":" in cmd` 分支但 `ctype=="stv"≠"cmd"`，**静默忽略，状态不恢复** | **必要** | main.py 新增函数 |
| 3 | `commandParse` 的 `stv:`/`statusValue` 入口 | 无对应分支 | **必要**（1/2 的接线） | `command_parse` 加两个分发 |
| 4 | `pointerArrayType_arrayLength` {8,8,8} 越界修复 | `config.ARRAY_TYPE_LENGTH=[8,8,8]` 天然正确 | 不必要 | — |
| 5 | `stringToByteArray` → `const char*` | `encode_frame` 用 bytes 一次构造 | 不必要（MicroPython GC 压缩式回收，无碎片化） | — |
| 6 | `serial_send` 双重载 / buffer 280 | `push_logical`→`encode_frame` 直接入队 | 不必要 | — |
| 7 | `bufferGetString` 栈缓冲 + `loop()` char[128] | pyboard 无此函数，接收状态机逐字节 append | 不必要 | — |
| 8 | （C#）Latin-1(28591) 编码修复 | 设备无关，已在 Moving.cs 生效，对 pyboard 路径同样有效 | 不必要（已生效） | 顺带：`comm.decode_frame_content` 用 ascii 解码，帧内容含非 ASCII 会静默丢弃，可选改 utf-8 |
| 9 | `VERSION` 升 V2.5 | `"V2.4"` 在兼容列表内 | 可选 | 想区分两套固件时再升 |

## 4. 移植实施要点（仅改 main.py，约 60 行）

### 4.1 `print_status_value()`（放在 `print_status` 之后）

```python
def print_status_value():
    s = state
    parts = [
        "0={0}".format(s["lick_mode"]),  "1={0}".format(s["trial"]),
        "2={0}".format(s["trial_set"]),  "3={0}".format(s["now_pos"]),
        "4={0}".format(s["lick_rec_pos"]),"5={0}".format(s["INDEBUGMODE"]),
        "6={0}".format(s["OGActiveMills"]),"7={0}".format(s["miniscopeRecord"]),
        "8={0}".format(s["waterServeWhenLick"]),"10={0}".format(s["lightControl"]),
    ]
    parts.append("a0=" + ",".join(str(x) for x in config.WATER_SERVE_MICROS))
    parts.append("a1=" + ",".join(str(x) for x in lick_count))
    parts.append("a2=" + ",".join(str(x) for x in water_flush))
    body = "stv:" + ";".join(parts)
    if len(body) > 250:   # 帧内容上限 0xFF
        comm.push_logical("log:statusValue too long, skipped")
        return
    comm.push_logical("cmd:" + body)
```

### 4.2 `parse_status_value(body)`

- 按 `;` split，每段按第一个 `=` 分 id/value；空段跳过
- 数组段（id 以 `a` 开头）：`a0`→`config.WATER_SERVE_MICROS[i]`、`a1`→`lick_count[i]`（纯赋值）；`a2`→`water_flush[i] = 1 if v > 0 else 0` 并 `pump_pins[i].high()/low()`（**直接置位，不走 L504 的翻转语义**）
- 标量段：跳过 `id==2`、`id==9`；`0<=id<len(config.STATE_ORDER)` 内赋值 `state[config.STATE_ORDER[id]] = val`，副作用映射：
  - `now_pos`：先 `state["pre_pos"] = state["now_pos"]`
  - `OGActiveMills`：`og.legacy_set(val)`
  - `miniscopeRecord`：`ms.set(0, val != 0)`
  - `lightControl`：clamp 0~4096 后 `set_light_power(v)`
- 越界/非法 id 静默跳过

### 4.3 `command_parse` 接线（两行分发）

```python
if cmd == "statusValue":
    print_status_value(); return
```
`":" in cmd` 分支中：
```python
if ctype == "cmd":
    _parse_new_cmd(body, cmd)
elif ctype == "stv":
    parse_status_value(body)
return
```

## 5. 验收清单

1. Unity 连 pyboard 运行：StartTrial 1s 后 Console 出现 `Arduino status backup updated: stv:...`；
2. 修改状态（如 `6=500` 设 OG、`2[3]=1` 设 water_flush）再索取快照，确认内容变化；
3. 重发保存的 `stv:` 体：og 通道 0 PWM 全高 500ms、water_flush 对应 `pump_pins` 电平变化、`trial_set`/`waterServeManual` 未被重放；
4. `//check`、`//checkArray`（pyboard 明文 `/check/` 双斜杠文法，经 comm TEXT_CMD_MARK）交叉核对；
5. 断线重连（或 forceinit）后自动重放生效。

## 6. 注意事项

- **不要**把 Arduino 的零堆改造（第 5~7 项）移植过来：MicroPython GC 是压缩式标记清除，不存在 newlib malloc 的碎片化问题；
- pyboard 的 water_flush 命令语义（main.py L504）是翻转，重放路径必须绕开（见 4.2）；
- 本文档对应的 Arduino 侧实现细节（含 Latin-1 编码修复、零堆链路）见 V2_5.ino 与 Moving.cs，关键行号以实际文件为准（Moving.cs：`ArduinoContextRquest` ~L1769、`ArduinoContextPost` ~L1774、case 7 ~L2968、重连 Post 调用 ~L3090/`CommandVerify` 内；`compatibleVersion` 已含 V2.5）。
