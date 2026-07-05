# 星际商店 - Steam 创意工坊置顶讨论帖模板（中英双语）

> 上传发布后，到 Workshop 模组页 → Discussions → 创建以下 4 个主题并置顶（创作者最多可置顶 4 帖）。
> 每个主题给出"中文版"和"English Version"两段，可以分别建中英两个帖子，也可以一个帖子里中英并列。

---

## 📌 帖 1：【请先读】反馈提交模板 / Read First: Feedback Template

### 标题
- 中文：📌【请先读】Bug / 建议反馈提交模板
- English: 📌 Read First — How to Report Bugs & Suggestions

### 正文（中文版）
```
欢迎使用「星际商店」！

为了让我能快速复现并修复问题，请按以下格式提交反馈。
直接评论 / 提问前 30 秒看完这份模板，能让你的反馈被处理得快 10 倍 ❤️

────────────────────────────
【反馈类型】Bug / 建议 / 平衡性 / 兼容性 / 其它
【RimWorld 版本】例：1.6.4518
【星际商店版本】例：1.0.0（见 About.xml）
【启用的其它模组】贴 ModsConfig.xml 列表，或截图 Mod 加载顺序
【问题描述】简短一句话 + 详细复现步骤（1. 2. 3.）
【截图 / 录屏】直接拖入帖子或贴图床链接
【Player.log】见下方"如何上传日志"
────────────────────────────

【如何快速找到 Player.log】
Windows：%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
macOS：~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log
Linux：~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log

【一键打包脚本（Windows，复制到 PowerShell 执行）】
Compress-Archive "$env:USERPROFILE\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log" "$env:USERPROFILE\Desktop\星际商店反馈.zip"
打包后把桌面上的 zip 拖到帖子里即可。

【更推荐：HugsLib Log Publisher】
如果你装了 HugsLib，游戏内按 Ctrl + F12，会自动把日志上传到 Gist 并复制链接，直接贴进帖子。

【其它反馈渠道】
- 中文 QQ 群：[填你的群号]
- GitHub（开发者向）：[填你的仓库地址，可留空]

感谢每一位提交反馈的玩家，下个版本的更新日志里会 @ 致谢。
```

### 正文（English Version）
```
Welcome to Interstellar Store!

To help me reproduce and fix issues quickly, please report using the template below.

────────────────────────────
[Type] Bug / Suggestion / Balance / Compatibility / Other
[RimWorld Version] e.g. 1.6.4518
[Mod Version] e.g. 1.0.0 (see About.xml)
[Other Active Mods] paste your ModsConfig.xml or a screenshot of load order
[Description] one short sentence + numbered reproduction steps
[Screenshot / Clip] drag into the post or paste an imgur link
[Player.log] see "How to grab logs" below
────────────────────────────

[How to grab Player.log]
Windows: %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Player.log
macOS:   ~/Library/Logs/Ludeon Studios/RimWorld by Ludeon Studios/Player.log
Linux:   ~/.config/unity3d/Ludeon Studios/RimWorld by Ludeon Studios/Player.log

[Easy way: HugsLib Log Publisher]
If you have HugsLib installed, press Ctrl + F12 in-game. It uploads your log to Gist
and copies the URL — just paste it in your post.

Thanks to everyone who takes the time to report. Contributors get credited in the changelog.
```

---

## 📌 帖 2：【Bug 汇总】已知问题与修复进度 / Known Issues & Fix Progress

### 标题
- 中文：🐛【Bug 汇总】已知问题与修复进度（持续更新）
- English: 🐛 Known Issues & Fix Progress (Updated Regularly)

### 正文（中文版）
```
本帖列出当前已知问题与修复进度，请在提交新 Bug 前先 Ctrl+F 搜一下。
状态说明：🔴 已确认 / 🟡 修复中 / 🟢 下个版本已修 / ⚪ 待复现

────────────────────────────
🔴 行列数修改后界面不刷新
   触发：在设置里改了网格布局后界面没有立刻应用
   临时方案：点击其它能刷新界面的按钮（如切换分类）
   计划：v1.0.x 修复

🔴 纯室内地图（旅队/房车）买卖会报黄字错误
   触发：在没有"户外/着陆区"的地图执行购买或出售
   临时方案：暂时回到主基地交易
   计划：v1.1 加入地图类型检测，自动禁用按钮

⚪ 其他已上报但未复现的问题
   （留空，随玩家反馈更新）
────────────────────────────

格式：标题 + 触发条件 + 临时解决 + 计划版本
每次发布新版本会刷新本帖，已修复的会标 🟢 并归档到楼层评论里。
```

