import { addNewWaypoint, createInputBox, updateInputBoxPosition } from './waypointManagement.js';
import { updatePolygon, generateFlightPathFromPolygon } from './flightPath.js';

const TOLERANCE = 1e-6;

function latLngEquals(latlng1, latlng2) {
    return Math.abs(latlng1.lat - latlng2.lat) < TOLERANCE && Math.abs(latlng1.lng - latlng2.lng) < TOLERANCE;
}

export function handleMapClick(e, state) {
    if (state.isSelecting) return; // Ignore if using rectangle selection

    if (!state.isDrawing) {
        state.isDrawing = true;
        state.currentPolygon = [];

        // Clear previous rectangle selection
        if (state.selectionBox) {
            state.flightPathLayer.removeLayer(state.selectionBox);
            state.selectionBox = null;
        }

        state.polygonMarkers.forEach(marker => map.removeLayer(marker));
        state.polygonMarkers = [];
        state.polygonLayer.clearLayers(); // Clear previous polygon
    }

    // Clear previous path if a new waypoint is placed
    state.flightPathLayer.clearLayers();

    // Add new vertex
    state.currentPolygon.push(e.latlng);

    let marker = L.marker(e.latlng, { 
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

export function handleMapMouseDown(e, state) {
    if (e.originalEvent.ctrlKey && e.originalEvent.button === 0) { // Ctrl + Left Click
        state.isSelecting = true;
        state.isDrawing = false; // Stop polygon drawing
        state.currentPolygon = [];

        // Close any existing input boxes
        state.polygonMarkers.forEach(marker => {
            if (marker.inputBox) {
                document.body.removeChild(marker.inputBox);
                marker.inputBox = null;
            }
        });

        state.polygonMarkers.forEach(marker => map.removeLayer(marker));
        state.polygonMarkers = [];
        state.polygonLayer.clearLayers(); // Remove polygon if present

        map.dragging.disable();
        state.selectionStart = e.latlng;
        state.selectionBox = L.rectangle([state.selectionStart, state.selectionStart], { color: "blue", weight: 1 }).addTo(state.flightPathLayer);

        map.on('mousemove', (e) => updateSelection(e, state));
    } else {
        if (state.isSelecting) return; // Ignore if using rectangle selection

        // Check if the click is on an existing marker
        let isClickOnMarker = state.polygonMarkers.some(marker => {
            return latLngEquals(marker.getLatLng(), e.latlng);
        });

        if (isClickOnMarker) {
            // Close any existing input box
            state.polygonMarkers.forEach(m => {
                if (m.inputBox) {
                    document.body.removeChild(m.inputBox);
                    m.inputBox = null;
                }
            });

            // Enable dragging for the existing marker
            state.polygonMarkers.forEach(marker => {
                if (latLngEquals(marker.getLatLng(), e.latlng)) {
                    marker.dragging.enable();
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

                    // Open new input box
                    marker.inputBox = createInputBox(marker, marker.getLatLng(), state.polygonMarkers.indexOf(marker), state);
                }
            });
            return; // Prevent adding a new waypoint if clicking on an existing marker
        }

        if (!state.isDrawing) {
            state.isDrawing = true;
            state.currentPolygon = [];

            // Clear previous rectangle selection
            if (state.selectionBox) {
                state.flightPathLayer.removeLayer(state.selectionBox);
                state.selectionBox = null;
            }

            state.polygonMarkers.forEach(marker => map.removeLayer(marker));
            state.polygonMarkers = [];
            state.polygonLayer.clearLayers(); // Clear previous polygon
        }

        // Clear previous path if a new waypoint is placed
        state.flightPathLayer.clearLayers();
    }
}

export function handleMapMouseMove(e, state) {
    if (state.isSelecting && state.selectionBox) {
        state.selectionBox.setBounds([state.selectionStart, e.latlng]);
    }
}

export function handleMapMouseUp(e, state) {
    if (state.isSelecting) {
        state.isSelecting = false;
        map.dragging.enable();
        map.off('mousemove', updateSelection);

        if (state.selectionBox) {
            const bounds = state.selectionBox.getBounds();
            const northEast = bounds.getNorthEast();
            const southWest = bounds.getSouthWest();
            state.currentPolygon = [
                southWest,
                L.latLng(southWest.lat, northEast.lng),
                northEast,
                L.latLng(northEast.lat, southWest.lng),
                southWest
            ];
            state.selectionBox = null;

     

            generateFlightPathFromPolygon(state.currentPolygon, state);
        }
    } else {
        map.dragging.enable();
    }
}

function updateSelection(e, state) {
    if (!state.selectionBox || !state.isSelecting) return;
    state.selectionBox.setBounds([state.selectionStart, e.latlng]);
}

// Add event listener for keydown and keyup to handle map dragging
document.addEventListener('keydown', function (e) {
    if (e.key === 'Control') {
        map.dragging.disable();
    }
});

document.addEventListener('keyup', function (e) {
    if (e.key === 'Control') {
        map.dragging.enable();
    }
});