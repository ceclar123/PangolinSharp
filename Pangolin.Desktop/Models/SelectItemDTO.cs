using System;

namespace Pangolin.Desktop.Models;

[Serializable]
public class SelectItemDTO
{
    public int Code { get; set; }
    public string Name { get; set; }

    public SelectItemDTO()
    {
    }

    public SelectItemDTO(int code, string name)
    {
        Code = code;
        Name = name;
    }
}