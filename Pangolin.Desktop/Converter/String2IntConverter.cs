using System;
using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Pangolin.Desktop.Converter;

public class String2IntConverter : IValueConverter
{
    enum String2IntEnum
    {
        LessThanZero,
        LessThanOrEqualZero,
        EqualZero,
        GreaterThanZero,
        GreaterThanEqualZero
    }

    /// <summary>
    /// int -> string
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(value is int val))
        {
            return BindingOperations.DoNothing;
        }

        if (!(parameter is string param))
        {
            return BindingOperations.DoNothing;
        }

        if (!(int.TryParse(param, out var type)))
        {
            return BindingOperations.DoNothing;
        }

        switch (type)
        {
            case (int)String2IntEnum.LessThanZero:
                if (val < 0)
                {
                    return val.ToString();
                }

                break;
            case (int)String2IntEnum.LessThanOrEqualZero:
                if (val <= 0)
                {
                    return val.ToString();
                }

                break;
            case (int)String2IntEnum.EqualZero:
                if (val == 0)
                {
                    return val.ToString();
                }

                break;
            case (int)String2IntEnum.GreaterThanZero:
                if (val > 0)
                {
                    return val.ToString();
                }

                break;
            case (int)String2IntEnum.GreaterThanEqualZero:
                if (val >= 0)
                {
                    return val.ToString();
                }

                break;
            default:
                break;
        }

        return BindingOperations.DoNothing;
    }

    /// <summary>
    /// string -> int
    /// </summary>
    /// <param name="value"></param>
    /// <param name="targetType"></param>
    /// <param name="parameter"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (!(value is string input))
        {
            return BindingOperations.DoNothing;
        }

        if (!(int.TryParse(input, out var val)))
        {
            return BindingOperations.DoNothing;
        }

        if (!(parameter is string param))
        {
            return BindingOperations.DoNothing;
        }

        if (!(int.TryParse(param, out var type)))
        {
            return BindingOperations.DoNothing;
        }

        switch (type)
        {
            case (int)String2IntEnum.LessThanZero:
                if (val < 0)
                {
                    return val;
                }

                break;
            case (int)String2IntEnum.LessThanOrEqualZero:
                if (val <= 0)
                {
                    return val;
                }

                break;
            case (int)String2IntEnum.EqualZero:
                if (val == 0)
                {
                    return val;
                }

                break;
            case (int)String2IntEnum.GreaterThanZero:
                if (val > 0)
                {
                    return val;
                }

                break;
            case (int)String2IntEnum.GreaterThanEqualZero:
                if (val >= 0)
                {
                    return val;
                }

                break;
            default:
                break;
        }

        return BindingOperations.DoNothing;
    }
}