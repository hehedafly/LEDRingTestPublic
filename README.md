# LEDRingTest 文档

## 项目概述

LEDRingTest 是一个基于 Unity 的啮齿动物行为训练系统，提供视觉刺激控制、奖励发放和行为数据收集功能，适用于自动化训练环境。

![系统界面](image.png)

---

## 系统功能

### 核心控制面板

| 按钮/控件 | 功能             | 说明                                   |
|-----------|------------------|----------------------------------------|
| **Start** | 开始训练         | 启动训练会话并播放提示音              |
| **Wait**  | 暂停/继续        | 切换暂停状态（显示"pause"时暂停）     |
| **Finish**| 完成当前试验     | 手动标记当前试验为成功                |
| **Skip**  | 跳过当前试验     | 手动跳过当前试验                      |
| **Exit**  | 退出程序         | 延迟1秒后退出                         |

### 状态显示面板

| 显示项             | 内容                                   |
|--------------------|----------------------------------------|
| **高频信息**       | 会话时间（HH:MM:SS）、FPS             |
| **固定信息**       | 触发模式和间隔信息                    |
| **试验信息**       | 当前试验编号、位置、舔次数、成功/失败统计 |

### 输入字段

| 输入字段           | 功能             | 用法                                   |
|--------------------|------------------|----------------------------------------|
| **Serial Message**| 发送命令         | `/变量=值` - 赋值<br>`//命令` - 原始命令 |
| **Timing (sec)**  | 设置时间（秒）   | 输入秒数后按 Ctrl+Shift+点击目标按钮   |
| **Timing Config** | 导入/导出配置    | 粘贴 JSON 配置以导入，或使用导出按钮   |

### 下拉菜单

| 下拉菜单           | 选项             | 功能                                   |
|--------------------|------------------|----------------------------------------|
| **Mode**          | 试验模式         | 选择试验模式（详见试验模式部分）       |
| **Trigger Mode**  | 触发模式         | 选择触发模式（0=延时，1=红外等）      |
| **Background**    | 背景材料         | 切换背景材质                          |

### 声音控制面板

| 按钮               | 功能             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| **Off**            | 禁用所有声音     | 取消选择其他声音选项                  |
| **BeforeTrial**    | 试验前播放声音   | 试验开始前播放提示音                  |
| **NearStart**      | 接近开始时播放   | 触发延迟后，试验开始前播放            |
| **BeforeGoCue**    | 开始提示前播放   | 试验开始后，开始提示前播放            |
| **BeforeLickCue**  | 启用舔食前播放   | 开始提示后，舔食启用前播放            |
| **InPos**          | 位置正确时播放   | 当鼠标在正确位置时播放（不可播放）    |
| **EnableReward**   | 奖励可用时播放   | 当奖励可以获得时播放                  |
| **AtFail**         | 失败时播放       | 当试验失败时播放                      |

每个声音按钮都有一个嵌入式下拉菜单用于选择音频剪辑。**Shift+Click** 预览声音。

### 设备控制面板

| 按钮               | 功能             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| **OG Start**       | 开始光遗传学     | 持续时间由 OG Time 输入设置            |
| **OG Stop**        | 停止光遗传学     | 立即停止                               |
| **OG Enable**      | 切换 OG 启用     | 绿色=启用，默认=禁用                   |
| **MS Start**       | 开始显微镜       | 持续时间由 MS Time 输入设置            |
| **MS Stop**        | 停止显微镜       | 立即停止                               |
| **MS Enable**      | 切换 MS 启用     | 绿色=启用，默认=禁用                   |

### 仿真控制

| 按钮               | 功能             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| **Lick Spout 1-8** | 模拟舔食         | 在指定的舔管上模拟舔食                |
| **Water Spout 1-8**| 喷水             | 喷水 0.2 秒                           |
| **Water Spout Single 1-8** | 单次喷水 | 同上                                   |
| **Shift+Click Water Spout** | 冲洗    | 冲洗水喷嘴 (`/p_water_flush[n]=1`)   |
| **IR In**          | 模拟红外传感器   | 触发入口检测                          |
| **Press Lever**    | 模拟压杆         | 触发压杆按下检测                      |