### 正文（English Version）
```
This thread tracks all known issues and their fix status. Please Ctrl+F here before reporting.
Legend: 🔴 Confirmed / 🟡 In Progress / 🟢 Fixed Next Version / ⚪ Needs Repro

────────────────────────────
🔴 Grid layout change does not refresh the panel
   Trigger: Changing grid size in settings does not apply immediately
   Workaround: Click any other refresh-triggering button (e.g. category tab)
   Planned fix: v1.0.x

🔴 Yellow log errors when buying/selling on fully indoor maps (caravan, RV)
   Trigger: Using the shop on a map with no outdoor / landing area
   Workaround: Trade from your main base for now
   Planned fix: v1.1 — auto-disable the buttons on such maps

⚪ Other reported but not yet reproduced issues
   (will be added as feedback comes in)
────────────────────────────

Fixed items are marked 🟢 and archived in replies below.
```

---

## 📌 帖 3：【建议征集】功能 & 平衡性建议 / Suggestion Box

### 标题
- 中文：💡【建议征集】想要的功能 / 平衡性建议
- English: 💡 Feature Requests & Balance Suggestions

### 正文（中文版）
```
有想要的功能、价格调整、UI 改进，都欢迎回帖说。
请一条回帖 = 一条建议，方便我点赞和回复。

建议格式：
- 标题（≤15 字）
- 详细描述
- 是否影响存档兼容
- 优先级（个人感受）

我会在已采纳的建议下打 👍 并标记目标版本。

不会全部采纳，但每条都会读 ❤️
```

### 正文（English Version）
```
Feature ideas, price tweaks, UI improvements — all welcome here.
Please one reply = one suggestion so I can upvote / reply individually.

Suggested format:
- Title (≤15 chars)
- Detailed description
- Save-compatibility concern (yes/no)
- Your priority (low / mid / high)

Adopted suggestions will get a 👍 reply with the target version tag.

I can't accept everything, but I read every single one ❤️
```

---

## 📌 帖 4：【兼容性报告】与其它 Mod 一起使用的情况 / Compatibility Reports

### 标题
- 中文：🔗【兼容性报告】与其它 Mod 一起使用的情况
- English: 🔗 Compatibility Reports with Other Mods

### 正文（中文版）
```
本帖收集星际商店与其它热门 Mod 的兼容性报告。
回帖前请先搜一下相同的 Mod 名称，避免重复。

回帖格式：
【目标 Mod】Mod 名 + 创意工坊链接
【兼容状态】✅ 完全兼容 / ⚠️ 有小问题 / ❌ 冲突
【现象】简短描述
【加载顺序】星际商店在前 / 在后
【RimWorld 版本】

已知兼容性结论：
────────────────────────────
✅ Combat Extended：物品价格正常
✅ Vanilla Expanded 全家桶：分类筛选可识别
⚠️ Hospitality：购买的物品可能被访客拾取（无影响）
（更多结论会随玩家反馈汇总到本帖顶部）
────────────────────────────
```

### 正文（English Version）
```
This thread collects compatibility reports for Interstellar Store + other mods.
Please search this thread before posting a duplicate.

Reply format:
[Target Mod] Mod name + Workshop link
[Status] ✅ Fully Compatible / ⚠️ Minor Issue / ❌ Conflict
[Symptom] short description
[Load Order] Star Store before / after target mod
[RimWorld Version]

Known results so far:
────────────────────────────
✅ Combat Extended — prices behave correctly
✅ Vanilla Expanded series — categories are detected
⚠️ Hospitality — bought items may be picked up by visitors (cosmetic)
(more results will be aggregated at the top of this thread)
────────────────────────────
```

---

## 使用提示
1. 上传发布后先把模组改成"仅好友可见"，在 Workshop 页右下角创建以上 4 个 Discussion 帖并点"Pin"。
2. 顺序建议从上到下：模板 → Bug → 建议 → 兼容性（玩家先看到的是模板）。
3. 中英文可以分两个帖子（推荐，搜索更精准），也可以一个帖子内中英并列。
4. 每次发新版本时去【Bug 汇总】帖刷新状态，并在 Change Notes 里 @ 致谢反馈者。
