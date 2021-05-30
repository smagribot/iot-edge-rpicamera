# iot-edge-rpicamera

Takes a picture with the Raspberry Pi Camera and uploads it to Azure Blob Storage on IoT Edge, which can upload the image to a Blob Storage in the cloud.

## Example .env
See https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2020-11
```sh
LOCAL_STORAGE_PATH=/srv/containerdata
LOCAL_STORAGE_ACCOUNT_NAME=<ACCOUNT NAME>
LOCAL_STORAGE_ACCOUNT_KEY=<YOUR KEY>
```

## IoTEdgeRaspberryPiCamera example desired properties:
```json
"timelapseConfig": {
    "period": 900
},
"imageConfig":{
    "width": 1280,
    "height": 720,
    "shutterSpeed": 0,
    "iso": 0,
    "encodingFormat": "JPEG",
    "pixelFormat": "I420",
    "quality": 90
}
```

## Azure Blob Storage on IoT Edge
See https://docs.microsoft.com/en-us/azure/iot-edge/how-to-store-data-blob?view=iotedge-2020-11


# TODO:
Azure IoT Edge RaspberryPi Camera Module
Version: 1.0.0.0
[2021-05-30T13:17:25Z] dbug: Raspberry Pi Camera Module[0] Connecting to IoT Hub Edge
[2021-05-30T13:17:27Z] dbug: Raspberry Pi Camera Module[0] Desired property changed: {"$version":1}
[2021-05-30T13:17:27Z] info: Raspberry Pi Camera Module[0] Setup new interval for timelapse interval. Interval is 15 minutes
[2021-05-30T13:17:31Z] info: Raspberry Pi Camera Module[0] Uploaded picture
[2021-05-30T13:25:29Z] dbug: Raspberry Pi Camera Module[0] Desired property changed: {"TimelapseConfig":{"Period":900},"ImageConfig":{"Width":1280,"Height":720,"ShutterSpeed":0,"ISO":0,"EncodingFormat":"JPEG","PixelFormat":"I420","Quality":90},"$version":2}
[2021-05-30T13:25:29Z] info: Raspberry Pi Camera Module[0] Got new image config.
[2021-05-30T13:32:30Z] info: Raspberry Pi Camera Module[0] Uploaded picture
[2021-05-30T14:02:27Z] fail: Raspberry Pi Camera Module[0] Uploaded picture interval got error: MMALSharp.MMALInvalidException: Argument is invalid. Unable to set camera config.    at MMALSharp.MMALNativeExceptionHelper.MMALCheck(MMAL_STATUS_T status, String message)    at MMALSharp.MMALCameraComponentExtensions.SetCameraConfig(MMALCameraComponent camera, MMAL_PARAMETER_CAMERA_CONFIG_T value)    at MMALSharp.Components.MMALCameraComponent.Initialise(IOutputCaptureHandler stillCaptureHandler, IOutputCaptureHandler videoCaptureHandler)    at MMALSharp.MMALCamera.ConfigureCameraSettings(IOutputCaptureHandler stillCaptureHandler, IOutputCaptureHandler videoCaptureHandler)    at IoTEdgeRaspberryPiCamera.Services.Camera.RpiCameraModule.TakePicture(ImageConfig imageConfig) in /app/Services/Camera/RpiCameraModule.cs:line 26

