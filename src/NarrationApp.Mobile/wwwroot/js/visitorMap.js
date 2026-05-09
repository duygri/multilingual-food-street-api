window.visitorMap = (() => {
    const instances = new Map();
    const radiusSourceId = "visitor-poi-radius";
    const radiusFillLayerId = "visitor-poi-radius-fill";
    const radiusLineLayerId = "visitor-poi-radius-line";
    const routeSourceId = "visitor-walking-route";
    const routeLayerId = "visitor-walking-route-line";

    function ensureContainer(containerId) {
        const container = document.getElementById(containerId);
        if (!container) {
            return null;
        }

        return container;
    }

    function render(containerId, accessToken, styleUrl, snapshot, dotNetRef) {
        const container = ensureContainer(containerId);
        if (!container) {
            return;
        }

        if (!accessToken || accessToken.startsWith("YOUR_")) {
            renderOfflineFallback(container, snapshot, dotNetRef, "Chưa có Mapbox access token. Đang dùng bản đồ offline từ dữ liệu đã lưu.");
            console.warn("[visitorMap] Mapbox token is a placeholder – skipping render.");
            return;
        }

        if (!window.mapboxgl) {
            renderOfflineFallback(container, snapshot, dotNetRef, "Mapbox chưa tải được. Đang dùng bản đồ offline từ dữ liệu đã lưu.");
            return;
        }

        try {
            renderMapbox(containerId, accessToken, styleUrl, snapshot, dotNetRef);
        } catch (error) {
            console.warn("[visitorMap] Mapbox render failed.", error);
            renderOfflineFallback(container, snapshot, dotNetRef, "Không khởi tạo được Mapbox. Đang dùng bản đồ offline từ dữ liệu đã lưu.");
        }
    }

    function renderMapbox(containerId, accessToken, styleUrl, snapshot, dotNetRef) {
        const container = ensureContainer(containerId);
        if (!container) {
            return;
        }

        mapboxgl.accessToken = accessToken;

        let instance = instances.get(containerId);
        if (instance && (instance.container !== container || !document.contains(instance.container))) {
            clearMapboxInstance(containerId);
            instance = null;
        }

        if (!instance) {
            container.innerHTML = "";
            const map = new mapboxgl.Map({
                container: container,
                style: styleUrl,
                center: [snapshot.centerLng, snapshot.centerLat],
                zoom: snapshot.zoom,
                attributionControl: false
            });

            instance = {
                containerId,
                container,
                map,
                markers: [],
                userMarker: null,
                userLocation: snapshot.userLocation,
                centerKey: null,
                markersKey: null,
                radiusKey: null,
                userLocationKey: null,
                routeKey: null,
                resizeQueued: false,
                resizeFrameId: null,
                disposed: false
            };

            instances.set(containerId, instance);
        }

        queueResize(instance);

        const centerKey = `${snapshot.centerLat.toFixed(6)}|${snapshot.centerLng.toFixed(6)}|${snapshot.zoom.toFixed(2)}`;
        const needsViewportAdjust = instance.centerKey !== centerKey;
        const hasMultipleMarkers = snapshot.markers.length > 1;
        const routeKey = buildRouteKey(snapshot.route);
        const routeChanged = instance.routeKey !== routeKey;

        const markersKey = snapshot.markers
            .map(marker => `${marker.id}:${marker.latitude.toFixed(6)}:${marker.longitude.toFixed(6)}:${getMarkerRadiusMeters(marker)}:${marker.isSelected ? 1 : 0}:${marker.isNearest ? 1 : 0}:${marker.accent}`)
            .join("|");
        const userLocationKey = snapshot.userLocation
            ? `user:${snapshot.userLocation.latitude.toFixed(5)}:${snapshot.userLocation.longitude.toFixed(5)}`
            : "";
        instance.userLocation = snapshot.userLocation;
        let markersChanged = true;

        if (instance.markersKey === markersKey) {
            markersChanged = false;
        }

        const userLocationChanged = instance.userLocationKey !== userLocationKey;

        if (!markersChanged && !userLocationChanged && !routeChanged && !needsViewportAdjust) {
            return;
        }

        if (markersChanged) {
            updateRadiusLayers(instance, snapshot.markers);

            for (const marker of instance.markers) {
                marker.remove();
            }

            instance.markers = snapshot.markers.map(marker => {
                const element = document.createElement("button");
                element.type = "button";
                element.className = `visitor-map-marker${marker.isSelected ? " is-selected" : ""}${marker.isNearest ? " is-nearest" : ""}`;
                element.style.background = marker.accent;
                element.title = marker.label;
                element.addEventListener("click", () => {
                    safeSelectPoi(dotNetRef, marker.id);
                });

                const popupContent = document.createElement("div");
                popupContent.className = "visitor-map-popup__title";
                popupContent.textContent = marker.label;

                return new mapboxgl.Marker({ element })
                    .setLngLat([marker.longitude, marker.latitude])
                    .setPopup(new mapboxgl.Popup({ offset: 16, className: "visitor-map-popup" }).setDOMContent(popupContent))
                    .addTo(instance.map);
            });

            instance.markersKey = markersKey;
        }

        if (userLocationChanged) {
            updateUserMarker(instance, snapshot.userLocation);
            instance.userLocationKey = userLocationKey;
        }

        if (routeChanged) {
            instance.routeKey = routeKey;
            updateRouteLayer(instance, snapshot.route);
        }

        if (needsViewportAdjust || routeChanged) {
            if (hasRenderableRoute(snapshot.route)) {
                fitBoundsForRoute(instance.map, snapshot, snapshot.route);
            } else if (hasMultipleMarkers) {
                const bounds = new mapboxgl.LngLatBounds();
                for (const marker of snapshot.markers) {
                    extendBoundsWithMarkerRadius(bounds, marker);
                }

                instance.map.fitBounds(bounds, {
                    padding: { top: 164, right: 28, bottom: 220, left: 28 },
                    maxZoom: 15.4,
                    duration: 320
                });
            } else {
                instance.map.easeTo({
                    center: [snapshot.centerLng, snapshot.centerLat],
                    zoom: snapshot.zoom,
                    duration: 280
                });
            }
        }

        instance.centerKey = centerKey;
    }

    function renderOfflineFallback(container, snapshot, dotNetRef, message) {
        clearMapboxInstance(container.id);
        const bounds = calculateOfflineBounds(snapshot);
        const fallback = document.createElement("div");
        fallback.className = "visitor-map-offline";

        if (hasRenderableRoute(snapshot.route)) {
            fallback.appendChild(createOfflineRoute(snapshot.route, bounds));
        } else {
            const routeLayer = document.createElement("div");
            routeLayer.className = "visitor-map-offline__route";
            fallback.appendChild(routeLayer);
        }

        const badge = document.createElement("div");
        badge.className = "visitor-map-offline__badge";
        badge.innerHTML = `<strong>Bản đồ offline</strong><span>${escapeHtml(message)}</span>`;
        fallback.appendChild(badge);

        for (const marker of snapshot.markers) {
            const radius = createOfflineRadius(marker, bounds);
            if (radius) {
                fallback.appendChild(radius);
            }
        }

        for (const marker of snapshot.markers) {
            const point = projectOfflinePoint(marker.latitude, marker.longitude, bounds);
            fallback.appendChild(createOfflineMarkerButton(marker, point, dotNetRef));
        }

        if (snapshot.userLocation) {
            const userPoint = projectOfflinePoint(snapshot.userLocation.latitude, snapshot.userLocation.longitude, bounds);
            const userMarker = document.createElement("div");
            userMarker.className = "visitor-map-offline__user";
            userMarker.style.left = `${userPoint.x}%`;
            userMarker.style.top = `${userPoint.y}%`;
            userMarker.title = snapshot.userLocation.label;
            fallback.appendChild(userMarker);
        }

        container.replaceChildren(fallback);
    }

    function createOfflineMarkerButton(marker, point, dotNetRef) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = `visitor-map-offline__marker${marker.isSelected ? " is-selected" : ""}${marker.isNearest ? " is-nearest" : ""}`;
        button.style.left = `${point.x}%`;
        button.style.top = `${point.y}%`;
        button.style.background = marker.accent;
        button.title = marker.label;
        button.innerHTML = `<span>${escapeHtml(marker.label)}</span>`;
        button.addEventListener("click", () => {
            safeSelectPoi(dotNetRef, marker.id);
        });

        return button;
    }

    function createOfflineRadius(marker, bounds) {
        const radiusMeters = getMarkerRadiusMeters(marker);
        if (radiusMeters <= 0) {
            return null;
        }

        const center = projectOfflinePoint(marker.latitude, marker.longitude, bounds);
        const deltas = getRadiusDegreeDeltas(marker.latitude, radiusMeters);
        const north = projectOfflinePoint(marker.latitude + deltas.latDelta, marker.longitude, bounds, false);
        const south = projectOfflinePoint(marker.latitude - deltas.latDelta, marker.longitude, bounds, false);
        const east = projectOfflinePoint(marker.latitude, marker.longitude + deltas.lngDelta, bounds, false);
        const west = projectOfflinePoint(marker.latitude, marker.longitude - deltas.lngDelta, bounds, false);
        const width = clamp(Math.abs(east.x - west.x), 2.5, 96);
        const height = clamp(Math.abs(south.y - north.y), 2.5, 96);

        const radius = document.createElement("div");
        radius.className = `visitor-map-offline__radius${marker.isSelected ? " is-selected" : ""}${marker.isNearest ? " is-nearest" : ""}`;
        radius.style.left = `${center.x}%`;
        radius.style.top = `${center.y}%`;
        radius.style.width = `${width}%`;
        radius.style.height = `${height}%`;
        radius.style.borderColor = toRgba(marker.accent, marker.isSelected ? 0.62 : 0.42);
        radius.style.background = `radial-gradient(circle, ${toRgba(marker.accent, marker.isSelected ? 0.2 : 0.12)}, ${toRgba(marker.accent, 0.04)} 68%, transparent 72%)`;
        radius.title = `${marker.label} • bán kính ${radiusMeters}m`;

        return radius;
    }

    function safeSelectPoi(dotNetRef, markerId) {
        if (!dotNetRef) {
            return;
        }

        dotNetRef.invokeMethodAsync("SelectPoiFromMap", markerId)
            .catch(error => console.warn("[visitorMap] Marker tap callback failed.", error));
    }

    function calculateOfflineBounds(snapshot) {
        const radiusPoints = [];
        for (const marker of snapshot.markers) {
            const radiusMeters = getMarkerRadiusMeters(marker);
            if (radiusMeters <= 0) {
                continue;
            }

            const deltas = getRadiusDegreeDeltas(marker.latitude, radiusMeters);
            radiusPoints.push(
                { latitude: marker.latitude + deltas.latDelta, longitude: marker.longitude },
                { latitude: marker.latitude - deltas.latDelta, longitude: marker.longitude },
                { latitude: marker.latitude, longitude: marker.longitude + deltas.lngDelta },
                { latitude: marker.latitude, longitude: marker.longitude - deltas.lngDelta });
        }

        const points = [
            ...snapshot.markers.map(marker => ({ latitude: marker.latitude, longitude: marker.longitude })),
            ...radiusPoints,
            ...(snapshot.userLocation ? [snapshot.userLocation] : []),
            ...(getRoutePoints(snapshot.route))
        ];

        if (points.length === 0) {
            return {
                minLat: snapshot.centerLat - 0.002,
                maxLat: snapshot.centerLat + 0.002,
                minLng: snapshot.centerLng - 0.002,
                maxLng: snapshot.centerLng + 0.002
            };
        }

        const latitudes = points.map(point => point.latitude);
        const longitudes = points.map(point => point.longitude);
        const minLat = Math.min(...latitudes);
        const maxLat = Math.max(...latitudes);
        const minLng = Math.min(...longitudes);
        const maxLng = Math.max(...longitudes);
        const latPadding = Math.max((maxLat - minLat) * 0.18, 0.0008);
        const lngPadding = Math.max((maxLng - minLng) * 0.18, 0.0008);

        return {
            minLat: minLat - latPadding,
            maxLat: maxLat + latPadding,
            minLng: minLng - lngPadding,
            maxLng: maxLng + lngPadding
        };
    }

    function createOfflineRoute(route, bounds) {
        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("class", "visitor-map-offline__route-svg");
        svg.setAttribute("viewBox", "0 0 100 100");
        svg.setAttribute("preserveAspectRatio", "none");

        const points = getRoutePoints(route)
            .map(point => projectOfflinePoint(point.latitude, point.longitude, bounds))
            .map(point => `${point.x.toFixed(2)},${point.y.toFixed(2)}`)
            .join(" ");

        const line = document.createElementNS("http://www.w3.org/2000/svg", "polyline");
        line.setAttribute("class", "visitor-map-offline__route-line");
        line.setAttribute("points", points);
        line.setAttribute("fill", "none");
        line.setAttribute("stroke", route.accent || "#1ed6af");
        line.setAttribute("stroke-width", "2.8");
        line.setAttribute("stroke-linecap", "round");
        line.setAttribute("stroke-linejoin", "round");
        svg.appendChild(line);

        return svg;
    }

    function projectOfflinePoint(latitude, longitude, bounds, shouldClamp = true) {
        const latRange = Math.max(bounds.maxLat - bounds.minLat, 0.0001);
        const lngRange = Math.max(bounds.maxLng - bounds.minLng, 0.0001);
        const x = ((longitude - bounds.minLng) / lngRange) * 100;
        const y = 100 - ((latitude - bounds.minLat) / latRange) * 100;

        return shouldClamp
            ? { x: clamp(x, 8, 92), y: clamp(y, 16, 86) }
            : { x, y };
    }

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    function escapeHtml(value) {
        const element = document.createElement("span");
        element.textContent = value ?? "";
        return element.innerHTML;
    }

    function buildRouteKey(route) {
        if (!hasRenderableRoute(route)) {
            return "";
        }

        return `${route.accent || "#1ed6af"}|${getRoutePoints(route)
            .map(point => `${point.latitude.toFixed(6)}:${point.longitude.toFixed(6)}`)
            .join("|")}`;
    }

    function hasRenderableRoute(route) {
        return getRoutePoints(route).length >= 2;
    }

    function getRoutePoints(route) {
        if (!route || !Array.isArray(route.points)) {
            return [];
        }

        return route.points.filter(point =>
            Number.isFinite(point.latitude)
            && Number.isFinite(point.longitude));
    }

    function getMarkerRadiusMeters(marker) {
        const radius = Number(marker.radiusMeters ?? marker.geofenceRadiusMeters ?? 0);
        return Number.isFinite(radius) ? Math.max(0, Math.round(radius)) : 0;
    }

    function getRadiusDegreeDeltas(latitude, radiusMeters) {
        const latDelta = radiusMeters / 111320;
        const lngMetersPerDegree = Math.max(111320 * Math.cos(toRadians(latitude)), 1);

        return {
            latDelta,
            lngDelta: radiusMeters / lngMetersPerDegree
        };
    }

    function toRadians(value) {
        return value * Math.PI / 180;
    }

    function toDegrees(value) {
        return value * 180 / Math.PI;
    }

    function toRgba(color, alpha) {
        const match = /^#?([0-9a-f]{6})$/i.exec(color || "");
        if (!match) {
            return `rgba(30, 214, 175, ${alpha})`;
        }

        const value = match[1];
        const red = parseInt(value.slice(0, 2), 16);
        const green = parseInt(value.slice(2, 4), 16);
        const blue = parseInt(value.slice(4, 6), 16);
        return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
    }

    function extendBoundsWithMarkerRadius(bounds, marker) {
        bounds.extend([marker.longitude, marker.latitude]);

        const radiusMeters = getMarkerRadiusMeters(marker);
        if (radiusMeters <= 0) {
            return;
        }

        const deltas = getRadiusDegreeDeltas(marker.latitude, radiusMeters);
        bounds.extend([marker.longitude, marker.latitude + deltas.latDelta]);
        bounds.extend([marker.longitude, marker.latitude - deltas.latDelta]);
        bounds.extend([marker.longitude + deltas.lngDelta, marker.latitude]);
        bounds.extend([marker.longitude - deltas.lngDelta, marker.latitude]);
    }

    function buildRadiusKey(markers) {
        return markers
            .map(marker => `${marker.id}:${marker.latitude.toFixed(6)}:${marker.longitude.toFixed(6)}:${getMarkerRadiusMeters(marker)}:${marker.isSelected ? 1 : 0}:${marker.isNearest ? 1 : 0}:${marker.accent}`)
            .join("|");
    }

    function buildRadiusFeatureCollection(markers) {
        return {
            type: "FeatureCollection",
            features: markers
                .map(marker => buildRadiusFeature(marker))
                .filter(feature => feature !== null)
        };
    }

    function buildRadiusFeature(marker) {
        const radiusMeters = getMarkerRadiusMeters(marker);
        if (radiusMeters <= 0 || !Number.isFinite(marker.latitude) || !Number.isFinite(marker.longitude)) {
            return null;
        }

        const earthRadiusMeters = 6378137;
        const angularDistance = radiusMeters / earthRadiusMeters;
        const centerLat = toRadians(marker.latitude);
        const centerLng = toRadians(marker.longitude);
        const coordinates = [];

        for (let step = 0; step <= 72; step += 1) {
            const bearing = toRadians((step / 72) * 360);
            const lat = Math.asin(
                Math.sin(centerLat) * Math.cos(angularDistance)
                + Math.cos(centerLat) * Math.sin(angularDistance) * Math.cos(bearing));
            const lng = centerLng + Math.atan2(
                Math.sin(bearing) * Math.sin(angularDistance) * Math.cos(centerLat),
                Math.cos(angularDistance) - Math.sin(centerLat) * Math.sin(lat));

            coordinates.push([toDegrees(lng), toDegrees(lat)]);
        }

        return {
            type: "Feature",
            properties: {
                id: marker.id,
                accent: marker.accent || "#1ed6af",
                isSelected: !!marker.isSelected,
                isNearest: !!marker.isNearest,
                radiusMeters
            },
            geometry: {
                type: "Polygon",
                coordinates: [coordinates]
            }
        };
    }

    function fitBoundsForRoute(map, snapshot, route) {
        const bounds = new mapboxgl.LngLatBounds();
        for (const point of getRoutePoints(route)) {
            bounds.extend([point.longitude, point.latitude]);
        }

        if (snapshot.userLocation) {
            bounds.extend([snapshot.userLocation.longitude, snapshot.userLocation.latitude]);
        }

        map.fitBounds(bounds, {
            padding: { top: 164, right: 28, bottom: 220, left: 28 },
            maxZoom: 16.2,
            duration: 360
        });
    }

    function updateRadiusLayers(instance, markers) {
        const requestedRadiusKey = buildRadiusKey(markers);
        instance.radiusKey = requestedRadiusKey;

        const applyRadius = () => {
            if (!isLiveInstance(instance.containerId, instance)) {
                return;
            }

            if (instance.radiusKey !== requestedRadiusKey) {
                return;
            }

            try {
                const radiusData = buildRadiusFeatureCollection(markers);
                if (radiusData.features.length === 0) {
                    removeRadiusLayers(instance.map);
                    return;
                }

                const existingSource = instance.map.getSource(radiusSourceId);
                if (existingSource) {
                    existingSource.setData(radiusData);
                } else {
                    instance.map.addSource(radiusSourceId, {
                        type: "geojson",
                        data: radiusData
                    });
                }

                const beforeLayerId = instance.map.getLayer(routeLayerId) ? routeLayerId : undefined;
                if (!instance.map.getLayer(radiusFillLayerId)) {
                    instance.map.addLayer({
                        id: radiusFillLayerId,
                        type: "fill",
                        source: radiusSourceId,
                        paint: {
                            "fill-color": ["coalesce", ["get", "accent"], "#1ed6af"],
                            "fill-opacity": ["case", ["get", "isSelected"], 0.2, ["get", "isNearest"], 0.14, 0.08]
                        }
                    }, beforeLayerId);
                }

                if (!instance.map.getLayer(radiusLineLayerId)) {
                    instance.map.addLayer({
                        id: radiusLineLayerId,
                        type: "line",
                        source: radiusSourceId,
                        paint: {
                            "line-color": ["coalesce", ["get", "accent"], "#1ed6af"],
                            "line-width": ["case", ["get", "isSelected"], 2.5, ["get", "isNearest"], 2, 1.4],
                            "line-opacity": ["case", ["get", "isSelected"], 0.72, ["get", "isNearest"], 0.54, 0.38]
                        }
                    }, beforeLayerId);
                }
            } catch (error) {
                instance.radiusKey = null;
                console.warn("[visitorMap] Radius layer update failed.", error);
            }
        };

        if (instance.map.isStyleLoaded && instance.map.isStyleLoaded()) {
            applyRadius();
        } else {
            instance.map.once("load", applyRadius);
        }
    }

    function updateRouteLayer(instance, route) {
        const requestedRouteKey = buildRouteKey(route);
        const applyRoute = () => {
            if (!isLiveInstance(instance.containerId, instance)) {
                return;
            }

            if (instance.routeKey !== requestedRouteKey) {
                return;
            }

            try {
                if (!hasRenderableRoute(route)) {
                    removeRouteLayer(instance.map);
                    return;
                }

                const routeData = {
                    type: "Feature",
                    properties: {},
                    geometry: {
                        type: "LineString",
                        coordinates: getRoutePoints(route).map(point => [point.longitude, point.latitude])
                    }
                };
                const existingSource = instance.map.getSource(routeSourceId);
                if (existingSource) {
                    existingSource.setData(routeData);
                } else {
                    instance.map.addSource(routeSourceId, {
                        type: "geojson",
                        data: routeData
                    });
                }

                if (!instance.map.getLayer(routeLayerId)) {
                    instance.map.addLayer({
                        id: routeLayerId,
                        type: "line",
                        source: routeSourceId,
                        layout: {
                            "line-cap": "round",
                            "line-join": "round"
                        },
                        paint: {
                            "line-color": route.accent || "#1ed6af",
                            "line-width": 5,
                            "line-opacity": 0.86
                        }
                    });
                } else {
                    instance.map.setPaintProperty(routeLayerId, "line-color", route.accent || "#1ed6af");
                }
            } catch (error) {
                instance.routeKey = null;
                console.warn("[visitorMap] Route layer update failed.", error);
            }
        };

        if (instance.map.isStyleLoaded && instance.map.isStyleLoaded()) {
            applyRoute();
        } else {
            instance.map.once("load", applyRoute);
        }
    }

    function removeRouteLayer(map) {
        try {
            if (map.getLayer(routeLayerId)) {
                map.removeLayer(routeLayerId);
            }

            if (map.getSource(routeSourceId)) {
                map.removeSource(routeSourceId);
            }
        } catch (error) {
            console.warn("[visitorMap] Route layer cleanup failed.", error);
        }
    }

    function removeRadiusLayers(map) {
        try {
            if (map.getLayer(radiusLineLayerId)) {
                map.removeLayer(radiusLineLayerId);
            }

            if (map.getLayer(radiusFillLayerId)) {
                map.removeLayer(radiusFillLayerId);
            }

            if (map.getSource(radiusSourceId)) {
                map.removeSource(radiusSourceId);
            }
        } catch (error) {
            console.warn("[visitorMap] Radius layer cleanup failed.", error);
        }
    }

    function queueResize(instance) {
        if (instance.resizeQueued) {
            return;
        }

        instance.resizeQueued = true;
        const containerId = instance.containerId;
        instance.resizeFrameId = requestAnimationFrame(() => {
            instance.resizeQueued = false;
            instance.resizeFrameId = null;

            if (!isLiveInstance(containerId, instance)) {
                return;
            }

            try {
                instance.map.resize();
            } catch (error) {
                console.warn("[visitorMap] Resize skipped.", error);
            }
        });
    }

    function isLiveInstance(containerId, instance) {
        const currentContainer = document.getElementById(containerId);
        return !instance.disposed
            && instances.get(containerId) === instance
            && !!instance.container
            && instance.container === currentContainer
            && document.contains(instance.container);
    }

    function updateUserMarker(instance, userLocation) {
        if (!userLocation) {
            if (instance.userMarker) {
                instance.userMarker.remove();
                instance.userMarker = null;
            }

            return;
        }

        if (instance.userMarker) {
            instance.userMarker.setLngLat([userLocation.longitude, userLocation.latitude]);
            instance.userMarker.getElement().title = userLocation.label;
            return;
        }

        const userElement = document.createElement("div");
        userElement.className = "visitor-map-user-marker";
        userElement.title = userLocation.label;

        instance.userMarker = new mapboxgl.Marker({ element: userElement, anchor: "center" })
            .setLngLat([userLocation.longitude, userLocation.latitude])
            .addTo(instance.map);
    }

    function centerOnUser(containerId) {
        const instance = instances.get(containerId);
        if (!instance || !isLiveInstance(containerId, instance) || !instance.userLocation) {
            return;
        }

        try {
            const currentZoom = typeof instance.map.getZoom === "function" ? instance.map.getZoom() : 15;
            instance.map.flyTo({
                center: [instance.userLocation.longitude, instance.userLocation.latitude],
                zoom: Math.max(currentZoom, 15),
                duration: 360,
                essential: true
            });
        } catch (error) {
            console.warn("[visitorMap] Center on user skipped.", error);
        }
    }

    function dispose(containerId) {
        clearMapboxInstance(containerId);
    }

    function clearMapboxInstance(containerId) {
        const instance = instances.get(containerId);
        if (!instance) {
            return;
        }

        instance.disposed = true;
        if (instance.resizeFrameId) {
            cancelAnimationFrame(instance.resizeFrameId);
            instance.resizeFrameId = null;
        }

        for (const marker of instance.markers) {
            try {
                marker.remove();
            } catch (error) {
                console.warn("[visitorMap] Marker cleanup skipped.", error);
            }
        }

        if (instance.userMarker) {
            try {
                instance.userMarker.remove();
            } catch (error) {
                console.warn("[visitorMap] User marker cleanup skipped.", error);
            }
        }

        try {
            instance.map.remove();
        } catch (error) {
            console.warn("[visitorMap] Map cleanup skipped.", error);
        }
        instances.delete(containerId);
    }

    return {
        render,
        centerOnUser,
        dispose
    };
})();
