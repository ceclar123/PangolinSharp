using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Pangolin.Utility;
using ReactiveUI;

namespace Pangolin.Desktop.ViewModels.Json;

public class JsonConvertCsharpUserControlViewModel : ViewModelBase
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;

    public ReactiveCommand<Unit, Unit> CmdConvert { get; protected set; }

    public JsonConvertCsharpUserControlViewModel()
    {
        CmdConvert = ReactiveCommand.CreateFromTask(CommandConvert);
    }

    private async Task CommandConvert()
    {
        if (string.IsNullOrWhiteSpace(From))
        {
            await MessageBoxManager.GetMessageBoxStandard("提示", "参数为空", ButtonEnum.Ok, Icon.Info, WindowStartupLocation.CenterOwner)
                .ShowWindowDialogAsync(this.ParentWindow);
        }
        else
        {
            try
            {
                await Task.Run(() =>
                {
                    To = Parse(From.Trim());
                    // 通知改变
                    this.RaisePropertyChanged(nameof(To));
                });
            }
            catch (Exception e)
            {
                await MessageBoxManager.GetMessageBoxStandard("错误", e.Message, ButtonEnum.Ok, Icon.Error, WindowStartupLocation.CenterOwner)
                    .ShowWindowDialogAsync(this.ParentWindow);
            }
        }
    }

    private string Parse(string json)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("public class Demo").Append("\r\n");
        builder.Append("{").Append("\r\n");
        JsonNode? node = JsonNode.Parse(json);
        if (node is null || !(node is JsonObject jsonObject))
        {
            return builder.ToString();
        }

        foreach (KeyValuePair<string, JsonNode?> item in jsonObject)
        {
            if (item.Value is null)
            {
                continue;
            }
            else if (item.Value is JsonObject)
            {
                builder.AppendFormat($"\tpublic Dictionary<string,object> {item.Key}").Append(" { get; set; }").Append("\r\n");
            }
            else if (item.Value is JsonArray jsonArray)
            {
                string typeName = GetJavaTypeName(jsonArray);
                builder.AppendFormat($"\tpublic List<{typeName}> {item.Key}").Append(" { get; set; }").Append("\r\n");
            }
            else if (item.Value is JsonValue value)
            {
                string typeName = GetJavaTypeName(value);
                builder.AppendFormat($"\tpublic {typeName} {item.Key}").Append(" { get; set; }").Append("\r\n");
            }
        }

        builder.Append("}");
        return builder.ToString();
    }

    private string GetJavaTypeName(JsonArray jsonArray)
    {
        HashSet<String> set = jsonArray.Where(ObjectUtil.IsNotNull)
            .Select(it =>
            {
                if (it is JsonObject || it is JsonArray)
                {
                    return "object";
                }
                else if (it is JsonValue value)
                {
                    return GetJavaTypeName(value);
                }
                else
                {
                    return "object";
                }
            }).ToHashSet();

        if (set.Count() == 1)
        {
            return set.First();
        }
        else
        {
            return "object";
        }
    }

    private string GetJavaTypeName(JsonValue value)
    {
        if (value.GetValueKind() == JsonValueKind.Null || value.GetValueKind() == JsonValueKind.Object || value.GetValueKind() == JsonValueKind.Undefined)
        {
            return "object";
        }
        else if (value.GetValueKind() == JsonValueKind.String)
        {
            return "string";
        }
        else if (value.GetValueKind() == JsonValueKind.True || value.GetValueKind() == JsonValueKind.False)
        {
            return "bool?";
        }
        else if (value.GetValueKind() == JsonValueKind.Number)
        {
            if (value.ToString().Contains("."))
            {
                return "decimal?";
            }
            else
            {
                return "int?";
            }
        }
        else if (value.GetValueKind() == JsonValueKind.Array)
        {
            if (value.TryGetValue(out List<object>? list))
            {
                if (CollectionUtil.IsEmpty(list))
                {
                    return "List<object>";
                }
                else
                {
                    HashSet<Type> typeSet = list.Where(ObjectUtil.IsNotNull).Select(it => it.GetType()).ToHashSet();
                    if (typeSet.Contains(typeof(string)))
                    {
                        return "List<string>";
                    }
                    else
                    {
                        return "List<int?>";
                    }
                }
            }
        }

        return "object";
    }
}