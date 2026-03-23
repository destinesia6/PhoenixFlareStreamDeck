//var websocket = null,
//    uuid = null,
//    actionInfo = {},
//    settings = {};

window.connectElgatoStreamDeckSocket = (inPort, inUUID, inRegisterEvent, inInfo, inActionInfo) => {
    const uuid = inUUID;
    const actionInfo = JSON.parse(inActionInfo);
    const settings = actionInfo.payload.settings;
    var select = document.getElementById('deviceSelector');
    select.innerHTML = '<option value="">looking for websocket</option>';

    const websocket = new WebSocket('ws://127.0.0.1:' + inPort);

    websocket.onopen = function () {
        // Register the PI
        websocket.send(JSON.stringify({ event: inRegisterEvent, uuid: inUUID }));

        // Ask C# to fetch devices from MAUI
        websocket.send(JSON.stringify({
            event: "sendToPlugin",
            context: uuid,
            action: actionInfo.action,
            payload: { command: "FETCH_DEVICES_FROM_MAUI" }
        }));
    };

    websocket.onmessage = function (evt) {
        var jsonObj = JSON.parse(evt.data);
        var select = document.getElementById('deviceSelector');
        if (jsonObj.event === "sendToPropertyInspector") {
            var payload = jsonObj.payload;
            if (payload && payload.devices) {
                updateDeviceDropdown(JSON.parse(payload.devices), websocket, uuid, settings);
            }
            else {
                select.innerHTML = '<option value="">payload null or empty</option>';
            }
        }
        else {
            select.innerHTML = '<option value="">notSendToInspector</option>';
        }
    };
}

function updateDeviceDropdown(devices, websocket, uuid, settings) {
    const select = document.getElementById('deviceSelector');
    select.innerHTML = '<option value="">-- Select Light --</option>';

    devices.forEach(dev => {
        const opt = document.createElement('option');
        opt.value = dev.id;
        opt.innerHTML = dev.name;
        if (settings.deviceId === dev.Id) opt.selected = true;
        select.appendChild(opt);
    });
    
    select.value = settings.deviceId;
    
    select.onchange = function () {
        settings.deviceId = this.value;
        websocket.send(JSON.stringify({
            event: "setSettings",
            context: uuid,
            payload: settings
        }));
    }
}