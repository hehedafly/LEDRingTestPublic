# LEDRingTest LinearTrack（VR）文档

本文档面向 **LinearTrack** 直线轨道 VR 系统（下位机为 Arduino Due）。主系统 LEDRing（环形）的文档见 [README.md](README.md)。

LinearTrack 与 LEDRing 共用同一个开始菜单：菜单页 **LEDRing → Go To VR** 进入 LinearTrack 页，选择配置后进入 `linear_track` 场景；运行中按 **Esc** 返回菜单（仍停留在 LinearTrack 页）。也可通过 `metaConfig.ini` 的 `prefer_context=LinearTrack` 让首次启动直接显示 LinearTrack 页（详见 README.md"启动流程与初始菜单"）。

实验内容：小鼠在直线轨道上前进，Unity 按位置铺设 context 走廊与奖励区；进入奖励区后按所选给水模式（舔水触发 / 速度依赖 / 里程依赖）出水，位置数据来自轨道编码器的串口上报。

---

## 配置文件

- **编辑器**：`Assets/Resources/linear_track/config*.ini`
- **构建**：`..._Data/Resources/linear_track/config*.ini`

菜单的 LinearTrack 页扫描同目录 `config*.ini` 供选择；直接运行场景时使用 `config.ini`。横条上显示的信息取自 `[trail_content]` 的 `context_for_repeat` 与 `repeat_num`。

### [serialSettings]

| 参数 | 格式 | 说明 |
|------|------|------|
| `serialSpeed` | int | 波特率（默认 115200；非法值自动回落 115200） |
| `blackList` | 逗号分隔 | 永不尝试的 COM 口 |
| `recommendPort` | 逗号分隔 | 优先尝试的 COM 口（把 Due 所在口写进来可跳过逐个试探，显著加快连接） |
| `compatibleVersion` | 逗号分隔 | 握手版本白名单；**留空 = 不校验**。Due 固件版本形如 `LT1.0`/`LT2.0`，白名单非空且不含板端上报版本时拒连 |

### [trail_content]

| 参数 | 格式 | 默认 | 说明 |
|------|------|------|------|
| `context_for_repeat` | 逗号分隔的 context 名 | `context0` | 会话运行的 context 序列，顺序即运行顺序（如 `context0,context1,context0`） |
| `repeat_num` | int | `5` | 整个序列重复轮数（即 trial 数） |
| `start_mode` | int | `0` | 启动时的给水模式（见"给水模式"） |

### [contextN]（N = 0、1、2……，每个 context 一节）

| 参数 | 格式 | 默认 | 说明 |
|------|------|------|------|
| `start` | 逗号分隔数值，或 `randomX~Y` | `0` | 每个 trial 在 context 内的起始相对位置；`random` 时每 trial 独立随机 |
| `length` | float | `10` | context 长度；**≥999 自动变为无限段**（长度置 2000，跑到头回绕继续，无终点结算） |
| `wall_paper` | 选项 | `point_sparse` | 走廊墙纸：`bar` / `point` / `point_sparse` / `custom`（custom 读取同目录 `custom.png`） |
| `reward_zone_paper` | 选项 | `bar` | 奖励区墙纸 |
| `reward_zone_start` | float | `1` | 奖励区起点（相对 context 起点；<0 则不生成奖励区） |
| `reward_zone_length` | float | `3` | 奖励区长度（超出自动截断到 context 末尾） |
| `landmark_name` | 选项 | `none` | 地标：`tower` / `twin_tower` / `tunnel` / `tunnel_no_ceil` / `tunnel_no_ceil_and_end` |
| `landmark_start` | float | `-1` | 地标位置（>0 才生成） |
| `lick_threshold` | `min,max` | `4,8` | 成功判定：本 trial **奖励区内**舔水次数落在 [min, max] 区间算成功 |
| `end_context` | `成功色,失败色` | `grey,black` | context 结束等待期的全屏颜色（HTML 色名，如 `grey`、`black`、`green`） |
| `end_arg` | float | `3` | context 结束后的等待秒数（>0 才做成败判定与变色；期间画面冻结、位置回弹、暂停出水） |
| `lick_count_max` | int | `8` | 历史参数，现版本不再下发固件，仅保留占位 |

---

## 界面与显示

### 位置指示图（PositionIndicateImage）

竖直条，**顶部 = 本 context 起点（0），底部 = 终点（length）**：

