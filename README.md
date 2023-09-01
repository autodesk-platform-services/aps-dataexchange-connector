
# Sample UI Connector

[![oAuth2](https://img.shields.io/badge/oAuth2-v2-green.svg)](http://developer.autodesk.com/)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)
![Intermediary](https://img.shields.io/badge/Level-Intermediary-lightblue.svg)

# Description
This application is an example that serves as a reference implemention for a UI-based, Autodesk Data Exchange Connector. The sample code supports creating and updating as well as retrieving an exchange via a desktop-based UI and provides integration points for client-applications. 

This is recommended for solutions that require integrating the Autodesk Data Exchange service with a Windows desktop based application.

For samples that do not use the UI component see https://github.com/autodesk-platform-services/aps-dataexchange-console

# Thumbnail
![ThumbnailUI](images/Thumbnail.png)

# Setup
The **Autodesk Data Exchange SDK** is installed into this project as a package reference. All required packages are a part of packages.config and will be restored automatically on first build.

## Prerequisites
1. [Register an app](https://aps.autodesk.com/myapps/), and select the Data Management and the Data Exchange APIs. Note down the values of **Client ID, Client Secret and Auth callback**. For more information on different types of apps, refer [Application Types](https://aps.autodesk.com/en/docs/oauth/v2/developers_guide/App-types/) page.
2. Verify that you have access to the [Autodesk Construction Cloud](https://acc.autodesk.com/) (ACC).
3. **Visual Studio**.
4. **Dot NET Framework 4.8** with basic knowledge of C#.

## Running locally
1. Clone this repository using *git clone*.
2. Follow [these](https://aps.autodesk.com/en/docs/dx-sdk-beta/v1/developers_guide/installing_the_sdk/#procedure) instructions for installing the Data Exchange .Net SDK NuGet package in Visual Studio.
3. Restore the Data Exchange SDK packages by one of the following approaches:
    * Building the solution using Visual Studio IDE, or 
    * Building the solution using *BuildSolution.bat* [Note:Prior to executing "BuildSolution.bat," follow these steps
      
      Step-1 download and unzip nuget packages with appropriate versions to parent directory of repo.
      
      Step-2  either add the path of msbuild.exe **(VS 2022)** to the environment variables or utilize the developer command prompt **( for VS 2022 only)**.]
4. Add values for Client Id, Client Secret and Auth callback in the App.Debug.config file in the sample connector.

Once you build and run the sample connector, it will open the URL for authentication in a web browser. 
You can enter your credentials in the authentication page and on successful authentication, you will see the Connector UI screen as seen in the Thumbnail above. 

## Further Reading
### Documentation:
* [Autodesk Data Exchange SDK](https://aps.autodesk.com/en/docs/dx-sdk-beta/v1/developers_guide/overview/) 

# License
This sample code is part of the Autodesk Data Exchange .NET SDK (Software Development Kit) beta. It is subject to the license in Center Code covering the Autodesk Data Exchange .NET SDK (Software Development Kit) beta.

# Written by
Rinku Thakur, Autodesk