### 实用按钮

| 按钮               | 功能             |
|--------------------|------------------|
| **Debug**          | 切换调试模式（绿色=启用） |
| **IPC Refresh**    | 刷新 IPC 连接（绿色=连接中） |
| **IPC Disconnect** | 断开 IPC 连接     |
| **Page Up/Down**   | 导航日志页面      |
| **Timing Export**  | 导出时间配置到输入字段 |
| **Timing Pause**   | 暂停/恢复所有定时警报 |
| **Message Post**   | 发送微信通知      |

### 灯光指示器

| 灯光               | 颜色             | 意义                                   |
|--------------------|------------------|----------------------------------------|
| **Lick**           | 绿色（开）/灰色（关） | 检测到舔食                             |
| **Reward**         | 青色（开）/灰色（关） | 奖励已发放                             |
| **Stop**           | 红色（开）/灰色（关） | 停止信号                               |

### 日志窗口

- 实时显示带时间戳的日志消息
- 可通过 Page Up/Down 按钮滚动
- 非手动滚动时自动滚动

---

## 配置文件（config.ini）

配置文件位于：
- **编辑器**: `Assets/Resources/config.ini`
- **构建**: `Buildings/LEDRingTest_Data/Resources/config.ini`

### 值格式：随机范围

许多参数支持使用 `randomX~Y` 格式的随机值指定：
- `random2.5~4` - 2.5 到 4 之间的随机值
- `5` - 固定值 5

### 设置部分

| 参数               | 格式             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `start_mode`       | hex (0x00, 0x01, 0x10, 0x11, 0x21, 0x22) | 试验奖励模式。**第一个十六进制数字**：0x0_=试验开始时奖励，0x1_=成功时奖励。**第二个十六进制数字**：_0=任意舔食推进，_1=正确舔食推进，_2=基于位置推进 |
| `triggerMode`      | int (0-4)        | 0=延时，1=红外传感器，2=压杆按下，3=位置信息，4=试验结束 |
| `start_method`     | string           | 位置选择方法：`random` 或 `assign`。在 `random` 后附加位置（例如，`random0,90,180`）以限制随机选择特定索引 |
| `available_pos`    | 以逗号分隔的整数 | 可用的杆位置（以度为单位）。索引映射：0→第一个值，1→第二个，依此类推。 |
| `assign_pos`       | pattern          | 位置分配模式（见位置分配语法部分）    |
| `barShiftLs`       | pattern or randomX~Y | 每个试验的杆位置偏移（支持与 `assign_pos` 相同的模式，或 `random-80~80` 的随机范围） |
| `barOffset`        | int              | 以度为单位的常量显示偏移（添加到所有位置） |
| `pump_pos`         | 以逗号分隔的整数 | 每个 `available_pos` 索引的泵编号（数量必须 ≥ `available_pos` 数量，或为空以自动索引 0,1,2...） |
| `lick_pos`         | 以逗号分隔的整数 | 每个 `available_pos` 索引的触摸面板/舔喷嘴编号（与 `pump_pos` 相同规则） |
| `MatStartMethod`   | string           | 材料选择方法：`random` 或 `assign`   |
| `MatAssign`        | pattern          | 材料分配模式（与 `assign_pos` 相同语法） |
| `MatAvailable`     | 以逗号分隔的字符串 | 可用的材料名称（必须与 `[matSettings] matList` 匹配） |
| `max_trial`        | int              | 最大试验次数                           |
| `barDelayTime`     | float            | 试验之间的最小时间（主动触发模式）    |
| `barLastingTime`   | float            | 成功试验后杆显示持续时间（0=立即隐藏） |
| `triggerModeDelay` | randomX~Y or float | 试验开始前的延迟（触发模式 0：声音提示前导时间；模式 1-3：触发后延迟） |
| `trialInterval`    | randomX~Y or float | 试验之间的间隔（模式 0：实际间隔；模式 1-4：最小间隔） |
| `success_wait_sec` | randomX~Y or float | 成功后的等待时间（在未设置 `trialInterval` 时使用） |
| `fail_wait_sec`   | randomX~Y or float | 失败后的等待时间（在未设置 `trialInterval` 时使用） |
| `waitFromStart`    | randomX~Y or float | 试验开始后提示开始的延迟             |
| `waitFromLastLick` | float            | 如果鼠标在会话开始时舔食，则延迟试验开始 |
| `trialExpireTime`  | float            | 自动失败前的试验超时                 |
| `backgroundLight`  | int (0-255)      | 背景亮度级别                          |
| `backgroundLightRed`| int (-1 to 255) | 红色分量覆盖；-1=灰度模式，0-255=固定红色级别 |
| `seed`             | int              | 随机种子（-1=使用当前时间）         |
| `standingSecInTrial`| float           | 在触发/目标区域内所需的站立时间      |