| 元素 | 含义 |
|------|------|
| DownArrow | 当前相对位置标记，随跑动上下移动 |
| LightGreen | 位于奖励区内时点亮 |
| LightRed | 位于奖励区外时点亮 |
| 数字文本 | 起点值、终点值（=context 长度）、奖励区起止位置；`now:` 显示实时相对位置（3 位小数） |
| 左侧红色横线 | 每次舔水记录 |
| 右侧蓝色横线 | 每次固件确认出水记录 |

无限段（`length`≥999）时指示图按 100 单位滚动窗口显示，超出窗口整体前移。

### 其他显示

- **LineChart**：编码器原始数据曲线与阈值参考线。
- **context_info**：当前 trial / context、是否处于结算等待（waiting）、奖励区前/内/后舔水计数、正确舔水判定、`lick_threshold`。
- **日志窗口**：串口与操作日志，超过 8000 字符自动分页，可用 `log_scrollbar` 或 Page Up/Down 浏览。

---

## 操作控件

### 键盘

| 键 | 功能 |
|----|------|
| `↑` / `↓` | 模拟编码器前进/后退（按住越久速度越高，约 1.5s 达到上限；松开即停）——无下位机时的小鼠运动模拟 |
| `Ctrl` + 前进 | 解除速度上限（快速穿越） |
| `Enter` | 提交聚焦中的输入框 |
| `Esc` | 退出运行，返回开始菜单 |

### UI 控件

| 控件 | 类型 | 功能 |
|------|------|------|
| `ModeSelect` | 下拉（mode 0/1/2） | 切换给水模式；切换时强制同步固件并把上一模式的残留计数清零，日志显示 `mode now: X`。mode>0 时 `Mode1Config` 下拉出现该模式的参数字典 |
| `Mode1Config` | 下拉（动态） | 选择当前模式的一个可调参数名 |
| `IFConfigValue` | 输入框+回车 | 给 `Mode1Config` 选中的参数赋新值（日志回显 `config changed: 参数 从 旧值 到 新值`）；mode 0 时忽略 |
| `IFSerialMessage` | 输入框+回车 | 手动发串口命令：以 `/` 开头按 `变量=值` 通道发送（如 `/p_serveWaterReward=1`）；否则按原始命令发送（如 `ping`、`init`） |
| `SliderMaxSpd` | 滑动条 | 最大速度（0~2）；文本参数为 `rolling` 时改设原始值上限 |
| `SliderScaleFactor` | 滑动条 | 速度换算系数（0.01~5） |
| `IFThreshold_x/y/z` | 输入框+回车 | 传感器速度阈值（默认 20），改动实时刷新 LineChart 参考线 |
| `IFSmooth` | 输入框 | 速度平滑采样数 |
| `IFSensorPause` | 输入框 | 无新编码器数据多久后自动停车（默认 0.2s） |

注：maxSpeed、thresholds 等默认值在场景 Inspector 中设置，运行时以上述 UI 为准。

---

## 给水模式

三种模式在运行时经 `ModeSelect` 随时切换；**全部要求小鼠位于奖励区内才出水**（mode 0 另有离开奖励区后 1 秒的额外窗口）。

### mode 0 —— 舔水触发（Unity 判定）

收到固件舔水上报后，在奖励区内（或刚离开 1s 内）且距上次给水超过 0.3s 时，按舔水所在通道发给出水命令。单次出水量由固件端给水脉宽参数决定。

### mode 1 —— 速度依赖自动给水

不看舔水，按跑动速度出水：

| 参数（Mode1Config 可选） | 默认 | 含义 |
|------|------|------|
| `speed_threshold` | 0.2 | 速度超过即记为开始跑动 |
| `lasting_time_threshold` | 0.4 | 持续超速该秒数后才开始给水 |
| `running_time_threshold` | 2 | 跑够该秒数从"尝试跑动"转为"稳定跑动"档 |
| `time_serve_interval` | 2 | 尝试期每该秒数出水一次 |
| `time_serve_interval_when_runing` | 1 | 稳定期每该秒数出水一次 |
| `random_delay_sec` | 2 | 可叠加的随机延迟上限（`time_run_begin`/`time_served`/`random_delayed` 为记录性键） |

### mode 2 —— 里程依赖自动给水

