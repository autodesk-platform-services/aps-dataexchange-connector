
# Sample UI Connector

[![oAuth2](https://img.shields.io/badge/oAuth2-v2-green.svg)](http://developer.autodesk.com/)
![.NET](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)
![Intermediary](https://img.shields.io/badge/Level-Intermediary-lightblue.svg)

# Description
This small application serves as a demo for setting up and using the Data Exchange SDK as a Nuget package. The sample demonstrates how one can Create or Update Data Exchanges using Data Exchange SDK.

# Thumbnail
![ThumbnailUI](https://github.com/autodesk-platform-services/aps-dataexchange-connector/assets/143052260/d20d7bea-4ee0-47a4-bb18-0e7d77af0497)

# Setup
**Data Exchange SDK** is installed into this project as a package reference. All required packages are a part of packages.config and will be restored automatically on first build.

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
      
      Step-1 download and uninstall nuget packages with appropriate versions to parent direcotry of repo.
      
      Step-2  either add the path of msbuild.exe **(VS 2022)** to the environment variables or utilize the developer command prompt **( for VS 2022 only)**.]
4. Add values for Client Id, Client Secret and Auth callback in the App.Debug.config file in the sample connector.

Once you build and run the sample connector, it will open the URL for authentication in a web browser. 
You can enter your credentials in the authentication page and on successful authententication, you will see the Connector UI screen as seen in the Thumbnail above. 

## Further Reading
### Documentation:
* [Data Exchange SDK](https://aps.autodesk.com/en/docs/dx-sdk-beta/v1/developers_guide/overview/) 

# License
This sample code is part of the Autodesk Data Exchange SDK beta. It is subject to the license in Center Code covering the Autodesk Data Exchange SDK beta.

# Written by
Rinku Thakur, PSET , Autodesk