### 位置/材料分配语法

由以下参数使用：`assign_pos`，`MatAssign`，`barShiftLs`（不使用随机范围时）

| 模式               | 示例             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| 单一值             | `0`              | 在一个试验中使用索引 0                 |
| 逗号序列           | `0,1,2,3`        | 按顺序使用索引 0,1,2,3                 |
| 重复与 `..`        | `0..`            | 对所有剩余试验使用索引 0               |
| 乘法器             | `0*10`           | 对 10 个试验使用索引 0                 |
| 和模式             | `(0+90+180+270)*4..` | 将和视为序列，重复 4 次，然后继续     |
| 范围模式           | `(0-1-2)*5..`    | 序列 0,1,2 重复 5 次，然后继续        |
| 组合               | `0*30,1*30`      | 索引 0 进行 30 次试验，然后索引 1 进行 30 次试验 |
| 随机单位           | `random0~20*10`  | 在 10 个试验中随机值 0-20（仅在 `barShiftLs` 中） |

**注意**：位置索引指的是 `available_pos` 索引（0-7），而不是角度。材料索引指的是 `MatAvailable`。

### 设备触发方法

格式：`[start]{...};[end]{...}` - 每个部分包含由 `|` 分隔的 `eventType:trialPattern`

**事件类型**：`certainTrialStart`，`everyTrialStart`，`certainTrialEnd`，`everyTrialEnd`，`certainTrialInTarget`，`everyTrialInTarget`，`nextTrialStart`，`nextTrialEnd`，`nextTrialInTarget`

**试验模式** 支持：
- `n` - 每第 n 个试验：`80*n` → 试验 80, 160, 240...
- `*(n+1)` - 偏移 1：`80*(n+1)` → 试验 80, 161, 242...
- 组合：`80*n40+` → 试验 120, 200, 280... (80×(n+0)+40)
- `~length` - 范围：`1~3` → 3 个连续试验
- `+offset` / `-offset` - 加/减偏移量
- 以逗号分隔的值：`10,20,30`
- `0` 或 `1` - 特殊值（0=不活动，1=活动，用于 `every...` 事件）

**示例**：`OGTriggerMethod=[start]{everyTrialEnd:30};[end]{nextTrialInTarget:0}`
- 在试验 30 结束时启动 OG
- 在下一个试验目标发生时结束 OG

### 附加设置

