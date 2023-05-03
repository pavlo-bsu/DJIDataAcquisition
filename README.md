# DJIDataAcquisition

In accordance with a legend, `DJI drone` sometimes has obscure malfunctions during a flight.

Mission: using `DJI Windows SDK`, find out drone subsystem that is causing malfunctions.

## Application logic
App logic is shown as UML sequence diagram in the figure shown hereinafter. The diagram shows the registration of the DroneStateChanged event as an example.

![Figure 1]( https://raw.githubusercontent.com/pavlo-bsu/DJIDataAcquisition/backmatter/DJIDAcquisition/img/umlSeqDiagram.png)

After launch, the app sends `DJI App Key` to `DJI server` for verification.

App establishes connection with a remote controller and waiting for connection of a remote controller and a drone.

Operator can start navigating a drone via remote controller.

To receive data from a drone, Operator presses button `Start receiving`. App subscribes to events from drone, camera, and remote controller. Events that should be registered can be added/removed in the source code (see file `DJIDAcquisition/VM/ViewModel.cs`). Each registered event is added to a collection that is displayed in UI (collection is locked during modification).

If registration of new events is not needed any more, Operator presses button `Stop receiving`. App unsubscribes from drone, camera, and remote controller events. All registered events are written to a json-file.

## Tools and accounts

1. Visual Studio with UWP components.
2. DJI account.
3. DJI drone supporting `DJI Windows SDK`.

## Steps for deploying

1. Register as a DJI Developer (see https://developer.dji.com/windows-sdk/documentation/application-development-workflow/workflow-register.html).
2. Create an `DJI App Key` for the application.
3. Set proper `DJI App Key` in the file `DJIkey.txt`.
4. Connect drone remote controller to a PC (see section `PRODUCT CONNECTION GUIDES` in https://developer.dji.com/windows-sdk/documentation/introduction/index.html).

Application is ready for receiving data from a drone.

## Using of DJI Windows SDK
See https://developer.dji.com/windows-sdk/documentation/introduction/index.html
