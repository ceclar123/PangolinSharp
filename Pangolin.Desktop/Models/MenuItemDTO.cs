using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangolin.Desktop.Models
{
    [Serializable]
    public class MenuItemDTO
    {
        public string? Name { get; set; }

        public string? Header { get; set; }

        /// <summary>
        /// Separator、MenuItem
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// 内容类名,例如：Pangolin.Desktop.Views.Encode.Base64UserControl
        /// </summary>
        public string? UserControl { get; set; }

        /// <summary>
        /// 内容类名,例如：Pangolin.Desktop.ViewModels.Encode.Base64UserControlViewModel
        /// </summary>
        public string? ViewModel { get; set; }

        /// <summary>
        /// 子节点
        /// </summary>
        public List<MenuItemDTO>? Items { get; set; }
    }
}
