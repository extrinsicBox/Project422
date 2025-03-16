import { generateFlightPathFromPolygon } from './flightPath.js';
import { resetPathsAndPolygons } from './utilities.js';
import { handleMapClick, handleMapMouseDown, handleMapMouseMove, handleMapMouseUp } from './eventHandlers.js';
import { addNewWaypoint, createInputBox, updateInputBoxPosition } from './waypointManagement.js';
import { exportFlightPath } from './export.js';

window.onload = function () {
    if (typeof map === 'undefined') {
        console.error("Error: Map is not defined. Ensure the map is initialized in index.html before loading script.js.");
        return;
    }

    /* **** CONSTANTS **** */
    const DEG_TO_METERS = 111319.5; // Approximate conversion factor (1 degree â‰ˆ 111,319.5 meters)
    var altitude = 150; // Default flight altitude
    var stepover = 200; // Default stepover in meters (adjustable)

    /* **** Layers and Variables **** */
    const state = {
        flightPathLayer: L.layerGroup().addTo(map),
        polygonLayer: L.layerGroup().addTo(map),
        currentPolygon: [],
        drawnPolygon: null,
        isDrawing: false,
        isSelecting: false,
        selectionBox: null,
        selectionStart: null,
        polygonMarkers: [],
        stepover: stepover, // Ensure stepover is defined in the state
        altitude: altitude // Ensure altitude is defined in the state
    };

    window.updateStepover = function () {
        let stepoverInput = document.getElementById("stepover");
        if (stepoverInput) {
            state.stepover = parseFloat(stepoverInput.value);
            console.log("Updated Stepover:", state.stepover);
            if (state.currentPolygon.length > 0) {
                generateFlightPathFromPolygon(state.currentPolygon, state); 
            }
        }
    };

    window.updateAltitude = function () {
        let altitudeInput = document.getElementById("altitude");
        if (altitudeInput) {
            state.altitude = parseFloat(altitudeInput.value);
            console.log("Updated Altitude:", state.altitude);
        }
    };

    // Attach event handlers
    map.on('click', (e) => handleMapClick(e, state));
    map.on('mousedown', (e) => handleMapMouseDown(e, state));
    map.on('mousemove', (e) => handleMapMouseMove(e, state));
    map.on('mouseup', (e) => handleMapMouseUp(e, state));

    // Add event listener for the "R" key to reset paths, polygons, and dialogues
    document.addEventListener("keydown", function (e) {
        if (e.key === "r" || e.key === "R") {
            resetPathsAndPolygons(state);
        }
    });

    // Attach exportFlightPath to the download button
    document.getElementById("downloadButton").onclick = function () {
        exportFlightPath(state);
    };
};