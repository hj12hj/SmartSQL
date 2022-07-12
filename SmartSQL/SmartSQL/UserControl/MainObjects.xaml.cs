using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HandyControl.Controls;
using HandyControl.Data;
using SmartSQL.Framework;
using SmartSQL.Framework.Exporter;
using SmartSQL.Framework.PhysicalDataModel;
using SmartSQL.Framework.SqliteModel;
using SmartSQL.Annotations;
using SmartSQL.Models;
using SmartSQL.Views;
using TextBox = System.Windows.Controls.TextBox;
using UserControlE = System.Windows.Controls.UserControl;
using MessageBox = HandyControl.Controls.MessageBox;
using DbType = SqlSugar.DbType;

namespace SmartSQL.UserControl
{
    /// <summary>
    /// MainObjects.xaml 的交互逻辑
    /// </summary>
    public partial class MainObjects : BaseUserControl
    {
        #region Filds
        public static readonly DependencyProperty SelectedObjectProperty = DependencyProperty.Register(
            "SelectedObject", typeof(PropertyNodeItem), typeof(MainObjects), new PropertyMetadata(default(PropertyNodeItem)));

        public static readonly DependencyProperty SelectedDataBaseProperty = DependencyProperty.Register(
            "SelectedDataBase", typeof(DataBase), typeof(MainObjects), new PropertyMetadata(default(DataBase)));

        public static readonly DependencyProperty SelectedConnectionProperty = DependencyProperty.Register(
            "SelectedConnection", typeof(ConnectConfigs), typeof(MainObjects), new PropertyMetadata(default(ConnectConfigs)));

        public static readonly DependencyProperty ObjectsViewDataProperty = DependencyProperty.Register(
            "ObjectsViewData", typeof(List<PropertyNodeItem>), typeof(MainObjects), new PropertyMetadata(default(List<PropertyNodeItem>)));

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            "Placeholder", typeof(string), typeof(MainObjects), new PropertyMetadata(default(string)));

