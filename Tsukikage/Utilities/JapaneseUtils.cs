using System.Collections.Frozen;

namespace Tsukikage.Utilities;

internal static class JapaneseUtils
{
    public static readonly FrozenDictionary<char, char> FrequentlyMisparsedCharactersDict = new KeyValuePair<char, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        new('が', 'か'), new('ぎ', 'き'), new('ぐ', 'く'), new('げ', 'け'), new('ご', 'こ'),
        new('ざ', 'さ'), new('じ', 'し'), new('ず', 'す'), new('ぜ', 'せ'), new('ぞ', 'そ'),
        new('だ', 'た'), new('ぢ', 'ち'), new('づ', 'つ'), new('で', 'て'), new('ど', 'と'),
        new('ば', 'は'), new('び', 'ひ'), new('ぶ', 'ふ'), new('べ', 'へ'), new('ぼ', 'ほ'),
        new('ぱ', 'は'), new('ぴ', 'ひ'), new('ぷ', 'ふ'), new('ぺ', 'へ'), new('ぽ', 'ほ'),
        new('ガ', 'カ'), new('ギ', 'キ'), new('グ', 'ク'), new('ゲ', 'ケ'), new('ゴ', 'コ'),
        new('ザ', 'サ'), new('ジ', 'シ'), new('ズ', 'ス'), new('ゼ', 'セ'), new('ゾ', 'ソ'),
        new('ダ', 'タ'), new('ヂ', 'チ'), new('ヅ', 'ツ'), new('デ', 'テ'), new('ド', 'ト'),
        new('バ', 'ハ'), new('ビ', 'ヒ'), new('ブ', 'フ'), new('ベ', 'ヘ'), new('ボ', 'ホ'),
        new('パ', 'ハ'), new('ピ', 'ヒ'), new('プ', 'フ'), new('ペ', 'ヘ'), new('ポ', 'ホ'),
        new('ゔ', 'う'), new('ヴ', 'ウ'), new('ゞ', 'ゝ'), new('ヾ', 'ヽ'),
        new('ヸ', 'ヰ'), new('ヹ', 'ヱ'), new('ヺ', 'ヲ')
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();

