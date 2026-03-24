// MapBox GL JS Helper cho FoodStreet
// Chú ý: Đang sử dụng Public Token demo của MapBox. Hãy thay bằng token thật của bạn khi lên production.
mapboxgl.accessToken = 'YOUR_MAPBOX_TOKEN';

window.AppMapHelper = {
    map: null,
    userMarker: null,
    poiMarkers: [],
    geofenceCircles: [],

    // Initialize map
    init: function (elementId, lat, lng, zoom) {
        if (this.map) {
            this.map.remove();
        }

        this.map = new mapboxgl.Map({
            container: elementId,
            style: 'mapbox://styles/mapbox/streets-v12', // style chuẩn
            center: [lng, lat], // MapBox dùng [lng, lat]
            zoom: zoom - 1 // Zoom MapBox thường lớn hơn Leaflet 1 cấp
        });

        this.map.addControl(new mapboxgl.NavigationControl());

        console.log('[MapBox] Initialized');
        return true;
    },

    // Update user location
    setUserLocation: function (lat, lng, accuracy) {
        if (!this.map) return;

        // Custom marker element
        if (!this.userMarker) {
            const el = document.createElement('div');
            el.className = 'user-marker';
            el.innerHTML = '<div class="user-marker-inner" style="font-size:32px; filter:drop-shadow(0 2px 4px rgba(0,0,0,0.3))">📍</div>';
            
            this.userMarker = new mapboxgl.Marker(el)
                .setLngLat([lng, lat])
                .setPopup(new mapboxgl.Popup({ offset: 25 }).setHTML('📍 Vị trí của bạn'))
                .addTo(this.map);
        } else {
            this.userMarker.setLngLat([lng, lat]);
        }

        // accuracy circle (dùng source/layer của MapBox)
        const radiusInMeters = accuracy || 50;
        
        // Cần add source sau khi map load, nhưng simplify logic bằng marker nếu phức tạp
        // Để giữ tính ổn định, accuracy circle có thể được vẽ bằng turf.js. Ở đây ta center map thôi.
        this.map.flyTo({ center: [lng, lat], zoom: this.map.getZoom() });
        console.log('[MapBox] User location updated:', lat, lng);
    },

    // Add POIs to map
    setPois: function (pois) {
        if (!this.map) return;

        // Clear existing markers
        this.poiMarkers.forEach(m => m.remove());
        this.poiMarkers = [];

        pois.forEach(poi => {
            const el = document.createElement('div');
            el.className = 'poi-marker';
            el.innerHTML = '<div class="poi-marker-inner" style="font-size:28px">🍜</div>';

            const popup = new mapboxgl.Popup({ offset: 25 })
                .setHTML(`<strong>${poi.name}</strong><br>${poi.description || ''}<br><em>${poi.distance.toFixed(0)}m</em>`);

            const marker = new mapboxgl.Marker(el)
                .setLngLat([poi.longitude, poi.latitude])
                .setPopup(popup)
                .addTo(this.map);

            this.poiMarkers.push(marker);
        });

        console.log('[MapBox] POIs updated:', pois.length);
    },

    // Center map
    centerOn: function (lat, lng, zoom) {
        if (this.map) {
            this.map.flyTo({ center: [lng, lat], zoom: zoom || this.map.getZoom() });
        }
    },

    // Fit bounds to show all markers
    fitBounds: function () {
        if (!this.map) return;

        let bounds = new mapboxgl.LngLatBounds();
        let hasPoints = false;

        this.poiMarkers.forEach(m => {
            bounds.extend(m.getLngLat());
            hasPoints = true;
        });

        if (this.userMarker) {
            bounds.extend(this.userMarker.getLngLat());
            hasPoints = true;
        }

        if (hasPoints) {
            this.map.fitBounds(bounds, { padding: 50 });
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

        this.map = new mapboxgl.Map({
            container: elementId,
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [lng, lat],
            zoom: zoom - 1
        });

        const el = document.createElement('div');
        el.className = 'picker-marker';
        el.innerHTML = '<div style="font-size: 32px; filter: drop-shadow(0 2px 4px rgba(0,0,0,0.3)); text-align: center;">📍</div>';

        const marker = new mapboxgl.Marker(el, { draggable: true })
            .setLngLat([lng, lat])
            .addTo(this.map);

        // Update when dragged
        marker.on('dragend', function () {
            const lngLat = marker.getLngLat();
            dotNetRef.invokeMethodAsync('UpdateCoordinatesFromMap', lngLat.lat, lngLat.lng);
        });

        // Update when map clicked
        this.map.on('click', function(e) {
            marker.setLngLat(e.lngLat);
            dotNetRef.invokeMethodAsync('UpdateCoordinatesFromMap', e.lngLat.lat, e.lngLat.lng);
        });

        // Fix map size inside modal
        setTimeout(() => { if (this.map) this.map.resize(); }, 100);
        setTimeout(() => { if (this.map) this.map.resize(); }, 500);

        console.log('[MapBox] Picker initialized');
        return true;
    },
    
    // Update picker marker position (when user types in input manually)
    updatePickerMarker: function(lat, lng) {
        if (this.map) {
            // Lấy marker đầu tiên làm picker marker
            const marker = this.map._markers ? this.map._markers[0] : null;
            if (marker) {
                marker.setLngLat([lng, lat]);
            }
            this.map.flyTo({ center: [lng, lat], zoom: this.map.getZoom() });
        }
    },

    // Search address using MapBox Geocoding API
    searchAddress: async function(query) {
        try {
            // Thêm country=vn để ưu tiên Việt Nam và lấy tối đa 5 kết quả
            const response = await fetch(`https://api.mapbox.com/geocoding/v5/mapbox.places/${encodeURIComponent(query)}.json?access_token=${mapboxgl.accessToken}&country=vn&limit=5`);
            const data = await response.json();
            if (data.features && data.features.length > 0) {
                return data.features.map(f => ({
                    lat: f.center[1],
                    lng: f.center[0],
                    text: f.text || '',
                    place_name: f.place_name || ''
                }));
            }
        } catch (e) {
            console.error('[MapBox] Search error', e);
        }
        return [];
    }
};

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
