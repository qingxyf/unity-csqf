# 节点系统研究与落地

## 参考实现

- [OrangeSensei/SlayTheSpireMapGeneration](https://github.com/OrangeSensei06/SlayTheSpireMapGeneration)：把地图生成、节点权重/楼层限制、遭遇数据和地图交互拆开；节点只处理激活状态、图标和路径。
- [renatomarcacini/Unity-Crusade-Map](https://github.com/renatomarcacini/Unity-Crusade-Map)：用可配置的树状地图结构生成节点图，运行时再由地图组件驱动展示。

## 共性模式

1. 地图生成器只产生 `Node` 图和可达关系，不直接实现战斗、商店或事件规则。
2. 节点类型通过权重、楼层限制或固定终点规则生成；具体遭遇数据由独立配置提供。
3. 地图控制器只允许当前可达节点，进入节点后暂停地图交互，内容完成后再恢复导航。
4. 内容控制器需要有明确的进入/完成生命周期，完成回调必须幂等，避免重复点击造成重复奖励或多次推进。

## 本项目落地

- `NodeContentManager` 继续负责 prefab/fallback 路由，并把当前 `Node` 绑定到 `NodeContentController`。
- 篝火、事件、宝藏、商店、普通/精英/Boss 战斗都使用同一生命周期基类。
- `GameManager.TryCompleteCurrentNode` 是唯一的地图推进门闩；过期内容或重复按钮回调会被拒绝。
- 宝藏奖励与战斗胜利奖励保持独立，Boss 完成仍由 GameManager 负责终局场景跳转。
