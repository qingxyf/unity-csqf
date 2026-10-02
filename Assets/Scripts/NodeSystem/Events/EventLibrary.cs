using System.Collections.Generic;
using UnityEngine;

public static class EventLibrary
{
    public static List<EventData> CreateDefaultEvents()
    {
        return new List<EventData>
        {
            CreateMysteriousAltar(),
            CreateWoundedTraveler(),
            CreateMagicFountain(),
            CreateGamblerChallenge(),
            CreateAbandonedChest(),
            CreateForgottenLibrary(),
            CreateDarkPact(),
            CreateWanderingMerchant(),
            CreateElementShrine(),
            CreateMirrorMaze(),
            CreateSilentGarden(),
            CreateStormBridge(),
            CreateMoonlitCaravan(),
            CreateWhisperingWell(),
            CreateAshenForge(),
            CreateFallenObservatory(),
            CreateBoundSpirit(),
            CreateSunkenSanctuary()
        };
    }

    private static EventData CreateEvent(string name, string description, params EventChoice[] choices)
    {
        EventData evt = ScriptableObject.CreateInstance<EventData>();
        evt.eventName = name;
        evt.description = description;
        evt.choices = new List<EventChoice>(choices);
        return evt;
    }

    private static EventChoice Choice(string button, string result)
    {
        return new EventChoice { buttonText = button, resultDescription = result };
    }

    private static EventData CreateMysteriousAltar()
    {
        EventChoice blood = Choice("献上鲜血", "符文吸收了鲜血，一股古老的力量强化了你的生命根基。");
        blood.healthChange = -15;
        blood.maxHealthChange = 10;

        EventChoice pray = Choice("祈祷", "温暖的光芒笼罩了你，伤口正在愈合。");
        pray.healthChange = 30;

        return CreateEvent("古老祭坛", "你发现了一座被藤蔓缠绕的祭坛。符文微微发亮，像是在等待献祭。", blood, pray, Choice("离开", "你没有触碰祭坛，继续前进。"));
    }

    private static EventData CreateWoundedTraveler()
    {
        EventChoice help = Choice("分享药水", "旅人恢复了一些元气，并将珍藏的卡牌交给你作为谢礼。");
        help.healthChange = -10;
        help.cardsToDraw = 3;

        EventChoice loot = Choice("搜刮物品", "你找到了一些卡牌，但旅人的眼神让你心里发冷。");
        loot.cardsToDraw = 2;
        loot.maxHealthChange = -5;

        return CreateEvent("受伤的旅人", "路边躺着一个奄奄一息的旅人，他虚弱地向你伸出手。", help, loot, Choice("匆匆离开", "你加快脚步离开了。"));
    }

    private static EventData CreateMagicFountain()
    {
        EventChoice drink = Choice("饮下泉水", "泉水在体内燃烧，你恢复了生命，也获得了新的灵感。");
        drink.healthChange = 20;
        drink.cardsToDraw = 1;

        EventChoice bathe = Choice("浸泡伤口", "泉水洗去了体内残留的诅咒。你感觉轻松许多。");
        bathe.healthChange = 35;

        EventChoice bottle = Choice("装走泉水", "你把泉水灌入瓶中。它化作一张闪烁的卡牌。");
        bottle.cardsToDraw = 2;

        return CreateEvent("魔法泉水", "你来到一处被魔力笼罩的泉水边。石碑上刻着：饮者必变。", drink, bathe, bottle);
    }

    private static EventData CreateGamblerChallenge()
    {
        EventChoice gamble = Choice("接受挑战", "骰子停在了幸运的一面，你赢得了赌徒藏起的卡牌。");
        gamble.isGamble = true;
        gamble.gambleSuccessRate = 0.55f;
        gamble.cardsToDraw = 4;
        gamble.gambleFailText = "骰子碎裂，阴影反噬了你。";
        gamble.gambleFailHealthChange = -25;

        EventChoice small = Choice("小赌一把", "你赢得了一点小奖励。");
        small.isGamble = true;
        small.gambleSuccessRate = 0.75f;
        small.cardsToDraw = 2;
        small.gambleFailText = "你输掉了一些生命力。";
        small.gambleFailHealthChange = -10;

        return CreateEvent("赌徒的挑战", "戴着面具的赌徒拦住你，把一枚黑白骰子放在桌上。", gamble, small, Choice("拒绝", "你绕开了赌桌。"));
    }

    private static EventData CreateAbandonedChest()
    {
        EventChoice open = Choice("直接打开", "宝箱里有几张保存完好的卡牌。");
        open.cardsToDraw = 3;

        EventChoice inspect = Choice("小心检查", "你拆除了机关，只拿走最安全的一部分战利品。");
        inspect.cardsToDraw = 1;
        inspect.shieldGain = 20;

        EventChoice force = Choice("强行撬开", "机关刺穿了你的手臂，但更深处的奖励也暴露出来。");
        force.healthChange = -15;
        force.cardsToDraw = 4;
        force.grantCollectible = true;

        return CreateEvent("被遗弃的宝箱", "一只落满灰尘的宝箱静静躺在路边，锁孔里透出幽蓝色光芒。", open, inspect, force);
    }