| 参数               | 格式             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `refSegement`      | int              | 参考段角度（0-359）                   |
| `destAreaFollow`   | bool             | 如果为 `true`，目标区域随杆位置旋转；如果为 `false`，使用固定区域 |
| `extraRewardTimeInSec` | float         | 试验成功后额外奖励期的持续时间（0=禁用，-2=无限制） |
| `stopExtraRewardMethod` | 以逗号分隔的字符串 | 停止额外奖励的方法：`lick`，`pos`，或 `lick,pos` |
| `stopExtraRewardUseTriggerSelectArea` | int | 用于基于位置的停止的触发区域索引 |
| `stopExtraRewardLickDelaySec` | float   | 在舔食后停止额外奖励的延迟           |
| `ServeRandomRewardAtEnd` | randomX~Y or int | 会话结束时提供的随机奖励数量       |
| `refSegement`      | int              | 参考段角度（0-359）                   |
| `checkConfigContent` | bool            | 启用配置验证检查                     |
| `openLogevent`     | bool             | 启用通过 LogEvent.exe 的外部日志记录 |
| `logEventPath`     | string           | LogEvent.exe 的路径                   |
| `openPythonScript` | bool             | 启用 Python 脚本集成（用于视频跟踪） |
| `PythonScriptCommand` | string         | Python 脚本命令行                     |
| `closePythonScriptBeforeExit` | bool    | 退出时关闭 Python 脚本               |

### 声音设置

| 参数               | 格式             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `soundLength`      | float            | 声音提示持续时间（秒）（0=禁用）     |
| `cueVolume`        | float (0-1)      | 提示音量（0=静音，1=最大）           |
| `TrialSoundPlayMode` | string         | 格式：`模式编号:声音名称`（例如，`6:6000hz`）。模式：0=关闭，1=试验前，2=接近开始，3=开始提示前，4=启用舔食前，5=在位时，6=奖励可用时，7=失败时。多个用 `;` 分隔 |
| `alarmPlayTimeInterval` | float       | 警报声音之间的最小间隔（秒，仅在 `soundLength` > 0 时使用） |

### 显示设置

| 参数               | 类型             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `barWidth`         | int              | 杆显示宽度（像素）                   |
| `barHeight`        | int              | 杆显示高度（像素）                   |
| `displayPixelsLength` | int            | 屏幕水平宽度（像素）                 |
| `displayPixelsHeight` | int           | 屏幕垂直高度（像素）                 |
| `isRing`           | bool             | 启用环形显示模式（重复杆模式）       |
| `separate`         | bool             | 使用单独的显示区域                   |
| `displayVerticalPos` | float (0-1)    | 垂直屏幕位置（0.5=居中）            |

### 串口设置

| 参数               | 格式             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `serialSpeed`      | int              | 波特率（例如，250000）                |
| `blackList`        | 以逗号分隔的字符串 | 要忽略的 COM 端口（例如，`COM1,COM3`） |
| `compatibleVersion` | 以逗号分隔的字符串 | 接受的 Arduino 固件版本（例如，`V2.2`） |

### 材料设置

| 参数               | 格式             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `matList`          | 以逗号分隔的字符串 | 配置中定义的所有可用材料名称         |
| `centerShaft`      | bool             | 启用中心参考轴显示                   |

#### 每种材料的属性（例如，`[barMat]`，`[barMat2]`，`[centerShaft]`）

| 参数               | 类型             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `isDriftGrating`   | bool             | 使用动画漂移格栅刺激                   |
| `isCircleBar`      | bool             | 将杆渲染为圆形而不是矩形             |
| `speed`            | float            | 漂移动画速度                          |
| `frequency`        | int              | 格栅条纹频率                          |
| `direction`        | `left` or `right` | 漂移方向                             |
| `horizontal`       | float (0-1)      | 运动角度：0=水平，1=垂直，介于两者之间=对角线 |
| `mat`              | string or #RRGGBB | 纹理名称（不带 .png）或十六进制颜色代码 |

### 默认选项设置

存储在 `InputfieldContent` 中，作为 JSON 编码的定时链，用于自动化 UI 控制。

格式：`IFTimingSet=>JSON1|||JSON_RECORD|||JSON2|||JSON_RECORD|||...`

---

## UI 控件参考

### 核心按钮

