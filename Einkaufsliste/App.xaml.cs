using Couchbase.Lite;
using Couchbase.Lite.Query;
using Couchbase.Lite.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Collections;
using System.Collections.ObjectModel;

namespace Einkaufsliste
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    public sealed partial class App : Application
    {

        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            this.StartDBConnection();
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird. Weitere Einstiegspunkte
        /// werden z. B. verwendet, wenn die Anwendung gestartet wird, um eine bestimmte Datei zu öffnen.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Zustand von zuvor angehaltener Anwendung laden
                }

                // Den Frame im aktuellen Fenster platzieren
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // Wenn der Navigationsstapel nicht wiederhergestellt wird, zur ersten Seite navigieren
                    // und die neue Seite konfigurieren, indem die erforderlichen Informationen als Navigationsparameter
                    // übergeben werden
                    Param param = new Param();
                    param.App = this;
                    rootFrame.Navigate(typeof(MainPage), param);
                }
                // Sicherstellen, dass das aktuelle Fenster aktiv ist
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Navigation auf eine bestimmte Seite fehlschlägt
        /// </summary>
        /// <param name="sender">Der Rahmen, bei dem die Navigation fehlgeschlagen ist</param>
        /// <param name="e">Details über den Navigationsfehler</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Anwendungszustand speichern und alle Hintergrundaktivitäten beenden
            deferral.Complete();
        }

        public void StartDBConnection()
        {
            Couchbase.Lite.Support.UWP.Activate();

            // Get the database (and create it if it doesn't exist)
            this.database = new Database("einkaufsliste");

            // Create replicator to push and pull changes to and from the cloud
            var targetEndpoint = new URLEndpoint(new Uri("ws://37.252.185.24:4984/db"));
            var replConfig = new ReplicatorConfiguration(this.database, targetEndpoint)
            {
                ReplicatorType = ReplicatorType.PushAndPull
            };
            replConfig.Channels = new List<String>();
            replConfig.Channels.Add("liste");
            replConfig.Continuous = true;

            // Add authentication
            replConfig.Authenticator = new BasicAuthenticator("UserEin", "Einkaufsliste");

            // Create replicator
            var replicator = new Replicator(replConfig);
            replicator.AddChangeListener((sender, args) =>
            {
                if (args.Status.Error != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error :: {args.Status.Error}");
                }
                System.Diagnostics.Debug.WriteLine("Test sync");

                using (var query = QueryBuilder.Select(SelectResult.All())
                .From(DataSource.Database(this.database)))
                {
                    // Run the query
                    var result = query.Execute();
                    var res = result.ToArray();
                    foreach (var i in res)
                    {
                        System.Diagnostics.Debug.WriteLine("Output " + Newtonsoft.Json.JsonConvert.SerializeObject(i));
                        System.Diagnostics.Debug.WriteLine("Output " + i.GetDictionary(0).GetString("name"));
                    }

                    result = query.Execute();
                    System.Diagnostics.Debug.WriteLine("Number " + result.Count());
                }
            });

            replicator.Start();     
        }

        public void AddItem(string name, String value)
        {
            using (var mutableDoc = new MutableDocument())
            {
                mutableDoc.SetString("name", name).SetString("value", value).SetString("ID", mutableDoc.Id);

                // Save it to the database
                this.database.Save(mutableDoc);
            }
        }

        public Database database { get; set; }
    }

    public class Param
    {
        public App App { get; set; }
    }
}
