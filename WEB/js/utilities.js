export function interpolateColor(startColor, endColor, factor) {
    return startColor.map((start, index) => Math.round(start + factor * (endColor[index] - start)));
}

export function resetPathsAndPolygons(state) {
    state.flightPathLayer.clearLayers();
    state.polygonLayer.clearLayers();
    state.polygonMarkers.forEach(marker => {
        map.removeLayer(marker);
        if (marker.inputBox) {
            document.body.removeChild(marker.inputBox);
        }
    });
    state.polygonMarkers = [];
    state.currentPolygon = [];
    state.drawnPolygon = null;
    console.log("All paths, polygons, and dialogues have been cleared.");
}