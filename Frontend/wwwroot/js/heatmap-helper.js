// Heatmap Helper - renders MapBox heatmap from analytics data
window.HeatmapHelper = {
    map: null,

    render: function (containerId, centerLat, centerLng, points) {
        const container = document.getElementById(containerId);
        if (!container) {
            console.warn('[Heatmap] Container not found:', containerId);
            return;
        }

        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        this.map = new mapboxgl.Map({
            container: containerId,
            style: 'mapbox://styles/mapbox/dark-v11', 
            center: [centerLng, centerLat],
            zoom: 14
        });

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
                const geojson = {
                    type: 'FeatureCollection',
                    features: points.map(p => ({
                        type: 'Feature',
                        geometry: { type: 'Point', coordinates: [p[1], p[0]] },
                        properties: { weight: p.length > 2 ? p[2] : 1 }
                    }))
                };

                self.map.addSource('heatmap-data', { type: 'geojson', data: geojson });

                self.map.addLayer({
                    id: 'heatmap-layer',
                    type: 'heatmap',
                    source: 'heatmap-data',
                    maxzoom: 17,
                    paint: {
                        'heatmap-weight': ['interpolate', ['linear'], ['get', 'weight'], 0, 0, 1, 1],
                        'heatmap-intensity': ['interpolate', ['linear'], ['zoom'], 11, 1, 17, 3],
                        'heatmap-color': [
                            'interpolate',
                            ['linear'],
                            ['heatmap-density'],
                            0, 'rgba(59, 130, 246, 0)',
                            0.2, 'rgba(6, 182, 212, 1)',
                            0.4, 'rgba(16, 185, 129, 1)',
                            0.6, 'rgba(245, 158, 11, 1)',
                            0.8, 'rgba(239, 68, 68, 1)',
                            1, 'rgba(220, 38, 38, 1)'
                        ],
                        'heatmap-radius': ['interpolate', ['linear'], ['zoom'], 11, 15, 17, 40],
                        'heatmap-opacity': ['interpolate', ['linear'], ['zoom'], 14, 1, 17, 0.5]
                    }
                });

                let bounds = new mapboxgl.LngLatBounds();
                points.forEach(p => bounds.extend([p[1], p[0]]));
                self.map.fitBounds(bounds, { padding: 50 });
            }
        });
    }
};
