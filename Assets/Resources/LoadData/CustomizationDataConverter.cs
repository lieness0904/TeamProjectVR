using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomizationDataConverter
{
    public static CustomizationDataDTO ToDTO(CustomizationData data)
    {
        return new CustomizationDataDTO
        {
            body = StripPath(data.body.ToString()),
            head = StripPath(data.head.ToString()),
            top = StripPath(data.top.ToString()),
            bottom = StripPath(data.bottom.ToString()),
            shoes = StripPath(data.shoes.ToString()),
            outfit = StripPath(data.outfit.ToString()),
            hairstyle = StripPath(data.hairstyle.ToString()),
            acc_head = StripPath(data.acc_head.ToString()),
            gender = data.gender.ToString(),
        };
    }

    public static CustomizationData FromDTO(CustomizationDataDTO dto)
    {
        var gender = (dto.gender ?? "").ToLower() == "m" ? "m" : "f";
        return new CustomizationData
        {
            body = BuildPath(gender, dto.body),       // 여기!
            head = BuildPath(gender, dto.head),
            top = BuildPath(gender, dto.top),
            bottom = BuildPath(gender, dto.bottom),
            shoes = BuildPath(gender, dto.shoes),
            outfit = BuildPath(gender, dto.outfit),
            hairstyle = BuildPath(gender, dto.hairstyle),
            acc_head = BuildPath(gender, dto.acc_head),
            gender = gender,
        };
    }
    public static CustomizationData FromJson(string json)
    {
        var dto = JsonUtility.FromJson<CustomizationDataDTO>(json);
        return FromDTO(dto);
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
    private static string StripPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return "";
        var parts = fullPath.Split('/');
        return parts[^1]; // 마지막 파츠 이름만 반환
    }
}