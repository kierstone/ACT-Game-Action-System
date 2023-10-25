# ACT-Game-Action-System

Action system for a standard ACT game.<br>
The ACT game genre includes Monster Hunter (3D ACT), Street Fighter (FTG, Fighting game) and Castlevania (Platformer).<br>
How does a character in an ACT game change their action?<br>
NOT by state machine, but by CANCEL system.

# 动作游戏动作切换系统

这是一个标准的动作游戏切换系统。<br>
所谓的动作游戏包括了怪物猎人、街霸、恶魔城系列，但是不包括魂系（等即时回合制游戏）。<br>
动作游戏的动作切换，核心依赖于Cancel，所以没有动作帧。<br>
由于是用Unity开发的，不得不妥协于Unity的框架，所以遗憾的是：没法用帧来实现正确的动作游戏动作框架。<br>
但是幸运的是：我们可以顺着Unity凑出一个以动作为单位的框架。<br>
这个框架的配套文章您可以在微信公众号“千猴马的游戏设计之道”中找到，文章中会有比注释更详细的描述。<br>
文章链接：https://mp.weixin.qq.com/s?__biz=MzA3NjQzMzYxMw==&mid=2650635859&idx=1&sn=ba171829af2fc461f5e5be9dcafc804e&chksm=87688ff1b01f06e77d42952eb6207feb736781798c7bdaaeb6df60f40986d6a452e8824ed94c&mpshare=1&scene=23&srcid=1025GxLrb9dTlBpNgWSbaQiN&sharer_shareinfo=7bd6fe24079630c5f2f68701fc31a9a8&sharer_shareinfo_first=7bd6fe24079630c5f2f68701fc31a9a8#rd
