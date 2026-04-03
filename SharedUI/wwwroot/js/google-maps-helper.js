// Google Maps JS Helper for FoodStreet
// Keeps the same AppMapHelper API so existing Razor pages need minimal changes.

(function () {
    function toLatLng(lat, lng) {
        return { lat: Number(lat), lng: Number(lng) };
    }

    function escapeHtml(value) {
        return String(value ?? "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function getMarkerPosition(marker) {
        const position = marker?.getPosition?.();
        return position ? { lat: position.lat(), lng: position.lng() } : null;
    }

    function createUserIcon() {
        return {
            path: google.maps.SymbolPath.CIRCLE,
            fillColor: "#4285F4",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: 3,
            scale: 8
        };
    }

    function createPoiIcon(isFocused) {
        return {
            path: google.maps.SymbolPath.CIRCLE,
            fillColor: isFocused ? "#ff6b35" : "#EA4335",
            fillOpacity: 1,
            strokeColor: "#ffffff",
            strokeWeight: isFocused ? 3 : 2,
            scale: isFocused ? 12 : 9
        };
    }

    window.AppMapHelper = {
        map: null,
        mapElementId: null,
        userMarker: null,
        poiMarkers: [],
        poiMarkersById: {},
        focusedPoiId: null,
        geofenceCircles: [],
        infoWindow: null,
        geocoder: null,
        placesLibPromise: null,
        autocompleteSessionToken: null,
        autocompleteSuggestionCache: {},
        libLoaded: false,
        scriptLoadingPromise: null,
        pendingScriptReject: null,
        authFailureDetected: false,
        lastError: "",

        resolveApiKey: function (apiKey) {
            return apiKey || window.FoodStreetMapConfig?.googleMapsApiKey || "";
        },

        setLastError: function (message) {
            this.lastError = message || "";
        },

        clearLastError: function () {
            this.lastError = "";
        },

        getLastError: function (fallbackMessage) {
            return this.lastError || fallbackMessage || "";
        },

        hasConfiguredApiKey: function () {
            return !!this.resolveApiKey();
        },

        ensureServices: function () {
            if (!window.google?.maps) {
                return;
            }

            if (!this.infoWindow) {
                this.infoWindow = new google.maps.InfoWindow();
            }

            if (!this.geocoder) {
                this.geocoder = new google.maps.Geocoder();
            }
        },

        ensurePlacesLibrary: async function () {
            if (!window.google?.maps?.importLibrary) {
                throw new Error("Google Maps Places library is unavailable.");
            }

            if (!this.placesLibPromise) {
                this.placesLibPromise = google.maps.importLibrary("places");
            }

            return await this.placesLibPromise;
        },

        ensureAutocompleteSession: async function () {
            const places = await this.ensurePlacesLibrary();
            if (!this.autocompleteSessionToken) {
                this.autocompleteSessionToken = new places.AutocompleteSessionToken();
            }

            return this.autocompleteSessionToken;
        },

        resetAutocompleteSession: function () {
            this.autocompleteSessionToken = null;
            this.autocompleteSuggestionCache = {};
        },

        resetScriptState: function (removeScriptTag) {
            this.scriptLoadingPromise = null;
            this.pendingScriptReject = null;

            if (removeScriptTag) {
                document.getElementById("foodstreet-google-maps-script")?.remove();
            }
        },

        notifyAuthFailure: function () {
            const message = "Google Maps từ chối browser key hiện tại. Hãy kiểm tra API restrictions, referrer/domain và billing.";
            this.authFailureDetected = true;
            this.setLastError(message);

            const reject = this.pendingScriptReject;
            this.resetScriptState(true);
            if (typeof reject === "function") {
                reject(new Error(message));
            }
        },

        loadScript: function (apiKey) {
            const resolvedApiKey = this.resolveApiKey(apiKey);
            if (!resolvedApiKey) {
                this.setLastError("Chưa cấu hình Google Maps browser key trong runtime-config.js.");
                return Promise.reject(new Error(this.lastError));
            }

            if (this.authFailureDetected) {
                return Promise.reject(new Error(this.getLastError("Google Maps từ chối browser key hiện tại.")));
            }

            if (window.google?.maps) {
                this.libLoaded = true;
                this.clearLastError();
                this.ensureServices();
                return Promise.resolve();
            }

            if (this.scriptLoadingPromise) {
                return this.scriptLoadingPromise;
            }

            this.authFailureDetected = false;
            this.scriptLoadingPromise = new Promise((resolve, reject) => {
                this.pendingScriptReject = reject;
                const callbackName = "__foodStreetGoogleMapsLoaded";
                const helper = this;
                const existingScript = document.getElementById("foodstreet-google-maps-script");
                if (existingScript) {
                    existingScript.remove();
                }

                const finalizeFailure = function (message, error) {
                    helper.setLastError(message);
                    helper.resetScriptState(true);
                    delete window[callbackName];
                    reject(error ?? new Error(message));
                };

                window[callbackName] = () => {
                    if (helper.authFailureDetected) {
                        finalizeFailure(helper.getLastError("Google Maps từ chối browser key hiện tại."));
                        return;
                    }

                    helper.libLoaded = true;
                    helper.clearLastError();
                    helper.ensureServices();
                    helper.resetScriptState(false);
                    resolve();
                    delete window[callbackName];
                };

                const script = document.createElement("script");
                script.id = "foodstreet-google-maps-script";
                script.async = true;
                script.defer = true;
                script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(resolvedApiKey)}&callback=${callbackName}&loading=async&libraries=places&v=weekly`;
                script.onerror = () => finalizeFailure("Không thể tải Google Maps JavaScript API. Hãy kiểm tra key, billing hoặc network.");
                document.head.appendChild(script);
            });

            return this.scriptLoadingPromise;
        },

        createMap: function (elementId, lat, lng, zoom) {
            const element = document.getElementById(elementId);
            if (!element) {
                throw new Error(`Không tìm thấy phần tử map '${elementId}'.`);
            }

            this.mapElementId = elementId;
            this.map = new google.maps.Map(element, {
                center: toLatLng(lat, lng),
                zoom: zoom || 15,
                mapTypeControl: false,
                streetViewControl: false,
                fullscreenControl: false,
                gestureHandling: "greedy"
            });

            this.ensureServices();
        },

        init: async function (elementId, lat, lng, zoom, apiKey) {
            if (this.map) {
                this.destroy();
            }

            try {
                await this.loadScript(apiKey);
                this.createMap(elementId, lat, lng, zoom);
                console.log("[GoogleMaps] Initialized");
                return true;
            } catch (error) {
                this.setLastError(error?.message || "Không thể khởi tạo Google Maps.");
                console.error("[GoogleMaps] Init error:", error);
                return false;
            }
        },

        setUserLocation: function (lat, lng) {
            if (!this.map || !window.google?.maps) {
                return;
            }

            const position = toLatLng(lat, lng);

            if (!this.userMarker) {
                this.userMarker = new google.maps.Marker({
                    position,
                    map: this.map,
                    title: "Vị trí của bạn",
                    icon: createUserIcon(),
                    zIndex: 1000
                });
            } else {
                this.userMarker.setPosition(position);
                this.userMarker.setMap(this.map);
            }

            this.map.panTo(position);
        },

        setPois: function (pois) {
            if (!this.map || !window.google?.maps) {
                return;
            }

            this.poiMarkers.forEach(marker => marker.setMap(null));
            this.poiMarkers = [];
            this.poiMarkersById = {};

            const bounds = new google.maps.LatLngBounds();

            if (this.userMarker) {
                const userPosition = getMarkerPosition(this.userMarker);
                if (userPosition) {
                    bounds.extend(userPosition);
                }
            }

            pois.forEach(poi => {
                const position = toLatLng(poi.latitude, poi.longitude);
                const distanceText = poi.distance
                    ? `<br><em style="color:#10b981; font-size:12px;">Cách bạn ${Number(poi.distance).toFixed(0)}m</em>`
                    : "";

                const marker = new google.maps.Marker({
                    position,
                    map: this.map,
                    title: poi.name,
                    icon: createPoiIcon(String(poi.id) === this.focusedPoiId)
                });

                marker.addListener("click", () => {
                    this.infoWindow.setContent(`
                        <div style="font-family: Roboto, Arial, sans-serif; padding: 4px; max-width: 260px;">
                            <strong style="font-size: 16px; color: #202124;">${escapeHtml(poi.name)}</strong><br>
                            <span style="font-size: 13px; color: #5f6368;">${escapeHtml(poi.description || "")}</span>
                            ${distanceText}
                        </div>
                    `);
                    this.infoWindow.open({ map: this.map, anchor: marker });
                });

                this.poiMarkers.push(marker);
                this.poiMarkersById[String(poi.id)] = marker;
                bounds.extend(position);
            });

            if (!bounds.isEmpty()) {
                this.map.fitBounds(bounds, 60);
            }
        },

        focusPoi: function (poiId, zoom) {
            if (!this.map) {
                return false;
            }

            const nextFocusedPoiId = String(poiId);
            const marker = this.poiMarkersById[nextFocusedPoiId];
            if (!marker) {
                return false;
            }

            this.focusedPoiId = nextFocusedPoiId;
            Object.entries(this.poiMarkersById).forEach(([id, currentMarker]) => {
                currentMarker.setIcon(createPoiIcon(id === this.focusedPoiId));
                currentMarker.setZIndex(id === this.focusedPoiId ? 2000 : undefined);
                currentMarker.setAnimation(null);
            });

            const position = marker.getPosition?.();
            if (!position) {
                return false;
            }

            this.map.setCenter({ lat: position.lat(), lng: position.lng() });
            if (zoom) {
                this.map.setZoom(zoom);
            }

            marker.setAnimation(google.maps.Animation.BOUNCE);
            window.setTimeout(() => marker.setAnimation(null), 1400);
            google.maps.event.trigger(marker, "click");
            return true;
        },

        fitUserAndPoi: function (poiId, padding) {
            if (!this.map) {
                return false;
            }

            const marker = this.poiMarkersById[String(poiId)];
            if (!marker) {
                return false;
            }

            const markerPosition = marker.getPosition?.();
            if (!markerPosition) {
                return false;
            }

            const bounds = new google.maps.LatLngBounds();
            bounds.extend({ lat: markerPosition.lat(), lng: markerPosition.lng() });

            const userPosition = getMarkerPosition(this.userMarker);
            if (userPosition) {
                bounds.extend(userPosition);
            }

            this.map.fitBounds(bounds, padding || 90);
            return true;
        },

        centerOn: function (lat, lng, zoom) {
            if (!this.map) {
                return;
            }

            this.map.setCenter(toLatLng(lat, lng));
            if (zoom) {
                this.map.setZoom(zoom);
            }
        },

        fitBounds: function () {
            if (!this.map || !window.google?.maps) {
                return;
            }

            const bounds = new google.maps.LatLngBounds();
            let hasPoints = false;

            this.poiMarkers.forEach(marker => {
                const position = getMarkerPosition(marker);
                if (position) {
                    bounds.extend(position);
                    hasPoints = true;
                }
            });

            const userPosition = getMarkerPosition(this.userMarker);
            if (userPosition) {
                bounds.extend(userPosition);
                hasPoints = true;
            }

            if (hasPoints) {
                this.map.fitBounds(bounds, 60);
            }
        },

        resize: function () {
            if (!this.map || !window.google?.maps) {
                return;
            }

            const center = this.map.getCenter();
            google.maps.event.trigger(this.map, "resize");
            if (center) {
                this.map.setCenter(center);
            }
        },

        destroy: function () {
            if (this.poiMarkers.length > 0) {
                this.poiMarkers.forEach(marker => marker.setMap(null));
                this.poiMarkers = [];
                this.poiMarkersById = {};
                this.focusedPoiId = null;
            }

            if (this.geofenceCircles.length > 0) {
                this.geofenceCircles.forEach(circle => circle.setMap(null));
                this.geofenceCircles = [];
            }

            if (this.userMarker) {
                this.userMarker.setMap(null);
                this.userMarker = null;
            }

            if (this.infoWindow) {
                this.infoWindow.close();
            }

            if (this.map) {
                google.maps.event.clearInstanceListeners(this.map);
                this.map = null;
            }

            if (this.mapElementId) {
                const element = document.getElementById(this.mapElementId);
                if (element) {
                    element.innerHTML = "";
                }
                this.mapElementId = null;
            }

            this.resetAutocompleteSession();
        },

        initPicker: async function (elementId, lat, lng, zoom, dotNetRef, apiKey) {
            if (this.map) {
                this.destroy();
            }

            try {
                await this.loadScript(apiKey);
                this.createMap(elementId, lat, lng, zoom);

                const startPosition = toLatLng(lat, lng);
                this.userMarker = new google.maps.Marker({
                    position: startPosition,
                    map: this.map,
                    draggable: true,
                    title: "POI position"
                });

                this.userMarker.addListener("dragend", () => {
                    const position = getMarkerPosition(this.userMarker);
                    if (position) {
                        dotNetRef.invokeMethodAsync("UpdateCoordinatesFromMap", position.lat, position.lng);
                    }
                });

                this.map.addListener("click", event => {
                    const position = {
                        lat: event.latLng.lat(),
                        lng: event.latLng.lng()
                    };

                    this.userMarker.setPosition(position);
                    dotNetRef.invokeMethodAsync("UpdateCoordinatesFromMap", position.lat, position.lng);
                });

                console.log("[GoogleMaps] Picker initialized");
                return true;
            } catch (error) {
                this.setLastError(error?.message || "Không thể khởi tạo Google Maps picker.");
                console.error("[GoogleMaps] Picker init error:", error);
                return false;
            }
        },

        updatePickerMarker: function (lat, lng) {
            if (!this.map || !this.userMarker) {
                return;
            }

            const position = toLatLng(lat, lng);
            this.userMarker.setPosition(position);
            this.map.panTo(position);
        },

        searchAddress: async function (query) {
            if (!query) {
                return [];
            }

            try {
                await this.loadScript();
                const places = await this.ensurePlacesLibrary();
                const sessionToken = await this.ensureAutocompleteSession();

                const { suggestions } = await places.AutocompleteSuggestion.fetchAutocompleteSuggestions({
                    input: query,
                    sessionToken,
                    includedRegionCodes: ["vn"],
                    language: "vi",
                    origin: { lat: 10.762622, lng: 106.660172 }
                });

                this.autocompleteSuggestionCache = {};

                const placeResults = (suggestions || [])
                    .map(suggestion => suggestion.placePrediction)
                    .filter(Boolean)
                    .slice(0, 5)
                    .map(prediction => {
                        const placeId = prediction.placeId;
                        if (placeId) {
                            this.autocompleteSuggestionCache[placeId] = prediction;
                        }

                        return {
                            lat: 0,
                            lng: 0,
                            placeId,
                            text: prediction.text?.toString?.() || prediction.mainText?.toString?.() || "",
                            place_name: prediction.secondaryText?.toString?.() || prediction.text?.toString?.() || ""
                        };
                    })
                    .filter(result => result.text);

                if (placeResults.length > 0) {
                    return placeResults;
                }

                return await this.searchAddressFallback(query);
            } catch (error) {
                console.error("[GoogleMaps] Search error:", error);
                return await this.searchAddressFallback(query);
            }
        },

        searchAddressFallback: async function (query) {
            try {
                this.ensureServices();
                const response = await this.geocoder.geocode({
                    address: query,
                    componentRestrictions: { country: "VN" }
                });

                return (response.results || []).slice(0, 5).map(result => {
                    const location = result.geometry.location;
                    const text = result.address_components?.[0]?.long_name
                        || result.formatted_address.split(",")[0]
                        || result.formatted_address;

                    return {
                        lat: location.lat(),
                        lng: location.lng(),
                        placeId: "",
                        text,
                        place_name: result.formatted_address
                    };
                });
            } catch (fallbackError) {
                console.error("[GoogleMaps] Fallback search error:", fallbackError);
                return [];
            }
        },

        resolveSelectedPlace: async function (placeId) {
            if (!placeId) {
                return null;
            }

            try {
                await this.loadScript();
                await this.ensurePlacesLibrary();

                const prediction = this.autocompleteSuggestionCache[placeId];
                if (!prediction) {
                    return null;
                }

                const place = prediction.toPlace();
                await place.fetchFields({
                    fields: ["displayName", "formattedAddress", "location"]
                });

                const result = {
                    lat: place.location?.lat() ?? 0,
                    lng: place.location?.lng() ?? 0,
                    placeId,
                    text: place.displayName || prediction.text?.toString?.() || "",
                    place_name: place.formattedAddress || prediction.secondaryText?.toString?.() || ""
                };

                this.resetAutocompleteSession();
                return result;
            } catch (error) {
                console.error("[GoogleMaps] Resolve selected place error:", error);
                return null;
            }
        },

        buildStaticMapUrl: function (lat, lng, zoom, width, height) {
            const resolvedApiKey = this.resolveApiKey();
            if (!resolvedApiKey) {
                return "";
            }

            const latitude = Number(lat).toString();
            const longitude = Number(lng).toString();
            const marker = encodeURIComponent(`color:red|label:P|${latitude},${longitude}`);

            return `https://maps.googleapis.com/maps/api/staticmap?center=${latitude},${longitude}&zoom=${zoom || 16}&size=${width || 800}x${height || 320}&scale=2&maptype=roadmap&markers=${marker}&key=${encodeURIComponent(resolvedApiKey)}`;
        }
    };

    window.gm_authFailure = function () {
        window.AppMapHelper?.notifyAuthFailure?.();
    };

    window.MapInterop = {
        requestLocation: function (dotNetRef) {
            if (!navigator.geolocation) {
                dotNetRef.invokeMethodAsync("OnLocationError", "Trình duyệt không hỗ trợ GPS");
                return;
            }

            navigator.geolocation.getCurrentPosition(
                function (pos) {
                    dotNetRef.invokeMethodAsync(
                        "OnLocationReceived",
                        pos.coords.latitude,
                        pos.coords.longitude,
                        pos.coords.accuracy || 50
                    );
                },
                function (err) {
                    dotNetRef.invokeMethodAsync("OnLocationError", err.message || "Không thể lấy vị trí");
                },
                { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 }
            );
        }
    };
})();
