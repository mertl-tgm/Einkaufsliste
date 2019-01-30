# Einkaufsliste

Eine genauere Dokumentation ist im Verzeichnis `protocol` zu finden.

## Funktion

Es handelt sich um eine einfache UWP-App, welche es ermöglicht eine Einkaufsliste über mehrere Geräte synchron zu halten.

## Plattform

Als Plattform zur Entwicklung wurde Couchbase gewählt und als Entwicklungsumgebung wurde UWP mit C# gewählt. Das Projekt ist als Visual Studio Projekt verfügbar, Visual Studio Version muss 2017 sein. 

Die App verwendet Couchbase Lite als lokale Datenbank, auf dem Server wurde ein Couchbase Server installiert. Dieser verwaltet alle Dokumente und mittels Sync Gateway werden diese mit allen Clients synchronisiert.

![Couchbase Plattform](https://blog.couchbase.com/wp-content/uploads/2017/03/building-net-apps-using-couchbase-lite-4-638.jpg)


## Usage

Unter dem Release Kategorie gibt es ein ZIP-Archive, dieses beinhaltet die App für die Plattformen x86, x64 und ARM. Mittels dem enthaltenen Powershell Skript kann die App auf der gewünschten Plattform installiert werden.
