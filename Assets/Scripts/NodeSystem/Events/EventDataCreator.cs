using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class EventDataCreator : MonoBehaviour
{
    #if UNITY_EDITOR
    [MenuItem("Tools/Create Default Events")]
    public static void CreateAllEvents()
    {
        string folder = "Assets/Resources/Events";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Resources", "Events");

        AssetDatabase.Refresh();

        Dictionary<string, string> illustrationPaths = new Dictionary<string, string>
        {
            { "古老祭坛", "Assets/Sprites/Events/event_ancient_altar.png" },
            { "受伤的旅人", "Assets/Sprites/Events/event_wounded_traveler.png" },
            { "魔法泉水", "Assets/Sprites/Events/event_magic_fountain.png" },
            { "赌徒的挑战", "Assets/Sprites/Events/event_gambler_challenge.png" },
            { "被遗弃的宝箱", "Assets/Sprites/Events/event_abandoned_chest.png" },
            { "被遗忘的图书馆", "Assets/Sprites/Events/event_forgotten_library.png" },
            { "黑暗契约", "Assets/Sprites/Events/event_dark_pact.png" },
            { "流浪商人", "Assets/Sprites/Events/event_wandering_merchant.png" },
            { "元素神龛", "Assets/Sprites/Events/event_element_shrine.png" },
            { "镜中迷宫", "Assets/Sprites/Events/event_mirror_maze.png" },
            { "无声花园", "Assets/Sprites/Events/event_silent_garden.png" },
            { "暴风断桥", "Assets/Sprites/Events/event_storm_bridge.png" }
        };

        foreach (EventData evt in EventLibrary.CreateDefaultEvents())
        {
            if (illustrationPaths.TryGetValue(evt.eventName, out string illustrationPath))
                evt.illustration = AssetDatabase.LoadAssetAtPath<Sprite>(illustrationPath);

            string assetPath = $"{folder}/Event_{evt.eventName}.asset";
            if (AssetDatabase.LoadAssetAtPath<EventData>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(evt, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("12 个默认事件创建完成！在 Resources/Events/ 下查看。");
    }

    // === 事件1：古老祭坛 ===
    static void CreateMysteriousAltar(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "古老祭坛";
        evt.description = "你发现了一座被藤蔓缠绕的古老祭坛，上面刻满了难以辨认的符文。\n祭坛散发着微弱的光芒，似乎在等待某种献祭。";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "献上鲜血",
                resultDescription = "你割破手掌，将鲜血滴在祭坛上。符文亮起，一股力量涌入你的身体。\n你感觉到自己的生命上限提升了！",
                healthChange = -15,
                maxHealthChange = 10
            },
            new EventChoice
            {
                buttonText = "祈祷",
                resultDescription = "你虔诚地跪在祭坛前祈祷。温暖的光芒笼罩了你，伤口正在愈合。",
                healthChange = 30
            },
            new EventChoice
            {
                buttonText = "离开",
                resultDescription = "你决定不冒险，默默离开了祭坛。"
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_古老祭坛.asset");
    }

    // === 事件2：受伤的旅人 ===
    static void CreateWoundedTraveler(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "受伤的旅人";
        evt.description = "路边躺着一个奄奄一息的旅人，他身上有战斗留下的伤痕。\n他虚弱地向你伸出手，似乎在请求帮助。";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "分享你的药水",
                resultDescription = "旅人喝下药水后恢复了一些元气。作为感谢，他将自己珍藏的卡牌送给了你。\n\"这些是我冒险生涯的全部积蓄，希望对你有用。\"",
                healthChange = -10,
                cardsToDraw = 3
            },
            new EventChoice
            {
                buttonText = "搜刮他的物品",
                resultDescription = "你翻遍了旅人的背包，找到了一些有用的东西。\n旅人绝望的眼神让你心里不太好受。",
                cardsToDraw = 2,
                maxHealthChange = -5
            },
            new EventChoice
            {
                buttonText = "匆匆离开",
                resultDescription = "你假装没看到，加快脚步离开了。"
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_受伤的旅人.asset");
    }

    // === 事件3：魔法泉水 ===
    static void CreateMagicFountain(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "魔法泉水";
        evt.description = "你来到了一处被魔力笼罩的泉水边。泉水闪烁着奇异的颜色，\n红色的水面下似乎藏着某种力量。一块石碑上刻着：\"饮者必变。\"";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "豪饮泉水",
                resultDescription = "泉水的力量在你体内奔涌！你感觉焕然一新，生命力回满了！",
                healToFull = true
            },
            new EventChoice
            {
                buttonText = "小心啜饮",
                resultDescription = "你小心地喝了一小口。温和的魔力修复了你的一些伤势，还获得了一层护盾。",
                healthChange = 20,
                shieldGain = 15
            },
            new EventChoice
            {
                buttonText = "用泉水清洗卡牌",
                resultDescription = "你将卡牌浸入泉水中。一些卡牌被泉水溶解了，但幸存的卡牌似乎变得更纯净了。",
                cardsToRemove = 2,
                cardsToDraw = 1
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_魔法泉水.asset");
    }

    // === 事件4：赌徒的挑战 ===
    static void CreateGamblerChallenge(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "赌徒的挑战";
        evt.description = "一个戴着面具的神秘人挡住了你的去路。\n\"和我玩个游戏吧，赢了你拿走一切，输了……嘿嘿。\"";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "接受挑战（高风险）",
                resultDescription = "命运之轮转动……你赢了！大量卡牌和生命值涌入！",
                healthChange = 25,
                cardsToDraw = 4,
                isGamble = true,
                gambleSuccessRate = 0.45f,
                gambleFailText = "命运之轮转动……你输了。神秘人哈哈大笑，带走了你的一部分生命力。",
                gambleFailHealthChange = -30
            },
            new EventChoice
            {
                buttonText = "小赌怡情",
                resultDescription = "你选择了小赌注。运气不错！得到了一些回报。",
                cardsToDraw = 2,
                isGamble = true,
                gambleSuccessRate = 0.65f,
                gambleFailText = "可惜，运气不在你这边。损失不大，但还是有点肉疼。",
                gambleFailHealthChange = -10
            },
            new EventChoice
            {
                buttonText = "\"我不赌。\"",
                resultDescription = "神秘人耸耸肩，消失在了黑暗中。\n\"无趣的人。\""
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_赌徒的挑战.asset");
    }

    // === 事件5：被遗弃的宝箱 ===
    static void CreateAbandonedChest(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "被遗弃的宝箱";
        evt.description = "一个精美的宝箱丢弃在路边，锁已经被撬开。\n箱子周围有一些细小的划痕，看起来像是陷阱的痕迹。";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "直接打开",
                resultDescription = "幸运！宝箱里装满了好东西！",
                cardsToDraw = 3,
                isGamble = true,
                gambleSuccessRate = 0.5f,
                gambleFailText = "\"咔哒\"——毒针从锁孔中弹出！你中了陷阱。",
                gambleFailHealthChange = -20
            },
            new EventChoice
            {
                buttonText = "小心检查后打开",
                resultDescription = "你仔细检查后安全地打开了宝箱，虽然里面的东西不多，但至少没有受伤。",
                cardsToDraw = 1,
                healthChange = 5
            },
            new EventChoice
            {
                buttonText = "不碰，绕道走",
                resultDescription = "你的直觉告诉你不要碰它。你选择了安全的道路。",
                healthChange = 5
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_被遗弃的宝箱.asset");
    }

    // === 事件6：被遗忘的图书馆 ===
    static void CreateForgottenLibrary(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "被遗忘的图书馆";
        evt.description = "你误入了一座藏在地下的古老图书馆。灰尘覆盖的书架上摆满了泛黄的魔法典籍。\n一本打开的书自行翻动着书页，似乎在邀请你阅读。";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "研读禁忌魔法",
                resultDescription = "你花了很长时间研读那些晦涩的符文。知识以疼痛的代价刻入了你的记忆——\n你领悟到了强大的卡牌技巧，但精神上的负担让你感到虚弱。",
                healthChange = -20,
                cardsToDraw = 3,
                maxHealthChange = 5
            },
            new EventChoice
            {
                buttonText = "翻阅治愈术手册",
                resultDescription = "你找到了一本治愈术的基础教程，按照书中的方法治疗了自己的伤势。",
                healthChange = 25
            },
            new EventChoice
            {
                buttonText = "整理书架（精简牌组）",
                resultDescription = "你帮图书馆整理了书架。在这个过程中，你也审视了自己的牌组，\n去掉了一些不太有用的卡牌，让策略更加精炼。",
                cardsToRemove = 3
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_被遗忘的图书馆.asset");
    }

    // === 事件7：黑暗契约 ===
    static void CreateDarkPact(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "黑暗契约";
        evt.description = "一道紫色的裂隙出现在你面前，一个低沉的声音从中传出：\n\"凡人，我可以给你力量，但一切都有代价……\"";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "交出生命换取力量",
                resultDescription = "暗影的力量涌入你的身体！你的最大生命值永久降低了，\n但获得了强大的卡牌作为补偿。",
                maxHealthChange = -15,
                cardsToDraw = 4
            },
            new EventChoice
            {
                buttonText = "献祭卡牌换取生命",
                resultDescription = "你将手中的卡牌投入裂隙。暗影吞噬了它们，\n作为交换，你的生命上限提升了。",
                cardsToRemove = 3,
                maxHealthChange = 15,
                healthChange = 15
            },
            new EventChoice
            {
                buttonText = "拒绝契约",
                resultDescription = "\"可惜……\"裂隙缓缓闭合。你松了一口气，但总觉得错过了什么。"
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_黑暗契约.asset");
    }

    // === 事件8：流浪商人 ===
    static void CreateWanderingMerchant(string folder)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = "流浪商人";
        evt.description = "一个背着巨大背包的商人坐在路边休息。\n\"嘿，旅行者！我这里有些好东西，用你的生命力来换如何？\n别担心，公平交易，童叟无欺！\"";

        evt.choices = new List<EventChoice>
        {
            new EventChoice
            {
                buttonText = "\"给我看看好货\"",
                resultDescription = "商人从背包深处掏出几张闪闪发光的卡牌。\n\"这些可是稀有货色！\"",
                healthChange = -15,
                cardsToDraw = 3
            },
            new EventChoice
            {
                buttonText = "\"帮我清理背包\"",
                resultDescription = "商人帮你审视了背包里的卡牌。\n\"这些没用的东西都扔了吧，轻装上阵！\"他还顺手帮你包扎了伤口。",
                cardsToRemove = 2,
                healthChange = 15
            },
            new EventChoice
            {
                buttonText = "聊聊天就走",
                resultDescription = "你和商人闲聊了几句。他分享了一些旅途中的经验，\n你觉得精神振奋了一些。",
                healthChange = 5,
                shieldGain = 10
            }
        };

        AssetDatabase.CreateAsset(evt, $"{folder}/Event_流浪商人.asset");
    }
    #endif
}
