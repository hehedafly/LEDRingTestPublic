正常开始后的界面：
<br>![image](https://github.com/user-attachments/assets/d8201130-3140-47db-8250-b2074e17fc6d)
<br>按下start后开始整个session
<br>Building版本config文件在路径"Buildings\LEDRingTest_Data\Resources"中，自定义图像在同一路径中
<br>
<br>示例config.ini:
<br>
<br>
<br>[settings]
<br>start_mode = 0x11
<br>#0x00与0x10为trial开始时给水，0x01和0x11为结算时给水，第一位为0不考虑trial正确与否，第一位为1按trial结果给水
<br>triggerMode = 1
<br>#0为延时开始下一个trial，1为红外主动触发，2为开关触发
<br>start_method = random         
<br>#random为在available_pos中随机选取位置，如random或"random20,290"，assign为按照assign_pos开始trial
<br>available_pos = 120,240
<br>#从0-359映射到所有display_pixels
<br>assign_pos = 75..
<br>#int 指定角度0-360，".."表示后续均指定为此模式，X*Y表示X角度重复Y次，(A-B-C)*Y表示以A,B,C等角度为单元重复Y次，需要与"*"联用
<br>#如 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*100,1*100,0*50,1*50.. or(0-1)*50,(2-3)*50 or (0-1)..
<br>pump_pos = 0,1
<br>#每个available_pos对应的泵的编号
<br>lick_pos = 0,1
<br>#每个available_pos对应的的TouchPannel编号
<br>MatStartMethod = random
<br>#设置每个trial中bar的材质，规则同上
<br>MatAssign = barMat..
<br>#选择参照下面的matSettings，设置方式同assign_pos
<br>MatAvailable = barMat,barMat2
<br>max_trial = 1000
<br>#暂时弃用
<br>barDelayTime = 0
<br>#bar出现后等待x秒内忽略小鼠舔的行为，float
<br>barLastingTime = 1
<br>#trial成功结束时bar维持x秒，float
<br>soundLength = 0
<br>#SoundCue持续时间，设0则无SoundCue
<br>triggerModeDelay = random2\~3
<br>#主动触发模式（1和2）下，触发后x秒开始trial，float，可设随机范围（格式为 "randomX\~Y",X与Y为设定范围秒数）或指定值（格式为 "X"，X为设定秒数）
<br>trialInterval = random4\~5
<br>#每个trial之间间隔时间，延时模式（0）下决定下一个trial开始时间，主动触发模式下决定每个trial之间最小间隔，随机值或指定值设置方法同上
<br>success_wait_sec = 5  
<br>#trialInterval设0时，trial等待时间按照trial判断结果进行延时，模式0x0_时默认正确
<br>fail_wait_sec = 8
<br>#trialInterval设0时，trial等待时间按照trial判断结果进行延时，模式0x0_时默认正确
<br>waitFromLastLick = 1
<br>#设大于0值时，trial将开始时若小鼠有舔水嘴动作，则延迟trial，x秒后再开始trial，float
<br>trialExpireTime = 999
<br>#float 超时未完成则进入下一个trial
<br>backgroundLight = 50
<br>#int 0-255背景亮度
<br>backgroundLightRed = true
<br>#true or false，true则屏幕红色分量为1，false时方便肉眼查看
<br>backgroundMaterial = #000000
<br>#如果为#开头颜色，backgroundLight等效果会最终覆盖，如果为其他，则查询对应名称png，环形屏要求分辨率1920*1080，其他按照副屏分辨率设定
<br>seed = -1
<br>#-1为随机种子
<br>
<br>
<br>[displaySettings]
<br>bar_width = 300
<br>#设定bar展示宽度
<br>displayPixels = 2048
<br>#屏幕水平像素数
<br>isRing = true
<br>#是否环形屏
<br>
<br>[serialSettings]
<br>blackList = COM1
<br>#若有非arduino的可通讯串口影响程序选择，添加至黑名单以忽略该串口
<br>
<br>[matSettings]
<br>matList = default,barMat,barMat2,centerShaftMat,backgroundMat
<br>#若添加其他mat，按照相同格式添加
<br>centerShaft = false
<br>
<br>[barMat]
<br>isDriftGrating = true
<br>#展示视觉刺激是否为drift，若是则按照以下几个参数设定，否则按照bar_material设定bar外观
<br>isCircleBar = false
<br>#展示bar是否为圆形
<br>speed = 0.5
<br>frequency = 4
<br>direction = left
<br>#direction为left时，垂直bar向左运动，水平bar向上运动，为right时反之
<br>horizontal = 0
<br>#0时driftgrating水平运动，1时上下运动
<br>mat = spr_slash
<br>#bar外观设置，若isDriftGrating为false，可设指定文件名按照给定图片设置bar外观，或设指定颜色如"#000000"为黑色，"#FFFFFF"为白色
<br>
<br>[barMat2]
<br>isDriftGrating = true
<br>#展示视觉刺激是否为drift，若是则按照以下几个参数设定，否则按照bar_material设定bar外观
<br>isCircleBar = false
<br>#展示bar是否为圆形
<br>speed = 0.5
<br>frequency = 4
<br>direction = left
<br>#direction为left时，垂直bar向左运动，水平bar向上运动，为right时反之
<br>horizontal = 1
<br>#float, 0时driftgrating水平运动，1时上下运动，中间值斜向运动
<br>mat = spr_slash
<br>#bar外观设置，若isDriftGrating为false，可设指定文件名按照给定图片设置bar外观，或设指定颜色如"#000000"为黑色，"#FFFFFF"为白色
<br>
<br>[centerShaft]
<br>centerShaftPos = 180
<br>#中心参考bar位置
<br>mat = #FFFFFF
<br>
<br>[backgroundMat]
<br>mat = #000000
<br>
<br>[serialSettings]
<br>blackList = COM1
<br>#若有非arduino的可通讯串口影响程序选择，添加至黑名单以忽略该串口
