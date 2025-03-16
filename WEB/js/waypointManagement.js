import { updatePolygon, generateFlightPathFromPolygon } from './flightPath.js';

const DEG_TO_METERS = 111319.5; // Ensure this constant is defined

export function addNewWaypoint(latlng, state) {
    // Add new vertex
    state.currentPolygon.push(latlng);

    let marker = L.marker(latlng, { 
        draggable: true, 
        icon: L.divIcon({ 
            className: 'custom-marker', 
            iconSize: [20, 20] // Adjust the size to match the CSS
        }) 
    }).addTo(map);
    marker.on('drag', function (event) {
        let index = state.polygonMarkers.indexOf(marker);
        if (index !== -1) {
            state.currentPolygon[index] = event.target.getLatLng();
            updatePolygon(state);
            updateInputBoxPosition(marker);
        }
    });

    marker.on('mouseup', function () {
        updateInputBoxPosition(marker); // Update the dialogue position on mouse up
    });

    // Add left-click event listener to open input boxes
    marker.on('click', function () {
        // Close any existing input box
        state.polygonMarkers.forEach(m => {
            if (m.inputBox) {
                document.body.removeChild(m.inputBox);
                m.inputBox = null;
            }
        });

        // Open new input box
        marker.inputBox = createInputBox(marker, marker.getLatLng(), state.polygonMarkers.indexOf(marker), state);
    });

    // Add right-click event listener to delete the marker
    marker.on('contextmenu', function () {
        let index = state.polygonMarkers.indexOf(marker);
        if (index !== -1) {
            state.polygonMarkers.splice(index, 1);
            state.currentPolygon.splice(index, 1);
            map.removeLayer(marker);
            updatePolygon(state);
        }
    });

    state.polygonMarkers.push(marker);

    if (state.currentPolygon.length > 2) {
        generateFlightPathFromPolygon(state.currentPolygon, state);
    }

    if (state.drawnPolygon) {
        state.polygonLayer.removeLayer(state.drawnPolygon);
    }
    state.drawnPolygon = L.polygon(state.currentPolygon, { color: 'blue', weight: 2 }).addTo(state.polygonLayer);
}

export function createInputBox(marker, latlng, index, state) {
    let container = document.createElement('div');
    container.style.position = 'absolute';
    container.style.backgroundColor = 'white';
    container.style.border = '1px solid black';
    container.style.padding = '5px';
    container.style.zIndex = '1000';
    container.style.width = '150px';

    let title = document.createElement('div');
    title.innerText = `Waypoint #${index + 1}`;
    title.style.marginBottom = '5px';
    container.appendChild(title);

    let inputX = document.createElement('input');
    inputX.type = 'text';
    inputX.value = (latlng.lng * DEG_TO_METERS).toFixed(2);
    inputX.style.marginRight = '5px';
    inputX.style.width = '60px';
    inputX.oninput = function () {
        let newLatLng = L.latLng(parseFloat(inputY.value) / DEG_TO_METERS, parseFloat(inputX.value) / DEG_TO_METERS);
        marker.setLatLng(newLatLng);
        updatePolygon(state);
        updateInputBoxPosition(marker);
    };

    let inputY = document.createElement('input');
    inputY.type = 'text';
    inputY.value = (latlng.lat * DEG_TO_METERS).toFixed(2);
    inputY.style.width = '60px';
    inputY.oninput = function () {
        let newLatLng = L.latLng(parseFloat(inputY.value) / DEG_TO_METERS, parseFloat(inputX.value) / DEG_TO_METERS);
        marker.setLatLng(newLatLng);
        updatePolygon(state);
        updateInputBoxPosition(marker);
    };

    container.appendChild(inputX);
    container.appendChild(inputY);
    document.body.appendChild(container);

    // Position the container next to the marker
    let point = map.latLngToContainerPoint(latlng);
    container.style.left = `${point.x + 10}px`;
    container.style.top = `${point.y + 10}px`;

    return container;
}

export function updateInputBoxPosition(marker) {
    if (marker.inputBox) {
        let latlng = marker.getLatLng();
        let point = map.latLngToContainerPoint(latlng);
        marker.inputBox.style.left = `${point.x + 10}px`;
        marker.inputBox.style.top = `${point.y + 10}px`;

        // Update input values
        let inputs = marker.inputBox.querySelectorAll('input[type="text"]');
        inputs[0].value = (latlng.lng * DEG_TO_METERS).toFixed(2);
        inputs[1].value = (latlng.lat * DEG_TO_METERS).toFixed(2);
    }
}