| 按钮名称           | 功能             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `StartButton`      | 开始会话         | 开始试验会话并播放提示音              |
| `WaitButton`       | 暂停/继续        | 切换暂停状态                          |
| `FinishButton`     | 完成试验         | 手动完成当前试验                      |
| `SkipButton`       | 跳过试验         | 跳过当前试验                          |
| `ExitButton`       | 退出             | 退出应用程序（延迟1秒）               |
| `DebugButton`      | 调试模式         | 切换调试模式                          |

### 输入字段

| 输入字段           | 功能             | 用法                                   |
|--------------------|------------------|----------------------------------------|
| `IFSerialMessage`  | 串口命令         | 发送命令到 Arduino（前缀 `/` 表示变量赋值，`//` 表示原始命令） |
| `IFTimingBySec`    | 定时（秒）       | 以秒为单位设置按钮定时               |
| `IFTimingByTrial`  | 定时（试验）     | 按试验计数设置按钮定时               |
| `IFTimingSet`      | 定时配置         | 导入/导出定时配置                     |
| `OGTime` / `MSTime`| 设备持续时间     | 设置光遗传学/显微镜持续时间（毫秒）  |
| `MouseInfoName`    | 鼠标名称         | 设置鼠标标识符                        |
| `MouseInfoIndex`   | 鼠标索引         | 设置鼠标索引                          |

### 下拉菜单

| 下拉菜单           | 选项             | 功能                                   |
|--------------------|------------------|----------------------------------------|
| `ModeSelect`       | 试验模式         | 选择试验模式（0x00, 0x01, 0x10, 0x11, 0x21, 0x22） |
| `TriggerModeSelect`| 触发模式         | 选择触发模式（0-4）                   |
| `BackgroundSwitch` | 材料             | 切换背景材质                          |
| `TimingBaseDropdown` | 定时配置       | 配置按钮定时层级                      |

### 声音控制（声音选项）

| 按钮               | 功能             |
|--------------------|------------------|
| `soundOff`         | 禁用所有声音     |
| `soundBeforeTrial` | 试验前播放声音   |
| `soundNearStart`   | 接近开始时播放   |
| `soundBeforeGoCue` | 开始提示前播放   |
| `soundBeforeLickCue` | 启用舔食前播放 |
| `soundInPos`       | 位置正确时播放   |
| `soundEnableReward`| 奖励可用时播放   |
| `soundAtFail`      | 失败时播放       |

每个声音按钮都有一个嵌入式下拉菜单用于选择音频剪辑。

### 设备控制

| 按钮               | 功能             |
|--------------------|------------------|
| `OGStart`          | 开始光遗传学设备 |
| `OGStop`           | 停止光遗传学设备 |
| `OGEnable`         | 切换光遗传学启用 |
| `MSStart`          | 开始显微镜设备   |
| `MSStop`           | 停止显微镜设备   |
| `MSEnable`         | 切换显微镜启用   |

### 舔喷嘴仿真

| 按钮               | 功能             |
|--------------------|------------------|
| `LickSpout1-8`     | 在喷嘴 1-8 上模拟舔食 |

### 喷水控制

| 按钮               | 功能             |
|--------------------|------------------|
| `WaterSpout1-8`    | 在喷嘴 1-8 上喷水 |
| `WaterSpoutSingle1-8` | 单次喷水模式   |
| Shift + Click       | 冲洗水喷嘴       |

### 定时控制

| 按钮               | 功能             |
|--------------------|------------------|
| `TimingConfigExport` | 导出定时配置到输入字段 |
| `TimingPause`      | 暂停/恢复所有定时警报 |

### 实用按钮

| 按钮               | 功能             |
|--------------------|------------------|
| `PageUp` / `PageDown` | 导航日志页面   |
| `IPCRefreshButton` | 刷新 IPC 连接    |
| `IPCDisconnect`    | 断开 IPC 连接    |
| `MessagePost`      | 发送微信通知     |

---