    private static EventData CreateForgottenLibrary()
    {
        EventChoice taboo = Choice("研读禁忌魔法", "知识以疼痛的代价刻入记忆，你获得了更多卡牌技巧。");
        taboo.healthChange = -20;
        taboo.cardsToDraw = 3;
        taboo.maxHealthChange = 5;
        taboo.upgradeRandomCard = true;

        EventChoice heal = Choice("翻阅治愈术", "你按照手册治疗了伤势。");
        heal.healthChange = 25;

        EventChoice clean = Choice("整理书架", "你在整理书架时重新审视牌组，丢掉了多余负担。");
        clean.cardsToRemove = 3;

        return CreateEvent("被遗忘的图书馆", "废墟深处有一座安静图书馆。一本书自行翻页，像是在邀请你阅读。", taboo, heal, clean);
    }

    private static EventData CreateDarkPact()
    {
        EventChoice life = Choice("交出生命", "暗影的力量涌入身体，你用生命上限换来了强大的卡牌。");
        life.maxHealthChange = -15;
        life.cardsToDraw = 4;
        life.grantCollectible = true;

        EventChoice cards = Choice("献祭卡牌", "裂隙吞噬了你的卡牌，返还为更强韧的生命。");
        cards.cardsToRemove = 3;
        cards.maxHealthChange = 15;
        cards.healthChange = 15;

        return CreateEvent("黑暗契约", "紫色裂隙中传来低语：凡人，我可以给你力量，但一切都有代价。", life, cards, Choice("拒绝", "裂隙缓缓闭合。"));
    }

    private static EventData CreateWanderingMerchant()
    {
        EventChoice buy = Choice("查看好货", "商人掏出几张闪闪发光的卡牌。");
        buy.healthChange = -15;
        buy.cardsToDraw = 3;

        EventChoice trim = Choice("清理背包", "商人帮你丢掉冗余卡牌，还顺手包扎了伤口。");
        trim.cardsToRemove = 2;
        trim.healthChange = 15;

        EventChoice talk = Choice("聊聊天", "你获得了一些旅途经验，精神振奋。");
        talk.healthChange = 5;
        talk.shieldGain = 10;
        talk.grantCollectible = true;

        return CreateEvent("流浪商人", "背着巨大背包的商人坐在路边休息，笑眯眯地招呼你。", buy, trim, talk);
    }

    private static EventData CreateElementShrine()
    {
        EventChoice fire = Choice("触碰火印", "火印灼烧掌心，回应你的是躁动的火系牌。");
        fire.healthChange = -8;
        fire.cardsToDraw = 2;
        fire.useRewardElement = true;
        fire.rewardElement = CardElement.Fire;

        EventChoice water = Choice("触碰水印", "寒意漫过指尖，你获得了水系卡牌。");
        water.cardsToDraw = 2;
        water.useRewardElement = true;
        water.rewardElement = CardElement.Water;

        EventChoice light = Choice("触碰光印", "温和的光回应了你，治愈了伤口。");
        light.healthChange = 20;
        light.cardsToDraw = 1;
        light.useRewardElement = true;
        light.rewardElement = CardElement.Light;

        return CreateEvent("元素神龛", "五枚元素印记环绕着破碎神龛。每一枚都通向不同的力量。", fire, water, light);
    }

    private static EventData CreateMirrorMaze()
    {
        EventChoice copy = Choice("追逐倒影", "倒影消失前留下一把钥匙，打开了隐藏牌匣。");
        copy.cardsToDraw = 2;
        copy.shieldGain = 15;

        EventChoice breakMirror = Choice("打碎镜面", "镜片划伤了你，但迷宫随之坍塌，露出核心奖励。");
        breakMirror.healthChange = -12;
        breakMirror.cardsToDraw = 3;

        EventChoice meditate = Choice("闭眼冥想", "你不再被倒影诱惑，牌组也因此变得更纯粹。");
        meditate.cardsToRemove = 2;

        return CreateEvent("镜中迷宫", "雾气凝成无数镜面，每个镜中都有一个出牌顺序完全不同的你。", copy, breakMirror, meditate);
    }

    private static EventData CreateSilentGarden()
    {
        EventChoice rest = Choice("在树下休息", "无声花粉落在伤口上，你恢复了大量生命。");
        rest.healthChange = 40;

        EventChoice seed = Choice("采集种子", "种子扎根进牌盒，带来生机系力量。");
        seed.cardsToDraw = 2;
        seed.useRewardElement = true;
        seed.rewardElement = CardElement.Nature;
        seed.grantCollectible = true;

        EventChoice prune = Choice("修剪枯枝", "你剪去枯枝，也剪去牌组里的累赘。");
        prune.cardsToRemove = 1;
        prune.maxHealthChange = 5;

        return CreateEvent("无声花园", "没有风声、虫鸣或脚步声，只有一棵巨树在无声地呼吸。", rest, seed, prune);
    }

