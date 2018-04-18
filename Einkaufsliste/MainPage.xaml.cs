using Couchbase.Lite;
using Couchbase.Lite.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x407 dokumentiert.

namespace Einkaufsliste
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Param param;
        private Item selectedItem;
        private String id;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.param = (Param)e.Parameter;
            this.ViewModel = new ItemViewModel(this.param);

            this.param.App.database.AddChangeListener((sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Changed local db");
                this.ViewModel.UpdateAll();

                this.UpdateItems();
                
            });
        }

        public async void UpdateItems()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MyListView.ItemsSource = null;
                MyListView.ItemsSource = this.ViewModel.Items;
            });
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private void Button_Click_Exit(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private async void Button_Click_Add(object sender, RoutedEventArgs e)
        {
            if (this.name.Text.Equals("")||this.value.Text.Equals(""))
            {
                var dialog = new MessageDialog("Name bzw. Einheit darf nicht leer sein.");
                await dialog.ShowAsync();
                return;
            }
            this.param.App.AddItem(this.name.Text, this.value.Text);
            this.name.Text = "";
            this.value.Text = "";
        }
        
        private void Button_Click_Edit(object sender, RoutedEventArgs e)
        {
            if (this.selectedItem == null) return;
            this.name.Text = this.selectedItem.Name;
            this.value.Text = this.selectedItem.Value;
            this.id = this.selectedItem.ID;

            this.ViewModel.Items.Remove(this.selectedItem);

            this.addButton.IsEnabled = false;
            this.editButton.IsEnabled = false;
            this.saveButton.IsEnabled = true;
        }

        private async void Button_Click_Save(object sender, RoutedEventArgs e)
        {
            if (this.name.Text.Equals("") || this.value.Text.Equals(""))
            {
                var dialog = new MessageDialog("Name bzw. Einheit darf nicht leer sein.");
                await dialog.ShowAsync();
                return;
            }

            using (var doc = this.param.App.database.GetDocument(this.id))
            using (var mutableDoc = doc.ToMutable())
            {
                mutableDoc.SetString("name", this.name.Text);
                mutableDoc.SetString("value", this.value.Text);
                this.param.App.database.Save(mutableDoc);
            }
            this.id = "";
            this.selectedItem = null;
            this.name.Text = "";
            this.value.Text = "";

            this.addButton.IsEnabled = true;
            this.editButton.IsEnabled = true;
            this.saveButton.IsEnabled = false;
        }

        private void Button_Click_Delete(object sender, RoutedEventArgs e)
        {
            Windows.UI.Xaml.Controls.Button button = (Windows.UI.Xaml.Controls.Button)sender;
            System.Diagnostics.Debug.WriteLine("delete" + button.DataContext);
            if (button.DataContext == null) return;
            Document doc = this.param.App.database.GetDocument((string) button.DataContext);
            this.param.App.database.Delete(doc);
        }

        private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.selectedItem = (Item) e.AddedItems?.FirstOrDefault();
            if (this.selectedItem == null) return;
        }

        public ItemViewModel ViewModel { get; set; }
    }

    public class ItemViewModel
    {
        private ObservableCollection<Item> items = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items { get { return this.items; } }
        private Param param;

        public ItemViewModel(Param param)
        {
            this.param = param;

            using (var query = QueryBuilder.Select(SelectResult.All())
                    .From(DataSource.Database(this.param.App.database)))
            {
                var result = query.Execute();
                var res = result.ToArray();
                foreach (var i in res)
                {
                    System.Diagnostics.Debug.WriteLine("Output " + Newtonsoft.Json.JsonConvert.SerializeObject(i));
                    System.Diagnostics.Debug.WriteLine("Output " + i.GetDictionary(0).GetString("ID"));


                    this.items.Add(new Item()
                    {
                        Name = i.GetDictionary(0).GetString("name"),
                        Value = i.GetDictionary(0).GetString("value"),
                        ID = i.GetDictionary(0).GetString("ID")
                    });
                }

                result = query.Execute();
                System.Diagnostics.Debug.WriteLine("Number " + result.Count());
            }
        }

        public async void UpdateAll()
        {
            using (var query = QueryBuilder.Select(SelectResult.All())
                   .From(DataSource.Database(this.param.App.database)))
            {
                var result = query.Execute();
                var res = result.ToArray();

                var documents = new List<string>();
                foreach (var i in res)
                {
                    Boolean found = false;
                    foreach (var item in Items)
                    {
                        if (item.ID.Equals(i.GetDictionary(0).GetString("ID")))
                        {
                            item.Name = i.GetDictionary(0).GetString("name");
                            item.Value = i.GetDictionary(0).GetString("value");
                            documents.Add(item.ID);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            this.items.Add(new Item()
                            {
                                Name = i.GetDictionary(0).GetString("name"),
                                Value = i.GetDictionary(0).GetString("value"),
                                ID = i.GetDictionary(0).GetString("ID")
                            });
                        });
                        
                        documents.Add(i.GetDictionary(0).GetString("ID"));
                    }
                }

                for (int i = 0; i < this.Items.Count; i++)
                {
                    var item = this.Items.ElementAt(i);

                    Boolean found = false;
                    foreach (var id in documents)
                    {
                        if (item.ID.Equals(id))
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            this.Items.RemoveAt(i);
                        });
                    }
                }
            }
        }
    }

    public class Item
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }

}