## ContextInfo 属性（运行时可修改）

这些属性可以在调试模式启用时通过串口命令 `///variableName=value` 在运行时修改。

| 属性               | 类型             | 说明                                   |
|--------------------|------------------|----------------------------------------|
| `startMethod`      | string           | 位置选择方法                          |
| `avaliablePosDict` | Dictionary<int,int> | 可用位置映射                          |
| `matStartMethod`   | string           | 材料选择方法                          |
| `matAvaliableArray`| List<string>     | 可用材料                              |
| `lickPosLs`        | List<int>       | 舔食位置分配                          |
| `pumpPosLs`        | List<int>       | 泵位置分配                            |
| `trackMarkLs`      | List<int>       | 跟踪标记分配                          |
| `barShiftLs`       | List<int>       | 杆偏移值                              |
| `barOffset`        | int              | 显示偏移                              |
| `destAreaFollow`   | bool             | 跟踪实际杆位置                        |
| `standingSecInTrigger` | float        | 在触发区域的站立时间                  |
| `standingSecInDest`| float            | 在目标区域的站立时间                  |
| `maxTrial`         | int              | 最大试验次数                          |
| `seed`             | int              | 随机种子                              |
| `barDelayTime`     | float            | 杆延迟时间                            |
| `barLastingTime`   | float            | 杆持续时间                            |
| `waitFromStart`    | List<float>      | 开始提示延迟范围                      |
| `waitFromLastLick` | float            | 最后舔食后的延迟                      |
| `backgroundLight`  | int              | 背景亮度                              |
| `backgroundRedMode`| int              | 红色分量                              |
| `trialInterval`    | List<float>      | 试验间隔                              |
| `sWaitSec`         | List<float>      | 成功等待时间                          |
| `fWaitSec`         | List<float>      | 失败等待时间                          |
| `trialTriggerMode` | int              | 触发模式                              |
| `trialTriggerDelay`| List<float>      | 触发延迟                              |
| `trialExpireTime`  | float            | 试验超时                              |
| `soundLength`      | float            | 声音持续时间                          |
| `cueVolume`        | float            | 声音音量                              |
| `countAfterLeave`  | bool             | 离开后计算舔食                        |
| `extraRewardTimeInSec` | float        | 额外奖励时间                          |
| `stopExtraRewardMethod` | string       | 额外奖励停止方法                      |
| `stopExtraRewardUseTriggerSelectArea` | int | 停止区域索引                  |
| `stopExtraRewardLickDelaySec` | float   | 停止舔食延迟                        |
| `minIgnoreLickInterval` | float       | 最小忽略间隔                          |
| `maxExtraRewardCount` | int           | 最多额外奖励                          |
| `randomRewardPerTrial` | List<int>    | 每个试验的随机奖励                    |
| `userName`         | string           | 用户/鼠标名称                        |
| `mouseInd`         | string           | 鼠标索引                              |

---

## 串口命令

### 变量赋值
```
/variableName=value
```
示例：`/p_water_flush[1]=1`

### 原始命令
```
//command
```

### 运行时属性修改（仅限调试模式）
```
///variableName=value
```

### 帮助命令
```
///help
```
列出所有可用属性及其类型。

---

## 试验模式

| 模式                 | 十六进制 | 奖励时机         | 推进条件               |
|----------------------|----------|------------------|------------------------|
| 始终，任意舔食       | 0x00    | 试验开始时       | 任意舔食推进           |
| 基于结果，正确舔食   | 0x01    | 仅在成功时       | 仅正确舔食推进         |
| 始终，完成           | 0x10    | 试验开始时       | 必须在位置内完成       |
| 基于结果，完成       | 0x11    | 仅在成功时       | 必须在位置内完成       |
| 始终，基于位置       | 0x21    | 试验开始时       | 基于位置的推进         |
| 基于结果，基于位置   | 0x22    | 仅在成功时       | 基于位置的推进         |

