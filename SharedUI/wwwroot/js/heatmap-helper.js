// Heatmap Helper - renders MapBox heatmap from analytics data
window.HeatmapHelper = {
    map: null,

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
        }

        // Initialize map
        this.map = new mapboxgl.Map({
            container: containerId,
            style: 'mapbox://styles/mapbox/dark-v11', // Dark style looks better for heatmap
            center: [centerLng, centerLat],
            zoom: 14
        });

        // Add center marker
        const el = document.createElement('div');
        el.className = 'heatmap-center';
        el.innerHTML = '<div style="font-size:24px; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.5));">📍</div>';
        
        new mapboxgl.Marker(el)
            .setLngLat([centerLng, centerLat])
            .setPopup(new mapboxgl.Popup({ offset: 25 }).setHTML('Tâm khu vực'))
            .addTo(this.map);

        const self = this;
        this.map.on('load', () => {
            if (points && points.length > 0) {
                // Convert points to GeoJSON
                const geojson = {
                    type: 'FeatureCollection',
                    features: points.map(p => ({
                        type: 'Feature',
                        geometry: {
                            type: 'Point',
                            coordinates: [p[1], p[0]] // Mapbox uses [lng, lat]
                        },
                        properties: {
                            // If your points have intensity at p[2], use it, otherwise 1
                            weight: p.length > 2 ? p[2] : 1
                        }
                    }))
                };

                self.map.addSource('heatmap-data', {
                    type: 'geojson',
                    data: geojson
                });

                self.map.addLayer({
                    id: 'heatmap-layer',
                    type: 'heatmap',
                    source: 'heatmap-data',
                    maxzoom: 17,
                    paint: {
                        // Increase weight as diameter breast height increases
                        'heatmap-weight': [
                            'interpolate',
                            ['linear'],
                            ['get', 'weight'],
                            0, 0,
                            1, 1
                        ],
                        // Increase intensity as zoom level increases
                        'heatmap-intensity': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            11, 1,
                            17, 3
                        ],
                        // Use sequential color palette
                        'heatmap-color': [
                            'interpolate',
                            ['linear'],
                            ['heatmap-density'],
                            0, 'rgba(59, 130, 246, 0)',    // blue transparent
                            0.2, 'rgba(6, 182, 212, 1)',   // cyan
                            0.4, 'rgba(16, 185, 129, 1)',  // green
                            0.6, 'rgba(245, 158, 11, 1)',  // amber
                            0.8, 'rgba(239, 68, 68, 1)',   // red
                            1, 'rgba(220, 38, 38, 1)'      // dark red
                        ],
                        // Adjust radius by zoom level
                        'heatmap-radius': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            11, 15,
                            17, 40
                        ],
                        // Transition from heatmap to circle layer by zoom level
                        'heatmap-opacity': [
                            'interpolate',
                            ['linear'],
                            ['zoom'],
                            14, 1,
                            17, 0.5
                        ]
                    }
                });

                // Fit bounds
                let bounds = new mapboxgl.LngLatBounds();
                points.forEach(p => {
                    bounds.extend([p[1], p[0]]); // [lng, lat]
                });
                self.map.fitBounds(bounds, { padding: 50 });

                console.log('[Heatmap] Rendered', points.length, 'points via MapBox GL');
            } else {
                console.log('[Heatmap] No data points, showing empty map');
            }
        });
    }
};
