import { interpolateColor } from './utilities.js';

const DEG_TO_METERS = 111319.5; // Conversion factor from degrees to meters

// Function to generate a flight path from a given polygon
export function generateFlightPathFromPolygon(polygon, state) {
    console.log("Generating flight path from polygon:", polygon);
    state.flightPathLayer.clearLayers(); // Clear existing flight path layers

    if (polygon.length === 0) {
        console.error("Error: Polygon is empty.");
        return;
    }

    // Get the bounding box of the polygon
    let bounds = L.polygon(polygon).getBounds();
    let startX = bounds.getWest();
    let endX = bounds.getEast();
    let startY = bounds.getSouth();
    let endY = bounds.getNorth();

    console.log("Bounds:", bounds);
    console.log("StartX:", startX, "EndX:", endX, "StartY:", startY, "EndY:", endY);

    let spacing = state.stepover / DEG_TO_METERS; // Calculate spacing in degrees
    let direction = 1; // Initial direction for raster pass
    let pathPoints = []; // Array to store path points

    // Close the polygon if not already closed
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
        }).addTo(state.flightPathLayer);
        pathPoints.push([start[1], start[0]]);
    }
    pathPoints.push([closedPolygon[0][1], closedPolygon[0][0]]); // Close the loop

    console.log("Profile Pass Path Points:", pathPoints);

    let offsetX = startX % spacing;
    let offsetY = startY % spacing;

    let totalLines = Math.ceil((endY - startY) / spacing);
    let totalColumns = Math.ceil((endX - startX) / spacing);

    console.log("Total Lines:", totalLines, "Total Columns:", totalColumns);

    // Generate horizontal raster pass
    for (let i = 0; i <= totalLines; i++) {
        let y = startY + (i * spacing) - offsetY;
        let startPoint = null;
        let endPoint = null;

        for (let x = startX - offsetX; x <= endX; x += spacing) {
            let point = turf.point([x, y]);
            if (turf.booleanPointInPolygon(point, turfPolygon)) {
                if (!startPoint) startPoint = [y, x];
                endPoint = [y, x];
            }
        }

        if (startPoint && endPoint) {
            if (direction === -1) [startPoint, endPoint] = [endPoint, startPoint]; // Reverse direction for zigzag pattern
            pathPoints.push(startPoint, endPoint);
        }
        direction *= -1; // Toggle direction
    }

    direction = 1;
    // Generate vertical raster pass
    for (let j = 0; j <= totalColumns; j++) {
        let x = startX + (j * spacing) - offsetX;
        let startPoint = null;
        let endPoint = null;

        for (let y = startY - offsetY; y <= endY; y += spacing) {
            let point = turf.point([x, y]);
            if (turf.booleanPointInPolygon(point, turfPolygon)) {
                if (!startPoint) startPoint = [y, x];
                endPoint = [y, x];
            }
        }

        if (startPoint && endPoint) {
            if (direction === -1) [startPoint, endPoint] = [endPoint, startPoint]; // Reverse direction for zigzag pattern
            pathPoints.push(startPoint, endPoint);
        }
        direction *= -1; // Toggle direction
    }

    console.log("Raster Path Points:", pathPoints);

    // Draw the flight path with interpolated colors
    for (let i = 0; i < pathPoints.length - 1; i += 2) {
        let start = pathPoints[i];
        let end = pathPoints[i + 1];
        let color = interpolateColor([0, 0, 255], [255, 255, 255], i / (pathPoints.length - 1));

        L.polyline([[start[0], start[1]], [end[0], end[1]]], {
            color: `rgb(${color[0]}, ${color[1]}, ${color[2]})`,
            weight: 2
        }).addTo(state.flightPathLayer);
    }

    console.log("Flight Path with Profile Pass Generated Inside Polygon:", pathPoints);
}

// Function to update the polygon on the map
export function updatePolygon(state) {
    const { currentPolygon, polygonLayer, drawnPolygon, flightPathLayer } = state;

    if (drawnPolygon) {
        polygonLayer.removeLayer(drawnPolygon); // Remove the existing polygon
    }
    state.drawnPolygon = L.polygon(currentPolygon, { color: 'blue', weight: 2 }).addTo(polygonLayer); // Draw the new polygon

    if (currentPolygon.length >= 3) {
        generateFlightPathFromPolygon(currentPolygon, state); // Generate flight path if polygon has at least 3 points
    } else {
        flightPathLayer.clearLayers(); // Clear the path if fewer than 3 markers remain
    }
}