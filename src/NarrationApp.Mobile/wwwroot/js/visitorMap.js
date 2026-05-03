window.visitorMap = (() => {
    const instances = new Map();

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

        mapboxgl.accessToken = accessToken;

        let instance = instances.get(containerId);
        if (!instance) {
            container.innerHTML = "";
            const map = new mapboxgl.Map({
                container: containerId,
                style: styleUrl,
                center: [snapshot.centerLng, snapshot.centerLat],
                zoom: snapshot.zoom,
                attributionControl: false
            });

            instance = {
                map,
                markers: [],
                userMarker: null,
                centerKey: null,
                markersKey: null,
                userLocationKey: null,
                resizeQueued: false
            };

            instances.set(containerId, instance);
        }

        queueResize(instance);

        const centerKey = `${snapshot.centerLat.toFixed(6)}|${snapshot.centerLng.toFixed(6)}|${snapshot.zoom.toFixed(2)}`;
        const needsViewportAdjust = instance.centerKey !== centerKey;
        const hasMultipleMarkers = snapshot.markers.length > 1;

        const markersKey = snapshot.markers
            .map(marker => `${marker.id}:${marker.latitude.toFixed(6)}:${marker.longitude.toFixed(6)}:${marker.isSelected ? 1 : 0}:${marker.isNearest ? 1 : 0}:${marker.accent}`)
            .join("|");
        const userLocationKey = snapshot.userLocation
            ? `user:${snapshot.userLocation.latitude.toFixed(5)}:${snapshot.userLocation.longitude.toFixed(5)}`
            : "";
        let markersChanged = true;

        if (instance.markersKey === markersKey) {
            markersChanged = false;
        }

        const userLocationChanged = instance.userLocationKey !== userLocationKey;

        if (!markersChanged && !userLocationChanged && !needsViewportAdjust) {
            return;
        }

        if (markersChanged) {
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
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync("SelectPoiFromMap", marker.id);
                    }
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

        if (needsViewportAdjust) {
            if (hasMultipleMarkers) {
                const bounds = new mapboxgl.LngLatBounds();
                for (const marker of snapshot.markers) {
                    bounds.extend([marker.longitude, marker.latitude]);
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

        const routeLayer = document.createElement("div");
        routeLayer.className = "visitor-map-offline__route";
        fallback.appendChild(routeLayer);

        const badge = document.createElement("div");
        badge.className = "visitor-map-offline__badge";
        badge.innerHTML = `<strong>Bản đồ offline</strong><span>${escapeHtml(message)}</span>`;
        fallback.appendChild(badge);

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
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("SelectPoiFromMap", marker.id);
            }
        });

        return button;
    }

    function calculateOfflineBounds(snapshot) {
        const points = [
            ...snapshot.markers.map(marker => ({ latitude: marker.latitude, longitude: marker.longitude })),
            ...(snapshot.userLocation ? [snapshot.userLocation] : [])
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

    function projectOfflinePoint(latitude, longitude, bounds) {
        const latRange = Math.max(bounds.maxLat - bounds.minLat, 0.0001);
        const lngRange = Math.max(bounds.maxLng - bounds.minLng, 0.0001);
        return {
            x: clamp(((longitude - bounds.minLng) / lngRange) * 100, 8, 92),
            y: clamp(100 - ((latitude - bounds.minLat) / latRange) * 100, 16, 86)
        };
    }

    function clamp(value, min, max) {
        return Math.min(max, Math.max(min, value));
    }

    function escapeHtml(value) {
        const element = document.createElement("span");
        element.textContent = value ?? "";
        return element.innerHTML;
    }

    function queueResize(instance) {
        if (instance.resizeQueued) {
            return;
        }

        instance.resizeQueued = true;
        requestAnimationFrame(() => {
            instance.resizeQueued = false;
            instance.map.resize();
        });
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

    function zoomIn(containerId) {
        const instance = instances.get(containerId);
        if (instance) {
            instance.map.zoomIn({ duration: 300 });
        }
    }

    function zoomOut(containerId) {
        const instance = instances.get(containerId);
        if (instance) {
            instance.map.zoomOut({ duration: 300 });
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

        for (const marker of instance.markers) {
            marker.remove();
        }

        if (instance.userMarker) {
            instance.userMarker.remove();
        }

        instance.map.remove();
        instances.delete(containerId);
    }

    return {
        render,
        zoomIn,
        zoomOut,
        dispose
    };
})();