        /// <summary>
        /// 当前选中对象
        /// </summary>
        public PropertyNodeItem SelectedObject
        {
            get => (PropertyNodeItem)GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }
        /// <summary>
        /// 当前选中数据库
        /// </summary>
        public DataBase SelectedDataBase
        {
            get => (DataBase)GetValue(SelectedDataBaseProperty);
            set => SetValue(SelectedDataBaseProperty, value);
        }
        /// <summary>
        /// 当前数据连接
        /// </summary>
        public ConnectConfigs SelectedConnection
        {
            get => (ConnectConfigs)GetValue(SelectedConnectionProperty);
            set => SetValue(SelectedConnectionProperty, value);
        }
        public List<PropertyNodeItem> ObjectsViewData
        {
            get => (List<PropertyNodeItem>)GetValue(ObjectsViewDataProperty);
            set
            {
                SetValue(ObjectsViewDataProperty, value);
                OnPropertyChanged(nameof(ObjectsViewData));
            }
        }
        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set
            {
                SetValue(PlaceholderProperty, value);
                OnPropertyChanged(nameof(Placeholder));
            }
        }
        #endregion
        public MainObjects()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// 加载页面数据
        /// </summary>
        public void LoadPageData()
        {
            ChkAll.IsChecked = false;
            NoDataText.Visibility = Visibility.Collapsed;
            if (SelectedObject.Type == ObjType.Type)
            {
                var headerT = "表名称";
                var placeHolderT = "请输入表名称或备注说明";
                switch (SelectedObject.Name)
                {
                    case "treeTable": break;
                    case "treeView":
                        headerT = "视图名称";
                        placeHolderT = "请输入视图名称或备注说明"; break;
                    default:
                        headerT = "存储过程名称";
                        placeHolderT = "请输入存储过程名称或备注说明"; break;
                }
                ObjHead.Header = headerT;
                Placeholder = placeHolderT;
                if (SelectedObject.Parent == null)
                {
                    ObjectsViewData = ObjectsViewData.First(x => x.Name == SelectedObject.Name).Children;
                }
                else
                {
                    ObjectsViewData = ObjectsViewData.First(x => x.DisplayName == SelectedObject.Parent.DisplayName)
                        .Children;
                    ObjectsViewData = ObjectsViewData.First(x => x.Name == SelectedObject.Name).Children;
                }
                if (!ObjectsViewData.Any())
                {
                    NoDataText.Visibility = Visibility.Visible;
                }
                ObjectsViewData.ForEach(x =>
                {
                    x.IsSelected = false;
                });
                ObjItems = ObjectsViewData;
                SearchObject.Text = string.Empty;
            }
        }

        private List<PropertyNodeItem> ObjItems;
        /// <summary>
        /// 实时搜索对象
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchObject_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            NoDataText.Visibility = Visibility.Collapsed;
            var searchText = SearchObject.Text.Trim();
            var searchData = ObjItems;
            if (!string.IsNullOrEmpty(searchText))
            {
                var obj = ObjItems.Where(x => x.DisplayName.ToLower().Contains(searchText.ToLower()) || (!string.IsNullOrEmpty(x.Comment) && x.Comment.ToLower().Contains(searchText.ToLower())));
                if (!obj.Any())
                {
                    NoDataText.Visibility = Visibility.Visible;
                    searchData = new List<PropertyNodeItem>();
                }
                else
                {
                    searchData = obj.ToList();
                }
            }
            ObjectsViewData = searchData;
        }

        private void DisplayToolTip_MouseEnter(object sender, MouseEventArgs e)
        {
            var currCell = (DataGridCell)sender;
            if (currCell.Column.SortMemberPath.Equals("DisplayName"))
            {
                var currObject = (PropertyNodeItem)currCell.DataContext;
                currCell.ToolTip = currObject.DisplayName;
            }
        }

        private string _cellEditValue = string.Empty;
        private void TableGrid_OnBeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (((DataGrid)sender).SelectedCells.Any())
            {
                //获取选中单元格(仅限单选)
                var selectedCell = ((DataGrid)sender).SelectedCells[0];
                //获取选中单元格数据
                var selectedData = selectedCell.Column.GetCellContent(selectedCell.Item);
                if (selectedData is TextBlock selectedText)
                {
                    _cellEditValue = selectedText.Text;
                }
            }
        }

        private void TableGrid_OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            #region MyRegion
            if (e.EditingElement != null && e.EditingElement is TextBox)
            {
                string newValue = (e.EditingElement as TextBox).Text;
                if (!_cellEditValue.Equals(newValue))
                {
                    if (SelectedObject.Type == ObjType.View)
                    {
                        ((TextBox)e.EditingElement).Text = _cellEditValue;
                        return;
                    }
                    var selectItem = (PropertyNodeItem)e.Row.Item;
                    var msgResult = MessageBox.Show($"确认修改{selectItem.DisplayName}的备注为{newValue}？", "温馨提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (msgResult == MessageBoxResult.OK)
                    {
                        var type = "";
                        var flag = false;
                        var dbConnectionString = SelectedConnection.SelectedDbConnectString(SelectedDataBase.DbName);
                        var db = SugarFactory.GetDbMaintenance(SelectedConnection.DbType, dbConnectionString);
                        switch (selectItem.Type)
                        {
                            case ObjType.Table:
                                if (db.IsAnyTableRemark(selectItem.Name))
                                {
                                    db.DeleteTableRemark(selectItem.Name);
                                }
                                flag = db.AddTableRemark(selectItem.Name, newValue);
                                break;
                            case ObjType.View:
                                if (db.IsAnyViewRemark(selectItem.Name))
                                {
                                    db.DeleteViewRemark(selectItem.Name);
                                }
                                flag = db.AddViewRemark(selectItem.Name, newValue);
                                break;
                            case ObjType.Proc:
                                if (db.IsAnyProcRemark(selectItem.Name))
                                {
                                    db.DeleteProcRemark(selectItem.Name);
                                }
                                flag = db.AddProcRemark(selectItem.Name, newValue);
                                break;
                        }
                        if (flag)
                            Growl.SuccessGlobal(new GrowlInfo { Message = $"修改成功", ShowDateTime = false });
                        else
                            Growl.WarningGlobal(new GrowlInfo { Message = $"修改失败", ShowDateTime = false });
                    }
                    else
                    {
                        ((TextBox)e.EditingElement).Text = _cellEditValue;
                    }
                }
            }
            #endregion
        }

        /// <summary>
        /// 导出文档
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExport_OnClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Window.GetWindow(this);
            ExportDoc exportDoc = new ExportDoc();
            exportDoc.Owner = mainWindow;
            exportDoc.ExportType = ExportEnum.Partial;
            exportDoc.SelectedConnection = SelectedConnection;
            exportDoc.SelectedDataBase = SelectedDataBase;
            var exportData = ObjectsViewData.Any(x => x.IsSelected == true)
                ? ObjectsViewData.Where(x => x.IsSelected == true).ToList()
                : ObjectsViewData;
            exportDoc.ExportData = exportData;
            exportDoc.ShowDialog();
        }

        private void ChkAll_OnClick(object sender, RoutedEventArgs e)
        {
            var isChecked = ((CheckBox)sender).IsChecked;
            var selectedItem = ObjectsViewData;
            selectedItem.ForEach(x =>
            {
                x.IsSelected = isChecked;
            });
            ObjectsViewData = selectedItem;
        }

        private void TableGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var curItem = (PropertyNodeItem)dataGrid.CurrentItem;
                if (curItem == null)
                {
                    return;
                }
                var curCell = dataGrid.CurrentCell.Column.Header;
                if (curCell.Equals("备注说明"))
                {
                    return;
                }
                curItem.IsSelected = !curItem.IsSelected;
            }
        }

        private void TableGrid_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dataGrid)
            {
                var curItem = (PropertyNodeItem)dataGrid.CurrentItem;
                if (curItem == null)
                {
                    return;
                }
                var curCell = dataGrid.CurrentCell.Column.Header;
                if (curCell.Equals("选择"))
                {
                    curItem.IsSelected = !curItem.IsSelected;
                }
            }
        }

        private void BtnSetGroup_OnClick(object sender, RoutedEventArgs e)
        {
            var sqLiteHelper = new SQLiteHelper();
            var list = sqLiteHelper.db.Table<ObjectGroup>().Where(x =>
                x.ConnectId == SelectedConnection.ID && x.DataBaseName == SelectedDataBase.DbName).ToList();
            if (!list.Any())
            {
                Growl.WarningGlobal(new GrowlInfo { Message = $"暂无分组，请先添加分组", WaitTime = 1, ShowDateTime = false });
                return;
            }
            var mainWindow = System.Windows.Window.GetWindow(this);
            var group = new SetObjectGroup();
            //group.ObjChangeRefreshEvent += ObjChangeRefreshEvent;
            group.Connection = SelectedConnection;
            group.SelectedDataBase = SelectedDataBase.DbName;
            var groupData = ObjectsViewData.Any(x => x.IsSelected == true)
                ? ObjectsViewData.Where(x => x.IsSelected == true).ToList()
                : ObjectsViewData;
            group.SelectedObjects = groupData;
            group.Owner = mainWindow;
            group.ShowDialog();
        }
    }
}