| 参数 | 默认 | 含义 |
|------|------|------|
| `length_threshold` | 1 | 累计跑动里程超过该值 |
| `time_serve_interval` | 2 | 且距上次出水超过该秒数 |
| `running_threshold`、`time_serve_interval_when_runing`、`time_served`、`random_delayed` | — | 记录/辅助键 |

满足条件时在奖励区内出水一次（固定通道 0）并清零里程。

mode 1/2 参数字典是**全局**的（不随 context 区分）；按 context 区分的是起点、奖励区位置、`lick_threshold`、结束等待与成败颜色等。

---

## 试验流程

1. 按 `[trail_content]` 序列生成当前 context 的走廊、奖励区、地标；小鼠从 `start` 指定的相对位置出发。
2. 前进越靠编码器数据驱动（或键盘模拟）；进入奖励区后按当前模式出水；舔水按"区前/区内/区后"分区计数。
3. 越过 context 终点：位置钳止，画面按区内舔水次数与 `lick_threshold` 判定成败并全屏染 `end_context` 对应颜色，等待 `end_arg` 秒后进入序列中下一个 context。
4. 序列走完一轮 `now_trial+1`；`now_trial > repeat_num` 时会话结束（全屏绿并停住，需人工 Esc 退出）。

---

## 与 Due 的串口通讯

- **连接**：启动时扫描端口，逐个发送 `//forceinit` 并等待 `initialed:<版本>`（每 ~600ms 重发一次），版本经 `compatibleVersion` 白名单核对后回 `ACK`、收到 `ACK_OK` 完成握手；连接过程日志带 `[serial ...]` 时间戳，可在 Player.log 中核对各阶段耗时。
- **无下位机模式**：所有端口握手失败时弹窗询问，选"Yes"进入纯键盘模拟模式（不发出水、不做串口校验）。
- **编码器上行**：Due 以帧格式 `0xAA|类型|长度|内容|0xDD` 推送位移增量，类型含 `mv`（编码器）、`ci`（舔水 `lick:通道` / 出水确认 `ws`）、`log`、`echo` 等。
- **命令下发与校验**：变量写入形如 `3=5`（下标即固件变量表序，如 0=`p_enter_reward_context`、7=`p_serveWaterReward`）；关键写入走同步回显校验（发命令→等 `echo:...:echo`），失败自动重试。
- **相关报错**（连接与语义在近期版本重构过，旧版 `verify failed at ...` 大部分场景已不会出现）：
  - `p_enter_reward_context=±1 verify failed`：进/出奖励区时固件状态同步失败，可能影响后续出水许可；
  - `context info sync failed`：trial 结算期参数同步多次重试仍失败，通常伴随连接不稳；
  - 以上均记录在日志窗口与 Player.log，出现时优先检查 USB 线/端口与固件是否运行中。

---

## 日志输出

- 文件路径：编辑器 `Assets/Resources/Logs/yyyy_MM_dd_HH_mm_rec.txt`；构建 `数据目录/Logs/` 下同名文件。
- 每个 FixedUpdate 一行：时间差 / 给水模式 / trial / context / 相对位置 / 编码器原始值 / 速度 / 区前-区内-区后舔水计数 / 正确舔水次数 / 出水次数。
- **Esc 退出时会把界面日志窗口全文追加写入该文件**——现场排查请保留该文件。

---

## 故障排除

1. **连接慢 / 连不上**
   - 正确端口排在后面时，每个无响应端口都要等满超时：在 `[serialSettings]` 设 `recommendPort=COMx` 或 `blackList` 排除虚拟口。
   - 报 `Incompatible version`：`compatibleVersion` 白名单与固件版本不符（留空可跳过校验）。
   - 反复 `Unexpected response / handshake timeout`：给 Due 断电重连后再试；检查波特率是否 115200。
2. **画面不动**：编码器无数据（检查轨道接线与 `recommendPort`）；或速度始终低于 `IFThreshold_x`。
3. **不出水**：确认小鼠在奖励区内（LightGreen 亮）；确认当前给水模式与 `Mode1Config` 参数；mode 0 检查舔水上报是否到达（左侧应出现红线）。
4. **trial 不推进 / 卡在结束画面**：`end_arg` 等待期正常持续，等待结束自动进入下一 context；会话跑满 `repeat_num` 后停在结束画面属正常，需 Esc 退出。
5. **验证失败日志频繁出现**：优先怀疑 USB 连接不稳或固件复位过，重连后系统会自动同步状态。

---

## 版本历史

有关详细更改，请参见 git 日志。
