using System;

namespace Pangolin.Desktop.Models;

[Serializable]
public class SelectItemDto
{
    public int Code { get; set; }
    public string? Name { get; set; }

    public SelectItemDto()
    {
    }

    public SelectItemDto(int code, string? name)
    {
        Code = code;
        Name = name;
    }
}