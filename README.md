<h1 align="center">Jellyfin Intros Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org">Jellyfin Project</a></h3>

<p align="center">
This is a plugin built with DotNet that can download flashy intros from <a href="https://prerolls.video">prerolls.video</a> for your movies.
</p>

## Install Process

1. Open the Dashboard in Jellyfin, then under Advanced, select Plugins, and open Repositories from the top bar.

2. Click the '+' button, and add the Repository URL as 'https://dkanada.xyz/plugins/manifest.json', naming it whatever you like. Save.

3. Select Catalog from the top bar, and at the very bottom of the list will be 'Intros'. Click and install the most recent version.

4. Restart Jellyfin, and go back to the Plugins category, select My Plugins from the top bar and then 'Intros' to configure.

## Build Process

1. Clone or download this repository

2. Ensure you have DotNet Core SDK setup and installed

3. Build plugin with following command

```sh
dotnet publish --configuration Release --output bin
```

4. Place the resulting file in the `plugins` folder