    private static EventData CreateStormBridge()
    {
        EventChoice rush = Choice("冒险冲过", "你穿过雷雨，虽然受伤，但捡到散落的卡牌。");
        rush.healthChange = -18;
        rush.cardsToDraw = 3;

        EventChoice wait = Choice("等待风停", "你耐心等待，保存了体力并获得短暂防护。");
        wait.shieldGain = 25;
        wait.healthChange = 10;

        EventChoice call = Choice("呼唤阴影渡鸦", "渡鸦载你越过断桥，也带来暗影的馈赠。");
        call.healthChange = -8;
        call.cardsToDraw = 2;
        call.useRewardElement = true;
        call.rewardElement = CardElement.Shadow;

        return CreateEvent("暴风断桥", "断桥悬在深渊上方，雷雨把桥索打得像琴弦一样颤动。", rush, wait, call);
    }

    private static EventData CreateMoonlitCaravan()
    {
        EventChoice trade = Choice("交换补给", "月光商队收下了补给，交给你一件来历不明却很实用的藏品。");
        trade.healthChange = -8; trade.grantCollectible = true;
        EventChoice guard = Choice("守夜护卫", "你整夜未眠，但商队以坚固的护符作为酬谢。");
        guard.shieldGain = 20; guard.cardsToDraw = 1;
        return CreateEvent("月下商队", "一支没有车夫的商队在月光下缓慢前行，车厢里传来金币碰撞声。", trade, guard, Choice("悄然离开", "你绕过商队，继续赶路。"));
    }

    private static EventData CreateWhisperingWell()
    {
        EventChoice listen = Choice("倾听井底", "低语指出了你牌组中最有潜力的一张牌。");
        listen.upgradeRandomCard = true;
        EventChoice throwCoin = Choice("投入金币", "井水泛起涟漪，映出一件被遗忘的护符。");
        throwCoin.healthChange = -6; throwCoin.grantCollectible = true;
        EventChoice drink = Choice("饮一口井水", "冰凉的泉水让你恢复清醒。"); drink.healthChange = 22;
        return CreateEvent("低语古井", "荒野中央有一口古井，井底不断传来像是你自己声音的低语。", listen, throwCoin, drink);
    }

    private static EventData CreateAshenForge()
    {
        EventChoice temper = Choice("淬炼一张卡牌", "灰烬熔炉吞下杂质，留下更加锋利的纹路。"); temper.upgradeRandomCard = true; temper.healthChange = -7;
        EventChoice salvage = Choice("拆解废铁", "你从废铁中找到了可用的护甲碎片。"); salvage.shieldGain = 18; salvage.cardsToDraw = 1;
        EventChoice take = Choice("取走炉心", "炉心在你掌中安静下来，化作永久的战利品。"); take.maxHealthChange = -5; take.grantCollectible = true;
        return CreateEvent("灰烬熔炉", "熄灭的熔炉仍散发余温，铁砧上刻着无人能读懂的锻造符文。", temper, salvage, take);
    }

    private static EventData CreateFallenObservatory()
    {
        EventChoice chart = Choice("校准星图", "星图展开一条清晰的道路，你从中找到新的法术。 "); chart.cardsToDraw = 2; chart.useRewardElement = true; chart.rewardElement = CardElement.Light;
        EventChoice gaze = Choice("凝视虚空", "群星回应了你，也带走了一点生命。 "); gaze.healthChange = -12; gaze.cardsToDraw = 3;
        EventChoice rest = Choice("在圆顶休息", "圆顶隔绝了风雨，你重新整理了呼吸。 "); rest.healthChange = 18; rest.shieldGain = 10;
        return CreateEvent("坠落观星台", "半座观星台嵌在山崖中，裂开的穹顶外是白昼也能看见的星河。", chart, gaze, rest);
    }

    private static EventData CreateBoundSpirit()
    {
        EventChoice free = Choice("解开锁链", "灵魂重获自由，留下守护你的遗物。 "); free.healthChange = -10; free.grantCollectible = true;
        EventChoice bargain = Choice("交换记忆", "你忘记了一段无关紧要的往事，换来几张精挑细选的卡牌。 "); bargain.cardsToDraw = 2; bargain.cardsToRemove = 1;
        EventChoice pass = Choice("不去触碰", "锁链在风中轻响，你没有回头。 "); pass.shieldGain = 12;
        return CreateEvent("缚灵锁链", "一团幽光被锁在断柱上，它安静地等待某个人作出决定。", free, bargain, pass);
    }

    private static EventData CreateSunkenSanctuary()
    {
        EventChoice dive = Choice("潜入水下", "你在神殿底部找到了一只密封的宝匣。 "); dive.healthChange = -9; dive.cardsToDraw = 2; dive.grantCollectible = true;
        EventChoice pray = Choice("向潮汐祈祷", "水流抚平伤口，带来温和的庇护。 "); pray.healthChange = 25; pray.shieldGain = 10;
        EventChoice gather = Choice("收集珊瑚", "珊瑚的纹路引导你找到水系的力量。 "); gather.cardsToDraw = 2; gather.useRewardElement = true; gather.rewardElement = CardElement.Water;
        return CreateEvent("沉没圣所", "潮水退去后，古老圣所的门扉从礁石间显露出来。", dive, pray, gather);
    }
}
