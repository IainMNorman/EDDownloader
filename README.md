# EDDownloader - Elite: Dangerous Downloader

A quick and dirty console app that downloads Elite: Dangerous files from the public server.

## Alpha notes

This will download the latest publically available release. Not any beta. It's currently not dealing well when it's checking existing files so it's best to always download to a clean folder and then copy those files over to your ED install.

Then from the launcher choose option=>validate game files to make sure EDDownloader didnt' miss anything.

## Usage

Download the [latest release](https://github.com/IainMNorman/EDDownloader/releases/download/0.2/edd.alpha.0.2.zip), unzip and double click edd.exe to run.

You can also pass arguments if you run from the commandline or create a shortcut.

edd [max downloads] [location path]

If you don't provide _edd_ with either arguments then it will default to 16 concurrent downloads and downloads to a 'download' folder in it's current location.

### Legal

There shoulnd't be a problem with this but if Frontier wish it removed I will do so.
