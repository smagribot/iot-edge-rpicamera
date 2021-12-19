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

