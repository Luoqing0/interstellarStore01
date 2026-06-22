using System.Collections.Generic;
using UnityEngine;
using Verse;

/// <summary>
/// 星际商店 - 看板娘 Def
/// AI 辅助生成
/// 必须放在全局命名空间，否则 RimWorld XML 解析器找不到
/// </summary>
public class StarStore_MascotDef : Def
{
    /// <summary>看板娘贴图路径（相对于 Textures/）</summary>
    public string texturePath = "看板娘/starstore_mascot";

    /// <summary>该看板娘独立的问候语池</summary>
    public List<string> greetings = new List<string>();

    private Texture2D _tex;

    /// <summary>获取看板娘贴图（带缓存）</summary>
    public Texture2D 获取贴图()
    {
        if (_tex == null || _tex == BaseContent.BadTex)
            _tex = ContentFinder<Texture2D>.Get(texturePath, false);
        return _tex;
    }
}