**十六进制数字解析**：`0xAB`
- **A（第一个数字）**：奖励时机 - `0`=在试验开始时，`1`=在成功时
- **B（第二个数字）**：推进条件 - `0`=任意舔食，`1`=正确舔食，`2`=基于位置

---

## 触发模式

| 模式 | 说明             |
|------|------------------|
| 0    | 延时 - 试验之间固定间隔 |
| 1    | 红外传感器 - 红外传感器触发试验 |
| 2    | 压杆按下 - 按下压杆触发试验 |
| 3    | 位置检测 - 视频位置检测 |
| 4    | 试验结束 - 在上一个试验结束后立即开始 |

---

## 位置分配语法

有关完整文档，请参见 [配置 - 位置/材料分配语法](#配置)。

**快速参考**：
- `0..` - 对所有试验重复使用索引 0
- `0*30,1*30` - 对 30 次试验使用索引 0，然后对 30 次试验使用索引 1
- `(0-1-2)*5..` - 序列 0,1,2 重复 5 次，然后继续
- `random0,90,180` - 从指定索引中随机选择

---

## 声音模式

| 模式 | 触发点           |
|------|------------------|
| 0    | 关闭声音         |
| 1    | 试验前           |
| 2    | 接近试验开始（经过延迟） |
| 3    | 开始提示前       |
| 4    | 启用舔食前       |
| 5    | 当在正确位置时   |
| 6    | 当奖励可用时     |
| 7    | 在试验失败时     |

---

## 日志和数据

### 记录类型
- `lick` - 舔食事件
- `start` - 试验开始
- `end` - 试验结束
- `init` - 初始化
- `entrance` - 入口检测
- `press` - 压杆事件
- `lickExpire` - 舔食超时
- `trigger` - 触发事件
- `stay` - 停留事件
- `soundplay` - 声音播放
- `OGManuplate` - 光遗传学操作
- `sync` - 同步事件
- `miniscopeRecord` - 显微镜记录
- `pump` - 泵激活

### 日志文件输出
- 带时间戳的试验信息记录到文件
- 会话结束时以 JSON 格式导出上下文信息
- UI 日志窗口实时显示

---

## 定时系统

### 按钮定时

使用 Ctrl+Shift+Click 设置定时按钮按下，或指定定时值：

#### 按时间（秒）
1. 在 `IFTimingBySec` 输入字段中输入值
2. Ctrl+Shift+Click 目标按钮
3. 按钮在指定时间后执行

#### 按试验计数
1. 在 `IFTimingByTrial` 输入字段中输入值
2. Ctrl+Shift+Click 目标按钮
3. 按钮在指定的试验计数后执行

### 层级定时

创建复杂的定时链：
1. 从下拉菜单中选择基础定时
2. 添加子定时和延迟
3. 导出/导入定时配置

### 定时命令
- Ctrl+Click 按钮 - 移除定时
- Shift+Click 声音按钮 - 预览声音

---

## 键盘快捷键

| 键                 | 功能             |
|--------------------|------------------|
| Return             | 执行输入字段命令 |
| Up/Down Arrow      | 导航串口命令历史 |
| Ctrl+Shift+Click   | 设置按钮定时     |
| Shift+Click        | 预览声音         |

---

## 构建配置

自定义图像应放置在与配置文件相同的目录中。

---

## 故障排除

### 常见问题

1. **串口连接失败**
   - 检查 `serialSettings.blackList` 在 config.ini 中的设置
   - 确保 Arduino 已连接

2. **IPC 连接失败**
   - 点击 `IPCRefreshButton` 重新连接
   - 检查 Python 脚本路径

3. **配置解析错误**
   - 验证 config.ini 中的语法
   - 检查参数名称中的拼写错误

4. **位置分配错误或类似问题**
   - 确保位置索引在 `available_pos` 范围内
   - 验证配置中的模式语法

---

## 版本历史

有关详细更改，请参见 git 日志。

---