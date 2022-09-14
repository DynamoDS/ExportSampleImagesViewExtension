# ExportSampleImagesViewExtension
 Export Sample Images for Dynamo

ExportSampleImages (ESI) is a View Extension for Dynamo which allows you to create images from Dyanmo graphs. The resulting image is a combination of the graph workspace combined with the geometry output view. The workspace will be 'cleaned-up' before the export resulting in a nice and tidy looking images. 
The extension supports multiple graphs and sports a basic, although informative UI showing the names of the Graphs to be exported, the progress and final result. You can refer to the images below for a brief introduction to ESI.

#### Workflow
![demo](https://user-images.githubusercontent.com/5354594/186433761-c28b4ea5-c55c-4d77-b7eb-0b4406a06167.gif)

#### Exporting Images
![demo_2](https://user-images.githubusercontent.com/5354594/186436384-f2060822-0394-4a91-aacf-21132fbf2d24.gif)


## Building

### Recommended Build Environment
- VisualStudio 2022
- .Net Framework 4.8

### Build Process
- Fork or download the repository
- ```Build``` the soluition 
- **Manifest File** (tell Dynamo where to find this Extension) - Open the ```ExportSampleImages_ViewExtensionDefinition.xml``` located in the ```manifest``` folder and change the path to the ```ExportSampleImagesViewExtension.dll``` by providing the correct path inside the ```<AssemblyPath></AssemblyPath>``` tags (the file should be in the ```bin\Debug``` of your solution) 
- Copy the ```ExportSampleImages_ViewExtensionDefinition.xml``` file from the manifest directory to the ```viewExtensions``` folder of Dynamo (this can be under the ```bin\Debug``` folder if you are working in Sandbox environment, or the ```C:\Program Files\Autodesk\Revit 202x\AddIns\DynamoForRevit\viewExtensions``` for Dynamo Revit).
-- Alternatively under the ```Build Events``` section of your solution, remove the **'REM'** infront of the Post-build event command line, and replace the **[YOUR_DYNAMO_SANDOX_LOCATION]** with the location of your Dynamo Sandbox solution (```REM copy /Y $(SolutionDir)ExportSampleImagesViewExtension\manifest\*.xml [YOUR_DYNAMO_SANDBOX_LOCATION]\Dynamo\bin\AnyCPU\Debug\viewExtensions```)

If you have done eveything correctly, you should see 'Show Export Sample Images Extension' under the Extensions tab in Dynamo.

<img src="https://user-images.githubusercontent.com/5354594/186402333-7c49302b-a544-41ec-8dc2-20310c215419.png" width="200">

## Debugging & Testing
In order to debug or run/create Unit Tests, you will have to take a few additional steps.

## Debugging
- Download DynamoCoreRuntime 2.16.0 (or higher) from https://dynamobuilds.com/. Alternatively, you can build Dynamo from Dynamo repository and use the bin folder equivalently.
- Copy all contents of the DynamoCoreRuntime to ```ExportSampleImagesViewExtension\ExportSampleImagesTests\bin\Debug\```. If you are building Dynamo locally, copy all contents of Dynamo from Dynamo/bin/AnyCPU/Debug to ```ExportSampleImagesViewExtension\ExportSampleImagesTests\bin\Debug\```
- Copy ExportSampleImages_ViewExtensionDefinition.xml from ```ExportSampleImagesViewExtension\ExportSampleImagesViewExtension\manifests\``` to ```ExportSampleImagesViewExtension\ExportSampleImagesTests\bin\Debug\viewExtensions\```
- Open the copied ```ExportSampleImages_ViewExtensionDefinition.xml``` and change the assemply path to ```..\ExportSampleImagesViewExtension.dll```
- Remove Export Sample Images from your Dynamo packages folder if you have it installed from package manager (otherwise ```ExportSampleImagesViewExtension.dll``` will get loaded twice). 
- Launch DynamoSandbox.exe, then click View-> Open Export Sample Images and use start debugging as you normally would.


### Testing
Since ExportSampleImages 

### Running Unit Tests
- Install NUnit 2 Test Adapter from VisualStudio->Extensions->Manage Extensions->Online.
- Open Test Explorer from VisualStudio->Test->Test Explorer. Now you should see a list of TuneUpTests.
- **If you are running Visual Studio 2022 and you are having issues, try using **ReSharper** and the test module it provides instead.*
- Make sure you are using 64bit Testing architecture by going to ```Test->Processor Architecture for AnyCPU Projects->x64```
- Click the target test to run or run them all.

<img src="https://user-images.githubusercontent.com/5354594/190202380-b05b7f9e-2223-4442-ba4d-16dca27d8c47.png" width="450">

## Known Issues
- Runing graphs under Dynamo or Revit could trigger Notification Dialogs (prompts) which will interrupt the flow.
- In rare circumstances, graphs could cause silent crash. In these scenarious, try utilizing the 'log.txt' file saved in the root folder of your Target location. The Extension would allow you to attemt and Resume your run from the last recorded graph