    public static readonly FrozenDictionary<char, char> NormalizationDict = new KeyValuePair<char, char>[]
    {
        #pragma warning disable format
        // ReSharper disable BadExpressionBracesLineBreaks
        new('ぁ', 'あ'), new('ぃ', 'い'), new('ぅ', 'う'), new('ぇ', 'え'), new('ぉ', 'お'),
        new('ゃ', 'や'), new('ゅ', 'ゆ'), new('ょ', 'よ'), new('っ', 'つ'), new('ゎ', 'わ'),
        new('ゕ', 'か'), new('ゖ', 'け'),

        new('ァ', 'ア'), new('ィ', 'イ'), new('ゥ', 'ウ'), new('ェ', 'エ'), new('ォ', 'オ'),
        new('ャ', 'ヤ'), new('ュ', 'ユ'), new('ョ', 'ヨ'), new('ッ', 'ツ'), new('ヮ', 'ワ'),
        new('ヵ', 'カ'), new('ヶ', 'ケ'),

        new('！','!'), new('＂','"'), new('＃','#'), new('＄','$'), new('％','%'),
        new('＆','&'), new('＇','\''), new('（','('), new('）',')'), new('＊','*'),
        new('＋','+'), new('，',','), new('－','-'), new('．','.'), new('／','/'),

        new('０','0'), new('１','1'), new('２','2'), new('３','3'), new('４','4'),
        new('５','5'), new('６','6'), new('７','7'), new('８','8'), new('９','9'),

        new('：',':'), new('；',';'), new('＜','<'), new('＝','='), new('＞','>'), new('？','?'),
        new('＠','@'),

        new('Ａ','A'), new('Ｂ','B'), new('Ｃ','C'), new('Ｄ','D'), new('Ｅ','E'),
        new('Ｆ','F'), new('Ｇ','G'), new('Ｈ','H'), new('Ｉ','I'), new('Ｊ','J'),
        new('Ｋ','K'), new('Ｌ','L'), new('Ｍ','M'), new('Ｎ','N'), new('Ｏ','O'),
        new('Ｐ','P'), new('Ｑ','Q'), new('Ｒ','R'), new('Ｓ','S'), new('Ｔ','T'),
        new('Ｕ','U'), new('Ｖ','V'), new('Ｗ','W'), new('Ｘ','X'), new('Ｙ','Y'),
        new('Ｚ','Z'),

        new('［','['), new('＼','\\'), new('］',']'), new('＾','^'), new('＿','_'),
        new('｀','`'),

        new('ａ','a'), new('ｂ','b'), new('ｃ','c'), new('ｄ','d'), new('ｅ','e'),
        new('ｆ','f'), new('ｇ','g'), new('ｈ','h'), new('ｉ','i'), new('ｊ','j'),
        new('ｋ','k'), new('ｌ','l'), new('ｍ','m'), new('ｎ','n'), new('ｏ','o'),
        new('ｐ','p'), new('ｑ','q'), new('ｒ','r'), new('ｓ','s'), new('ｔ','t'),
        new('ｕ','u'), new('ｖ','v'), new('ｗ','w'), new('ｘ','x'), new('ｙ','y'),
        new('ｚ','z'),

        new('｛','{'), new('｜','|'), new('｝','}'), new('～','~'),
        new('｡','。'), new('｢','「'), new('｣','」'), new('､','、'), new('･','・'),

        new('ｦ','ヲ'),
        new('ｧ','ァ'), new('ｨ','ィ'), new('ｩ','ゥ'), new('ｪ','ェ'), new('ｫ','ォ'),
        new('ｬ','ャ'), new('ｭ','ュ'), new('ｮ','ョ'), new('ｯ','ッ'),

        new('ｰ','ー'),
        new('ｱ','ア'), new('ｲ','イ'), new('ｳ','ウ'), new('ｴ','エ'), new('ｵ','オ'),
        new('ｶ','カ'), new('ｷ','キ'), new('ｸ','ク'), new('ｹ','ケ'), new('ｺ','コ'),
        new('ｻ','サ'), new('ｼ','シ'), new('ｽ','ス'), new('ｾ','セ'), new('ｿ','ソ'),
        new('ﾀ','タ'), new('ﾁ','チ'), new('ﾂ','ツ'), new('ﾃ','テ'), new('ﾄ','ト'),
        new('ﾅ','ナ'), new('ﾆ','ニ'), new('ﾇ','ヌ'), new('ﾈ','ネ'), new('ﾉ','ノ'),
        new('ﾊ','ハ'), new('ﾋ','ヒ'), new('ﾌ','フ'), new('ﾍ','ヘ'), new('ﾎ','ホ'),
        new('ﾏ','マ'), new('ﾐ','ミ'), new('ﾑ','ム'), new('ﾒ','メ'), new('ﾓ','モ'),
        new('ﾔ','ヤ'), new('ﾕ','ユ'), new('ﾖ','ヨ'),
        new('ﾗ','ラ'), new('ﾘ','リ'), new('ﾙ','ル'), new('ﾚ','レ'), new('ﾛ','ロ'),
        new('ﾜ','ワ'), new('ﾝ','ン'),

        new('￠','¢'), new('￡','£'), new('￢','¬'), new('￣','¯'),
        new('￤','|'), new('￥','¥'), new('￦','₩'),
        new('￨','|'),

        new('￩','←'), new('￪','↑'), new('￫','→'), new('￬','↓'),
        new('￭','■'), new('￮','○'),

        new('―', 'ー'), new('‐', 'ー'), new('-', 'ー'),
        new('–', 'ー'), new('−', 'ー'), new('─', 'ー'),
        new('〜', '～'), new('〰', '～'), new('∼', '～'),

        new('·', '・'), new('•', '・'),
        new('　', ' '),
        new('“', '"'), new('”', '"'), new('‘', '\''), new('’', '\''),
        new('⋯', '…'), new('ー', '一'),
        new('｟', '('), new('｠', ')'),
        // ReSharper restore BadExpressionBracesLineBreaks
        #pragma warning restore format
    }.ToFrozenDictionary();
}
