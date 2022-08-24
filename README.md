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
- Open the ```ExportSampleImages_ViewExtensionDefinition.xml``` and change the path to the ```ExportSampleImagesViewExtension.dll``` by providing the correct path inside the ```<AssemblyPath></AssemblyPath>``` tags (the file should be in the ```bin\Debug``` of your solution) 
- Copy the ```ExportSampleImages_ViewExtensionDefinition.xml``` file from the manifest directory to the ```viewExtensions``` folder of Dynamo (this can be under the ```bin\Debug``` folder if you are working in Sandbox environment, or the ```C:\Program Files\Autodesk\Revit 202x\AddIns\DynamoForRevit\viewExtensions``` for Dynamo Revit).

If you have done eveything correctly, you should see 'Show Export Sample Images Extension' under the Extensions tab in Dynamo.

<img src="https://user-images.githubusercontent.com/5354594/186402333-7c49302b-a544-41ec-8dc2-20310c215419.png" width="200">

## Testing
WIP

### Debugging
WIP

### Running Unit Tests

