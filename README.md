示例config.ini:

[settings]
start_mode = 0x11
#0x00与0x10为trial开始时给水，0x01和0x11为结算时给水，第一位为0不考虑trial正确与否，第一位为1按trial结果给水
triggerMode = 1
#0为延时开始下一个trial，1为红外主动触发，2为开关触发
start_method = random         
#random为在available_pos中随机选取位置，assign为按照assign_pos开始trial
assign_pos = 75..
#int 指定角度0-360，".."表示后续均指定为此角度
available_pos = 120,240
#从0-359映射到所有display_pixels
pump_pos = 0,1
#每个available_pos对应的泵的编号
lick_pos = 0,1
#每个available_pos对应的的TouchPannel编号
max_trial = 1000
#暂时弃用
barDelayTime = 0
#bar出现后等待x秒内忽略小鼠舔的行为，float
barLastingTime = 1
#trial成功结束时bar维持x秒，float
soundLength = 0
#SoundCue持续时间，设0则无SoundCue
triggerModeDelay = random2~3
#主动触发模式（1和2）下，触发后x秒开始trial，float，可设随机范围（格式为 "randomX~Y",X与Y为设定范围秒数）或指定值（格式为 "X"，X为设定秒数）
trialInterval = random4~5
#每个trial之间间隔时间，延时模式（0）下决定下一个trial开始时间，主动触发模式下决定每个trial之间最小间隔，随机值或指定值设置方法同上
success_wait_sec = 5  
#trialInterval设0时，trial等待时间按照trial判断结果进行延时，模式0x0_时默认正确
fail_wait_sec = 8
#trialInterval设0时，trial等待时间按照trial判断结果进行延时，模式0x0_时默认正确
waitFromLastLick = 1
#设大于0值时，trial将开始时若小鼠有舔水嘴动作，则延迟trial，x秒后再开始trial，float
trialExpireTime = 999
#暂时弃用
seed = -1
#-1为随机种子


[displaySettings]
bar_width = 300
#设定bar展示宽度
displayPixels = 2048
#屏幕水平像素数
isRing = true
#是否环形屏

[barSettings]
isDriftgrating = true
#展示视觉刺激是否为drift，若是则按照以下几个参数设定，否则按照bar_material设定bar外观
isCircleBar = false
#展示bar是否为圆形
speed = 0.5
frequency = 4
direction = right
#direction为left时，垂直bar向左运动，水平bar向上运动，为right时反之
horizontal = 1
#0时driftgrating水平运动，1时上下运动
barMaterial = spr_slash
#bar外观设置，可设指定文件名按照给定图片设置bar外观，或设指定颜色如"#000000"为黑色，"#FFFFFF"为白色
centerShaft = true
#是否展示中心参考bar
centerShaftPos = 180
#中心参考bar位置
centerShaftMat = #FFFFFF
#中心参考bar外观，设置方法同上

[serialSettings]
blackList = COM1
#若有非arduino的可通讯串口影响程序选择，添加至黑名单以忽略该串口