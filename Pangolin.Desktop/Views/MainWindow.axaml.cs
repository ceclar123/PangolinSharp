using Avalonia.Controls;
using Pangolin.Desktop.Models;
using Pangolin.Utility;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Pangolin.Desktop.Views.Encode;
using Pangolin.Desktop.ViewModels.Encode;
using System.Reflection;
using Pangolin.Desktop.ViewModels;

namespace Pangolin.Desktop.Views
{
    public partial class MainWindow : Window
    {
        private WindowNotificationManager? _manager;
        public MainWindow()
        {
            InitializeComponent();
            this.LoadMenu();
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _manager = new WindowNotificationManager(this) { MaxItems = 3 };
        }

        private void LoadMenu()
        {
            Menu? menu = this.FindControl<Menu>("menu");
            if (ObjectUtil.IsNull(menu))
            {
                return;
            }

            List<MenuItemDTO> list = JsonSerializer.Deserialize<List<MenuItemDTO>>(Pangolin.Desktop.Properties.Resources.MenuJsonData);
            if (ObjectUtil.IsNotNull(list))
            {
                menu?.Items.Clear();
                foreach (MenuItemDTO item in list)
                {
                    if (ObjectUtil.IsNull(item))
                    {
                        continue;
                    }
                    object val = GetSubMenuItem(item);
                    if (ObjectUtil.IsNotNull(val))
                    {
                        menu?.Items.Add(val);
                    }
                }
            }
        }


        private object GetSubMenuItem(MenuItemDTO current)
        {
            if (ObjectUtil.IsNull(current))
            {
                return null;
            }


            if ("MenuItem".Equals(current.Type))
            {
                MenuItem menuItem = new MenuItem()
                {
                    Name = current.Name,
                    Header = current.Header,
                    Tag = current.UserControl + "," + current.ViewModel
                };
                // 递归                
                if (ObjectUtil.IsNotNull(current.Items))
                {
                    foreach (MenuItemDTO child in current.Items)
                    {
                        if (ObjectUtil.IsNull(child))
                        {
                            continue;
                        }
                        object val = GetSubMenuItem(child);
                        if (ObjectUtil.IsNotNull(val))
                        {
                            menuItem.Items.Add(val);
                        }
                    }
                }

                menuItem.Click += MenuItem_Click;
                return menuItem;
            }
            else
            {
                return new Separator();
            }
        }

        private void MenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 标记事件已处理，防止冒泡到其他处理程序
            e.Handled = true;
            MenuItem menuItem = (sender as MenuItem) ?? null;
            if (ObjectUtil.IsNull(menuItem))
            {
                return;
            }
            TabControl tabControl = this.FindControl<TabControl>("tabControl") ?? null;
            if (ObjectUtil.IsNull(tabControl))
            {
                return;
            }

            int index = -1;
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                TabItem findOne = (tabControl.Items[i] as TabItem) ?? null;
                if (findOne != null && Object.Equals(findOne.Name, menuItem.Name))
                {
                    index = i;
                    break;
                }
            }
            if (index > -1)
            {
                tabControl.SelectedIndex = index;
            }
            else
            {
                TabItem tabItem = new TabItem { Name = menuItem.Name, Header = menuItem.Header, Content = new Panel() };
                tabItem.Margin = new Avalonia.Thickness(5, 0, 5, 0);
                UserControl userControl = this.BuildUserControl(menuItem);
                if (userControl != null)
                {
                    tabItem.Content = userControl;
                    tabControl.Items.Add(tabItem);
                    tabControl.SelectedItem = tabItem;
                }
                else
                {
                    _manager?.Show(new Notification("错误", "配置错误", NotificationType.Information));
                }
            }
        }

        private UserControl BuildUserControl(MenuItem menuItem)
        {
            string tag = menuItem.Tag?.ToString();
            string[] array = tag.Split(',');
            if (array == null || array.Length != 2 || String.IsNullOrEmpty(array[0]) || String.IsNullOrEmpty(array[1]))
            {
                return null;
            }

            Type ctlType = Assembly.GetExecutingAssembly().GetType(array[0]) ?? null;
            Type viewModelType = Assembly.GetExecutingAssembly().GetType(array[1]) ?? null;
            if (ctlType == null || viewModelType == null)
            {
                return null;
            }

            UserControl userControl = Activator.CreateInstance(ctlType) as UserControl;
            ViewModelBase viewModel = Activator.CreateInstance(viewModelType) as ViewModelBase;
            if (userControl == null || viewModel == null)
            {
                return null;
            }

            viewModel.ParentWindow = this;
            userControl.DataContext = viewModel;

            return userControl;
        }
    }
}