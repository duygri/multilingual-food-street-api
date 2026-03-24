// Geofence Test Map Helper - Web-only tool for testing POI proximity
// Uses Mapbox GL JS to visualize POIs, geofence circles, and simulated user position

window.GeofenceTestMap = {
    map: null,
    testMarker: null,
    poiMarkers: [],
    geofenceCircles: [],
    dotNetRef: null,

    // Initialize the test map
    init: function (elementId, lat, lng, zoom, dotNetRef) {
        this.dotNetRef = dotNetRef;

        if (this.map) {
            this.map.remove();
        }

        mapboxgl.accessToken = 'YOUR_MAPBOX_TOKEN';

        this.map = new mapboxgl.Map({
            container: elementId,
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [lng, lat],
            zoom: zoom || 15
        });

        this.map.addControl(new mapboxgl.NavigationControl());

        // Add test position marker (draggable)
        const el = document.createElement('div');
        el.innerHTML = '<div style="font-size:36px; filter:drop-shadow(0 3px 6px rgba(0,0,0,0.4)); cursor:grab; user-select:none;">🧑</div>';
        el.style.cursor = 'grab';

        this.testMarker = new mapboxgl.Marker(el, { draggable: true })
            .setLngLat([lng, lat])
            .setPopup(new mapboxgl.Popup({ offset: 25 }).setHTML('<strong>📍 Vị trí Test</strong><br>Kéo để di chuyển'))
            .addTo(this.map);

        // When marker is dragged, notify Blazor
        this.testMarker.on('dragend', () => {
            const lngLat = this.testMarker.getLngLat();
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnTestPositionChanged', lngLat.lat, lngLat.lng);
            }
        });

        // When map is clicked, move marker there
        this.map.on('click', (e) => {
            this.testMarker.setLngLat(e.lngLat);
            if (this.dotNetRef) {
                this.dotNetRef.invokeMethodAsync('OnTestPositionChanged', e.lngLat.lat, e.lngLat.lng);
            }
        });

        // Wait for map style to load before adding sources
        this.map.on('load', () => {
            console.log('[GeofenceTest] Map loaded and ready');
        });

        console.log('[GeofenceTest] Map initialized at', lat, lng);
        return true;
    },

    // Update test marker position (from input fields)
    updateTestPosition: function (lat, lng) {
        if (!this.map || !this.testMarker) return;
        this.testMarker.setLngLat([lng, lat]);
        this.map.flyTo({ center: [lng, lat], zoom: this.map.getZoom() });
    },

    // Display POIs with geofence radius circles
    setPoisWithGeofence: function (pois, testLat, testLng) {
        if (!this.map) return;

        // Clear existing POI markers
        this.poiMarkers.forEach(m => m.remove());
        this.poiMarkers = [];

        // Remove existing geofence circle layers/sources
        this.geofenceCircles.forEach(id => {
            if (this.map.getLayer(id + '-fill')) this.map.removeLayer(id + '-fill');
            if (this.map.getLayer(id + '-outline')) this.map.removeLayer(id + '-outline');
            if (this.map.getSource(id)) this.map.removeSource(id);
        });
        this.geofenceCircles = [];

        pois.forEach((poi, index) => {
            const isInside = poi.isInGeofence;
            const sourceId = 'geofence-' + index;

            // Create geofence circle using GeoJSON circle approximation
            const circleGeoJSON = this._createCircleGeoJSON(poi.latitude, poi.longitude, poi.radius);

            // Add geofence circle source and layers
            const addCircle = () => {
                if (this.map.getSource(sourceId)) return; // already added

                this.map.addSource(sourceId, {
                    type: 'geojson',
                    data: circleGeoJSON
                });

                // Fill layer
                this.map.addLayer({
                    id: sourceId + '-fill',
                    type: 'fill',
                    source: sourceId,
                    paint: {
                        'fill-color': isInside ? '#22c55e' : '#3b82f6',
                        'fill-opacity': isInside ? 0.2 : 0.08
                    }
                });

                // Outline layer
                this.map.addLayer({
                    id: sourceId + '-outline',
                    type: 'line',
                    source: sourceId,
                    paint: {
                        'line-color': isInside ? '#16a34a' : '#2563eb',
                        'line-width': isInside ? 2.5 : 1.5,
                        'line-dasharray': isInside ? [1, 0] : [4, 3]
                    }
                });

                this.geofenceCircles.push(sourceId);
            };

            if (this.map.isStyleLoaded()) {
                addCircle();
            } else {
                this.map.on('load', addCircle);
            }

            // Create POI marker
            const markerEl = document.createElement('div');
            markerEl.innerHTML = `<div style="
                font-size: 24px;
                background: ${isInside ? '#dcfce7' : '#fff'};
                border: 2px solid ${isInside ? '#16a34a' : '#e2e8f0'};
                border-radius: 50%;
                width: 40px;
                height: 40px;
                display: flex;
                align-items: center;
                justify-content: center;
                box-shadow: 0 2px 8px rgba(0,0,0,0.15);
                ${isInside ? 'animation: geoPulse 1.5s ease-in-out infinite;' : ''}
            ">🍜</div>`;

            const distText = poi.distance >= 1000
                ? (poi.distance / 1000).toFixed(1) + ' km'
                : poi.distance.toFixed(0) + ' m';

            const statusText = isInside
                ? '<span style="color:#16a34a; font-weight:700;">✅ TRONG geofence</span>'
                : '<span style="color:#94a3b8;">⚪ Ngoài geofence</span>';

            const popup = new mapboxgl.Popup({ offset: 25, maxWidth: '280px' })
                .setHTML(`
                    <div style="font-family: system-ui, sans-serif;">
                        <strong style="font-size:14px;">${poi.name}</strong><br>
                        <span style="color:#64748b; font-size:12px;">${poi.description || ''}</span><br>
                        <div style="margin-top:6px; padding-top:6px; border-top:1px solid #f1f5f9;">
                            <span style="font-size:12px;">📏 ${distText} | 📐 R=${poi.radius}m</span><br>
                            ${statusText}
                        </div>
                    </div>
                `);

            const marker = new mapboxgl.Marker(markerEl)
                .setLngLat([poi.longitude, poi.latitude])
                .setPopup(popup)
                .addTo(this.map);

            this.poiMarkers.push(marker);
        });

        // Add CSS animation
        if (!document.getElementById('geofence-test-styles')) {
            const style = document.createElement('style');
            style.id = 'geofence-test-styles';
            style.textContent = `
                @keyframes geoPulse {
                    0%, 100% { transform: scale(1); box-shadow: 0 2px 8px rgba(22,163,106,0.15); }
                    50% { transform: scale(1.1); box-shadow: 0 4px 16px rgba(22,163,106,0.3); }
                }
            `;
            document.head.appendChild(style);
        }

        console.log('[GeofenceTest] POIs updated:', pois.length);
    },

    // Fit map to show all POIs + test position
    fitAll: function () {
        if (!this.map) return;

        const bounds = new mapboxgl.LngLatBounds();
        let hasPoints = false;

        this.poiMarkers.forEach(m => {
            bounds.extend(m.getLngLat());
            hasPoints = true;
        });

        if (this.testMarker) {
            bounds.extend(this.testMarker.getLngLat());
            hasPoints = true;
        }

        if (hasPoints) {
            this.map.fitBounds(bounds, { padding: 60, maxZoom: 17 });
        }
    },

    // Create GeoJSON circle polygon (approximation with 64 points)
    _createCircleGeoJSON: function (lat, lng, radiusMeters) {
        const points = 64;
        const coords = [];
        const km = radiusMeters / 1000;
        const distanceX = km / (111.32 * Math.cos(lat * Math.PI / 180));
        const distanceY = km / 110.574;

        for (let i = 0; i < points; i++) {
            const theta = (i / points) * (2 * Math.PI);
            const x = lng + distanceX * Math.cos(theta);
            const y = lat + distanceY * Math.sin(theta);
            coords.push([x, y]);
        }
        coords.push(coords[0]); // close polygon

        return {
            type: 'Feature',
            geometry: {
                type: 'Polygon',
                coordinates: [coords]
            }
        };
    },

    // Destroy
    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.testMarker = null;
            this.poiMarkers = [];
            this.geofenceCircles = [];
            this.dotNetRef = null;
        }
    }
};
