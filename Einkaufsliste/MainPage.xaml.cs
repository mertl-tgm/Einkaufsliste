using Couchbase.Lite.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
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
            });
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            //SplitView.IsPaneOpen = !SplitView.IsPaneOpen;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.param.App.AddItem("Apfel", "1");
        }
        
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {

        }

        private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Test");
            //var item = e.AddedItems?.FirstOrDefault();
            // edit: also get container
            //var container = ((ListViewItem)(listView.ContainerFromItem(item)));
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
                // Run the query
                var result = query.Execute();
                var res = result.ToArray();
                foreach (var i in res)
                {
                    System.Diagnostics.Debug.WriteLine("Output " + Newtonsoft.Json.JsonConvert.SerializeObject(i));
                    System.Diagnostics.Debug.WriteLine("Output " + i.GetDictionary(0).GetString("name"));


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
                // Run the query
                var result = query.Execute();
                var res = result.ToArray();

                var documents = new List<string>();
                foreach (var i in res)
                {
                    Boolean found = false;
                    foreach (var item in Items)
                    {
                        if (item.ID == i.GetDictionary(0).GetString("id"))
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
                        await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                        () =>
                        {
                            this.items.Add(new Item()
                            {
                                Name = i.GetDictionary(0).GetString("name"),
                                Value = i.GetDictionary(0).GetString("value"),
                                ID = i.GetDictionary(0).GetString("ID")
                            });
                        }
                        );
                        
                        documents.Add(i.GetDictionary(0).GetString("ID"));
                    }
                }

                for (int i = 0; i < this.Items.Count; i++)
                {
                    var item = this.Items.ElementAt(i);

                    Boolean found = false;
                    foreach (var id in documents)
                    {
                        if (item.ID == id)
                        {
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        this.Items.RemoveAt(i);
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
