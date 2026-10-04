# 蓝色大肥鱼战斗验收

蓝色大肥鱼使用 `Assets/Resources/Enemies/RiceKeeper.prefab`，战斗素材是独立的透明底待机图和攻击图，位于 `Assets/Art/Roguelike/Enemies/RiceKeeper/`。艰难战斗复用正式的 `CombatController`、手牌、伤害、敌方回合和奖励流程。

事件战斗沿用原来的对面布局：主角保持场景中的原始位置，大肥鱼使用普通战斗的敌人站位。手牌保留原来的展开宽度与悬停放大效果，选中卡牌时允许临时遮挡人物。

## 独立可玩版本

GitHub Actions 的 **RiceKeeper visual acceptance** 工作流生成 `ricekeeper-visual-preview` 下载产物。它复制 `forth` 场景进行 Development WebGL 构建，附加验收入口；不改动正式场景、正式 WebGL/Windows 构建或事件池随机规则。

解压产物后，在包含 `index.html` 的目录执行 `python -m http.server 8792`，浏览器打开 `http://localhost:8792/`。WebGL 需要 HTTP 服务，不能直接双击 HTML。

1. 野生营地选择「吃光大米饭」，点击继续。
2. 讨债人选择「进入一场艰难的战斗」。
3. 使用手牌或上方攻击按钮，与 240 生命、20 护盾、24 攻击的蓝色大肥鱼实际战斗。

左上角「重新验收」重置这条验收流程。「慢速播放」仅放慢动画和游戏时间，方便查看攻击姿态；不改变伤害、血量或胜负规则。验收使用营地相同的低费初始牌组领取方法。

正式游戏依旧需要先遇到营地并选择吃光，后续事件池才会加入讨债人；每个事件每局仅遇到一次。正式部署继续使用 **Unity CI** 的普通 WebGL 产物。
