using System.Text.Json.Serialization;
using Pangolin.Desktop.Models;

namespace Pangolin.Desktop.Json;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(MenuItemDTO))]
public partial class MenuItemDtoGenerationContext : JsonSerializerContext
{
}