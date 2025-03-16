export function exportFlightPath(state) {
    if (state.flightPathLayer.getLayers().length === 0) {
        alert("No flight path generated. Please select an area first.");
        return;
    }

    const DEG_TO_METERS = 111319.5; // Ensure this constant is defined
    const altitude = state.altitude || 100; // Default flight altitude

    let gcode = [];
    gcode.push("; G-Code for RDR2 Drone Mod");
    gcode.push("; Generated from Leaflet Flight Planner");
    gcode.push("G21 ; Set units to mm (meters * 1000)");
    gcode.push("G90 ; Absolute positioning");

    let previousPoint = null;

    state.flightPathLayer.eachLayer(function (line) {
        let latlngs = line.getLatLngs();

        latlngs.forEach((point) => {
            let x = (point.lng * DEG_TO_METERS).toFixed(2);
            let y = (point.lat * DEG_TO_METERS).toFixed(2);
            let z = altitude.toFixed(2);
            let heading = previousPoint ? ((Math.atan2(point.lat - previousPoint.lat, point.lng - previousPoint.lng) * 180 / Math.PI) + 360) % 360 : 0;

            gcode.push(`G1 X${x} Y${y} Z${z} A-60 B${heading.toFixed(2)} F500`);
            previousPoint = point;
        });
    });

    let blob = new Blob([gcode.join("\n")], { type: "text/plain" });
    let a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "flightpath.gcode";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}