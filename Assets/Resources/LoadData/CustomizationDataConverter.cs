using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomizationDataConverter
{
    public static CustomizationDataDTO ToDTO(CustomizationData data)
    {
        return new CustomizationDataDTO
        {
            body = BuildPath(data.gender.ToString(), data.body.ToString()),
            head = BuildPath(data.gender.ToString(), data.head.ToString()),
            top = BuildPath(data.gender.ToString(), data.top.ToString()),
            bottom = BuildPath(data.gender.ToString(), data.bottom.ToString()),
            shoes = BuildPath(data.gender.ToString(), data.shoes.ToString()),
            outfit = BuildPath(data.gender.ToString(), data.outfit.ToString()),
            hairstyle = BuildPath(data.gender.ToString(), data.hairstyle.ToString()),
            acc_head = BuildPath(data.gender.ToString(), data.acc_head.ToString()),
            gender = data.gender.ToString(),
        };
    }

    public static CustomizationData FromDTO(CustomizationDataDTO dto)
    {
        return new CustomizationData
        {
            body = dto.body,
            head = dto.head,
            top = dto.top,
            bottom = dto.bottom,
            shoes = dto.shoes,
            outfit = dto.outfit,
            hairstyle = dto.hairstyle,
            acc_head = dto.acc_head,
            gender = (dto.gender ?? "").ToLower() == "m" ? "m" : "f",
        };
    }

    private static string BuildPath(string gender, string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        // 먼저 gender 폴더에 존재하면 그걸로, 없으면 Shared fallback
        var fullPath = $"Customization/{gender}/{name}";
        if (Resources.Load(fullPath) != null)
            return fullPath;

        fullPath = $"Customization/Shared/{name}";
        if (Resources.Load(fullPath) != null)
            return fullPath;

        return ""; // 못 찾은 경우
    }
}