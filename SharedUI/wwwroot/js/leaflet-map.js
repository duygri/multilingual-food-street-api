// Leaflet Map Helper for GPS Test
window.LeafletMap = {
    map: null,
    userMarker: null,
    poiMarkers: [],
    geofenceCircles: [],

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

        // Optional: add accuracy circle
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

    // Add POIs to map
    setPois: function (pois) {
        if (!this.map) return;

        // Clear existing markers
        this.poiMarkers.forEach(m => this.map.removeLayer(m));
        this.geofenceCircles.forEach(c => this.map.removeLayer(c));
        this.poiMarkers = [];
        this.geofenceCircles = [];

        pois.forEach(poi => {
            // Add marker
            const marker = L.marker([poi.latitude, poi.longitude], { icon: this.poiIcon })
                .addTo(this.map)
                .bindPopup(`<strong>${poi.name}</strong><br>${poi.description || ''}<br><em>${poi.distance.toFixed(0)}m</em>`);
            this.poiMarkers.push(marker);

            // Add geofence circle
            const circle = L.circle([poi.latitude, poi.longitude], {
                radius: poi.radius || 50,
                color: poi.isInGeofence ? '#10b981' : '#f59e0b',
                fillColor: poi.isInGeofence ? '#10b981' : '#f59e0b',
                fillOpacity: 0.15,
                weight: 2
            }).addTo(this.map);
            this.geofenceCircles.push(circle);
        });

        console.log('[Map] POIs updated:', pois.length);
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
            this.poiMarkers = [];
            this.geofenceCircles = [];
        }
    },

    // Initialize map for picking location
    initPicker: function (elementId, lat, lng, zoom, dotNetRef) {
        if (this.map) {
            this.map.remove();
        }

        this.map = L.map(elementId).setView([lat, lng], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        // Custom icon
        const pickerIcon = L.divIcon({
            className: 'picker-marker',
            html: '<div style="font-size: 32px; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.3)); text-align: center;">📍</div>',
            iconSize: [40, 40],
            iconAnchor: [20, 40]
        });

        const marker = L.marker([lat, lng], {
            icon: pickerIcon,
            draggable: true
        }).addTo(this.map);

        // Update when dragged
        marker.on('dragend', function (event) {
            const position = marker.getLatLng();
            dotNetRef.invokeMethodAsync('UpdateCoordinatesFromMap', position.lat, position.lng);
        });

        // Update when map clicked
        this.map.on('click', function(e) {
            marker.setLatLng(e.latlng);
            dotNetRef.invokeMethodAsync('UpdateCoordinatesFromMap', e.latlng.lat, e.latlng.lng);
        });

        // Fix map size inside modal (wait for modal animation)
        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 100);
        
        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 500);

        setTimeout(() => {
            if (this.map) this.map.invalidateSize();
        }, 1000);

        console.log('[Map] Picker initialized');
        return true;
    },
    
    // Update picker marker position (when user types in input manually)
    updatePickerMarker: function(lat, lng) {
        if (this.map) {
            this.map.eachLayer(function(layer) {
                if (layer instanceof L.Marker) {
                    layer.setLatLng([lat, lng]);
                }
                if (layer instanceof L.Circle) {
                    layer.setLatLng([lat, lng]);
                }
            });
            this.map.setView([lat, lng], this.map.getZoom());
        }
    },

    // Search address using Nominatim API
    searchAddress: async function(query) {
        try {
            const response = await fetch(`https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=1`);
            const data = await response.json();
            if (data && data.length > 0) {
                return { lat: data[0].lat, lng: data[0].lon, display_name: data[0].display_name };
            }
        } catch (e) {
            console.error('[Map] Search error', e);
        }
        return null;
    }
};

// FIX Bug 2: MapInterop uses DotNetObjectReference (instance method) instead of
// static Assembly invoke — safe for multi-tab and proper garbage collection.
window.MapInterop = {
    requestLocation: function (dotNetRef) {
        if (!navigator.geolocation) {
            dotNetRef.invokeMethodAsync('OnLocationError', 'Trình duyệt không hỗ trợ GPS');
            return;
        }

        navigator.geolocation.getCurrentPosition(
            function (pos) {
                dotNetRef.invokeMethodAsync(
                    'OnLocationReceived',
                    pos.coords.latitude,
                    pos.coords.longitude,
                    pos.coords.accuracy || 50
                );
            },
            function (err) {
                dotNetRef.invokeMethodAsync('OnLocationError', err.message || 'Không thể lấy vị trí');
            },
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
        );
    }
};

