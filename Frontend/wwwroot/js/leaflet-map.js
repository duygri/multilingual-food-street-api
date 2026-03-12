// Leaflet Map Helper with Nearest POI Highlighting
window.LeafletMap = {
    map: null,
    userMarker: null,
    accuracyCircle: null,
    poiMarkers: [],
    geofenceCircles: [],
    nearestPoiId: null,

    // Custom icons
    userIcon: null,
    poiIcon: null,
    nearestPoiIcon: null,

    // Initialize map
    init: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        // Custom icons
        this.userIcon = L.divIcon({
            className: 'user-marker',
            html: '<div class="user-marker-inner">📍</div>',
            iconSize: [40, 40],
            iconAnchor: [20, 40]
        });

        this.poiIcon = L.divIcon({
            className: 'poi-marker',
            html: '<div class="poi-marker-inner">🍜</div>',
            iconSize: [36, 36],
            iconAnchor: [18, 36]
        });

        // Icon đặc biệt cho POI gần nhất
        this.nearestPoiIcon = L.divIcon({
            className: 'poi-marker nearest',
            html: '<div class="poi-marker-inner nearest-glow">⭐</div>',
            iconSize: [44, 44],
            iconAnchor: [22, 44]
        });

        console.log('[Map] Initialized');
        return true;
    },

    // Update user location
    setUserLocation: function (lat, lng, accuracy) {
        if (!this.map) return;

        if (this.userMarker) {
            this.userMarker.setLatLng([lat, lng]);
        } else {
            this.userMarker = L.marker([lat, lng], { icon: this.userIcon })
                .addTo(this.map)
                .bindPopup('📍 Vị trí của bạn');
        }

        // Accuracy circle
        if (this.accuracyCircle) {
            this.accuracyCircle.setLatLng([lat, lng]).setRadius(accuracy);
        } else if (accuracy) {
            this.accuracyCircle = L.circle([lat, lng], {
                radius: accuracy,
                color: '#0ea5e9',
                fillColor: '#0ea5e9',
                fillOpacity: 0.1,
                weight: 1
            }).addTo(this.map);
        }

        this.map.setView([lat, lng], this.map.getZoom());
        console.log('[Map] User location updated:', lat, lng);
    },

    // Add POIs to map with nearest highlight
    setPois: function (pois) {
        if (!this.map) return;

        // Clear existing markers
        this.poiMarkers.forEach(m => this.map.removeLayer(m));
        this.geofenceCircles.forEach(c => this.map.removeLayer(c));
        this.poiMarkers = [];
        this.geofenceCircles = [];

        // Tìm POI gần nhất
        let nearestPoi = null;
        let minDist = Infinity;
        pois.forEach(poi => {
            if (poi.distance < minDist) {
                minDist = poi.distance;
                nearestPoi = poi;
            }
        });

        pois.forEach(poi => {
            const isNearest = nearestPoi && poi.id === nearestPoi.id;
            const icon = isNearest ? this.nearestPoiIcon : this.poiIcon;

            // Add marker
            const marker = L.marker([poi.latitude, poi.longitude], { 
                icon: icon,
                zIndexOffset: isNearest ? 1000 : 0  // POI gần nhất hiện trên cùng
            })
                .addTo(this.map)
                .bindPopup(`
                    <div style="min-width:180px">
                        <strong>${isNearest ? '⭐ ' : ''}${poi.name}</strong>
                        <br>${poi.description || ''}
                        <br><em style="color:#10b981;font-weight:600">${poi.distance.toFixed(0)}m</em>
                        ${isNearest ? '<br><span style="color:#ff6b35;font-size:12px;font-weight:600">📍 Gần nhất</span>' : ''}
                    </div>
                `);
            
            // Tự động mở popup cho POI gần nhất
            if (isNearest && poi.isInGeofence) {
                marker.openPopup();
            }

            this.poiMarkers.push(marker);

            // Add geofence circle
            const circle = L.circle([poi.latitude, poi.longitude], {
                radius: poi.radius || 50,
                color: isNearest ? '#ff6b35' : (poi.isInGeofence ? '#10b981' : '#f59e0b'),
                fillColor: isNearest ? '#ff6b35' : (poi.isInGeofence ? '#10b981' : '#f59e0b'),
                fillOpacity: isNearest ? 0.25 : 0.15,
                weight: isNearest ? 3 : 2,
                dashArray: isNearest ? null : '5, 5'
            }).addTo(this.map);
            this.geofenceCircles.push(circle);
        });

        this.nearestPoiId = nearestPoi?.id;
        console.log('[Map] POIs updated:', pois.length, 'nearest:', nearestPoi?.name);
    },

    // Center map on location
    centerOn: function (lat, lng, zoom) {
        if (this.map) {
            this.map.setView([lat, lng], zoom || this.map.getZoom());
        }
    },

    // Fit bounds to show all markers
    fitBounds: function () {
        if (!this.map) return;

        const allMarkers = [...this.poiMarkers];
        if (this.userMarker) allMarkers.push(this.userMarker);

        if (allMarkers.length > 0) {
            const group = L.featureGroup(allMarkers);
            this.map.fitBounds(group.getBounds().pad(0.1));
        }
    },

    // Destroy map
    destroy: function () {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.userMarker = null;
            this.accuracyCircle = null;
            this.poiMarkers = [];
            this.geofenceCircles = [];
            this.nearestPoiId = null;
        }
    }
};
