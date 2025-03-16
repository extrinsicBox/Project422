window.onload = function () {
    if (typeof map === 'undefined') {
        console.error("Error: Map is not defined. Ensure the map is initialized in index.html before loading script.js.");
        return;
    }

    /* **** CONSTANTS **** */
    const DEG_TO_METERS = 111319.5; // Approximate conversion factor (1 degree ≈ 111,319.5 meters)
    var altitude = 100; // Default flight altitude
    var stepover = 50; // Default stepover in meters (adjustable)

    /* **** Layers and Variables **** */
    var flightPathLayer = L.layerGroup().addTo(map);
    var polygonLayer = L.layerGroup().addTo(map);
    var currentPolygon = [];
    var drawnPolygon = null;
    var isDrawing = false;
    var isSelecting = false;
    var selectionBox = null;
    var selectionStart = null;
    var polygonMarkers = [];

    window.updateStepover = function () {
        let stepoverInput = document.getElementById("stepover");
        if (stepoverInput) {
            stepover = parseFloat(stepoverInput.value);
            console.log("Updated Stepover:", stepover);
            if (currentPolygon.length > 0) {
                generateFlightPathFromPolygon(currentPolygon); 
            }
        }
    };

    window.updateAltitude = function () {
        let altitudeInput = document.getElementById("altitude");
        if (altitudeInput) {
            altitude = parseFloat(altitudeInput.value);
            console.log("Updated Altitude:", altitude);
        }
    };

    function generateFlightPathFromPolygon(polygon) {
        console.log("Generating flight path from polygon:", polygon);
        flightPathLayer.clearLayers(); 

        let bounds = L.polygon(polygon).getBounds();
        let startX = bounds.getWest();
        let endX = bounds.getEast();
        let startY = bounds.getSouth();
        let endY = bounds.getNorth();

        console.log("Bounds:", bounds);
        console.log("StartX:", startX, "EndX:", endX, "StartY:", startY, "EndY:", endY);

        let spacing = stepover / DEG_TO_METERS;
        let direction = 1;
        let pathPoints = [];

        let closedPolygon = [...polygon.map(latlng => [latlng.lng, latlng.lat])];
        if (closedPolygon[0][0] !== closedPolygon[closedPolygon.length - 1][0] ||
            closedPolygon[0][1] !== closedPolygon[closedPolygon.length - 1][1]) {
            closedPolygon.push(closedPolygon[0]); 
        }
        let turfPolygon = turf.polygon([closedPolygon]);

        console.log("Closed Polygon:", closedPolygon);

        // Add profile pass along the polygon perimeter
        for (let i = 0; i < closedPolygon.length - 1; i++) {
            let start = closedPolygon[i];
            let end = closedPolygon[i + 1];
            L.polyline([[start[1], start[0]], [end[1], end[0]]], {
                color: "red",
                weight: 2
            }).addTo(flightPathLayer);
            pathPoints.push([start[1], start[0]]);
        }
        pathPoints.push([closedPolygon[0][1], closedPolygon[0][0]]); // Close the loop

        console.log("Profile Pass Path Points:", pathPoints);

        let offsetX = startX % spacing;
        let offsetY = startY % spacing;

        let totalLines = Math.ceil((endY - startY) / spacing);
        let totalColumns = Math.ceil((endX - startX) / spacing);

        console.log("Total Lines:", totalLines, "Total Columns:", totalColumns);

        for (let i = 0; i <= totalLines; i++) {
            let y = startY + (i * spacing) - offsetY;
            let line = [];

            for (let x = startX - offsetX; x <= endX; x += spacing) {
                let point = turf.point([x, y]);
                if (turf.booleanPointInPolygon(point, turfPolygon)) {
                    line.push([y, x]);
                }
            }

            if (line.length > 1) {
                if (direction === -1) line.reverse(); 
                pathPoints.push(...line);
            }
            direction *= -1;
        }

        direction = 1;
        for (let j = 0; j <= totalColumns; j++) {
            let x = startX + (j * spacing) - offsetX;
            let line = [];

            for (let y = startY - offsetY; y <= endY; y += spacing) {
                let point = turf.point([x, y]);
                if (turf.booleanPointInPolygon(point, turfPolygon)) {
                    line.push([y, x]);
                }
            }

            if (line.length > 1) {
                if (direction === -1) line.reverse(); 
                pathPoints.push(...line);
            }
            direction *= -1;
        }

        console.log("Raster Path Points:", pathPoints);

        for (let i = 0; i < pathPoints.length - 1; i++) {
            let start = pathPoints[i];
            let end = pathPoints[i + 1];
            let color = interpolateColor([0, 0, 255], [255, 255, 255], i / (pathPoints.length - 1));

            L.polyline([[start[0], start[1]], [end[0], end[1]]], {
                color: `rgb(${color[0]}, ${color[1]}, ${color[2]})`,
                weight: 2
            }).addTo(flightPathLayer);
        }

        console.log("Flight Path with Profile Pass Generated Inside Polygon:", pathPoints);
    }
};