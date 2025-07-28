using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustomizationDataConverter
{
    public static CustomizationDataDTO ToDTO(CustomizationData data)
    {
        return new CustomizationDataDTO
        {
            body = data.body.ToString(),
            head = data.head.ToString(),
            top = data.top.ToString(),
            bottom = data.bottom.ToString(),
            shoes = data.shoes.ToString(),
            outfit = data.outfit.ToString(),
            hairstyle = data.hairstyle.ToString(),
            acc_head = data.acc_head.ToString(),
            gender = data.gender.ToString()
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
            gender = dto.gender
        };
    }
}