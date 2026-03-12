// Heatmap Helper - renders Leaflet heatmap from analytics data
window.HeatmapHelper = {
    map: null,
    heatLayer: null,

    render: function (containerId, centerLat, centerLng, points) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn('[Heatmap] Container not found:', containerId);
            return;
        }

        // Destroy existing map
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.heatLayer = null;
        }

        // Initialize map
        this.map = L.map(containerId).setView([centerLat, centerLng], 15);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(this.map);

        // Add center marker
        L.marker([centerLat, centerLng], {
            icon: L.divIcon({
                className: 'heatmap-center',
                html: '<div style="font-size:24px">📍</div>',
                iconSize: [30, 30],
                iconAnchor: [15, 30]
            })
        })
        .addTo(this.map)
        .bindPopup('Tâm khu vực');

        if (points && points.length > 0) {
            // Create heat layer
            this.heatLayer = L.heatLayer(points, {
                radius: 25,
                blur: 15,
                maxZoom: 17,
                max: 1.0,
                gradient: {
                    0.0: '#3b82f6',  // blue - ít
                    0.3: '#06b6d4',  // cyan
                    0.5: '#10b981',  // green - trung bình
                    0.7: '#f59e0b',  // amber
                    0.9: '#ef4444',  // red
                    1.0: '#dc2626'   // dark red - nhiều
                }
            }).addTo(this.map);

            // Fit bounds
            var lats = points.map(p => p[0]);
            var lngs = points.map(p => p[1]);
            var bounds = [
                [Math.min(...lats) - 0.002, Math.min(...lngs) - 0.002],
                [Math.max(...lats) + 0.002, Math.max(...lngs) + 0.002]
            ];
            this.map.fitBounds(bounds);

            console.log('[Heatmap] Rendered', points.length, 'points');
        } else {
            console.log('[Heatmap] No data points, showing empty map');
        }
    }
};
