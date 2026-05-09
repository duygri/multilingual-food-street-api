window.adminAnalyticsMap = (() => {
    const instances = new Map();
    const district4Center = [106.7045, 10.7604];
    const district4Bounds = [[106.691, 10.747], [106.718, 10.7735]];
    const district4MaxBounds = [[106.6875, 10.7435], [106.7215, 10.777]];
    const fallbackCenter = district4Center;
    const fallbackZoom = 14.1;
    const fallbackStyleUrl = "mapbox://styles/mapbox/light-v11";

    function normalizeStyleUrl(styleUrl) {
        if (typeof styleUrl !== "string" || !styleUrl.trim()) {
            return fallbackStyleUrl;
        }

        return styleUrl.trim();
    }

    function ensureContainer(containerId) {
        const container = document.getElementById(containerId);
        if (!container) {
            return null;
        }

        if (!window.mapboxgl) {
            container.innerHTML = "<div style='padding:18px;color:#eff6ff;'>Mapbox chưa tải được.</div>";
            return null;
        }

        return container;
    }

    function getOrCreateInstance(containerId, accessToken, styleUrl) {
        const effectiveStyleUrl = normalizeStyleUrl(styleUrl);
        let instance = instances.get(containerId);
        if (instance) {
            if (instance.styleUrl !== effectiveStyleUrl) {
                instance.styleUrl = effectiveStyleUrl;
                instance.map.setStyle(effectiveStyleUrl);
            }

            return instance;
        }

        mapboxgl.accessToken = accessToken;

        const map = new mapboxgl.Map({
            container: containerId,
            style: effectiveStyleUrl,
            center: fallbackCenter,
            zoom: fallbackZoom,
            maxBounds: district4MaxBounds,
            attributionControl: false
        });

        instance = { map, styleUrl: effectiveStyleUrl };
        instances.set(containerId, instance);
        return instance;
    }

    function updateGeoJsonSource(map, sourceId, geoJson) {
        const existingSource = map.getSource(sourceId);
        if (existingSource) {
            existingSource.setData(geoJson);
            return;
        }

        map.addSource(sourceId, {
            type: "geojson",
            data: geoJson
        });
    }

    function removeLayerIfExists(map, layerId) {
        if (map.getLayer(layerId)) {
            map.removeLayer(layerId);
        }
    }

    function removeSourceIfExists(map, sourceId) {
        if (map.getSource(sourceId)) {
            map.removeSource(sourceId);
        }
    }

    function isWithinDistrict4(point) {
        const [lng, lat] = point;
        const [southWest, northEast] = district4Bounds;
        return lng >= southWest[0]
            && lng <= northEast[0]
            && lat >= southWest[1]
            && lat <= northEast[1];
    }

    function filterPointsWithinDistrict4(points) {
        return points.filter(point => isWithinDistrict4([point.lng, point.lat]));
    }

    function filterFlowsWithinDistrict4(flows) {
        return flows.filter(flow =>
            isWithinDistrict4([flow.fromLng, flow.fromLat])
                && isWithinDistrict4([flow.toLng, flow.toLat]));
    }

    function fitDistrict4Bounds(map) {
        const bounds = new mapboxgl.LngLatBounds(district4Bounds[0], district4Bounds[1]);
        map.fitBounds(bounds, {
            padding: 36,
            duration: 0,
            maxZoom: 14.8
        });
    }

    function applyHeatmap(map, points) {
        const sourceId = "analytics-heatmap-source";
        const heatLayerId = "analytics-heatmap-layer";
        const pointLayerId = "analytics-heatmap-points";
        const scopedPoints = filterPointsWithinDistrict4(points ?? []);
        const geoJson = {
            type: "FeatureCollection",
            features: scopedPoints.map(point => ({
                type: "Feature",
                properties: { weight: point.weight },
                geometry: {
                    type: "Point",
                    coordinates: [point.lng, point.lat]
                }
            }))
        };

        updateGeoJsonSource(map, sourceId, geoJson);

        if (!map.getLayer(heatLayerId)) {
            map.addLayer({
                id: heatLayerId,
                type: "heatmap",
                source: sourceId,
                paint: {
                    "heatmap-weight": ["interpolate", ["linear"], ["get", "weight"], 0, 0, 10, 0.35, 25, 0.7, 50, 1],
                    "heatmap-intensity": ["interpolate", ["linear"], ["zoom"], 0, 0.8, 12, 1.5],
                    "heatmap-color": [
                        "interpolate",
                        ["linear"],
                        ["heatmap-density"],
                        0,
                        "rgba(56, 189, 248, 0)",
                        0.2,
                        "rgba(34, 211, 238, 0.35)",
                        0.45,
                        "rgba(16, 185, 129, 0.5)",
                        0.7,
                        "rgba(250, 204, 21, 0.72)",
                        1,
                        "rgba(248, 113, 113, 0.95)"
                    ],
                    "heatmap-radius": ["interpolate", ["linear"], ["zoom"], 0, 18, 12, 32],
                    "heatmap-opacity": 0.92
                }
            });
        }

        if (!map.getLayer(pointLayerId)) {
            map.addLayer({
                id: pointLayerId,
                type: "circle",
                source: sourceId,
                minzoom: 11,
                paint: {
                    "circle-radius": ["interpolate", ["linear"], ["get", "weight"], 0, 4, 25, 8, 50, 12],
                    "circle-color": "rgba(125, 211, 252, 0.78)",
                    "circle-stroke-width": 1,
                    "circle-stroke-color": "rgba(226, 232, 240, 0.55)",
                    "circle-opacity": 0.45
                }
            });
        }

        fitDistrict4Bounds(map);
    }

    function buildFlowNodeFeatures(flows) {
        const nodes = new Map();

        flows.forEach(flow => {
            const fromKey = `${flow.fromPoiId}:from`;
            const toKey = `${flow.toPoiId}:to`;

            if (!nodes.has(fromKey)) {
                nodes.set(fromKey, {
                    type: "Feature",
                    properties: { label: flow.fromPoiName, weight: flow.weight },
                    geometry: {
                        type: "Point",
                        coordinates: [flow.fromLng, flow.fromLat]
                    }
                });
            }

            if (!nodes.has(toKey)) {
                nodes.set(toKey, {
                    type: "Feature",
                    properties: { label: flow.toPoiName, weight: flow.weight },
                    geometry: {
                        type: "Point",
                        coordinates: [flow.toLng, flow.toLat]
                    }
                });
            }
        });

        return Array.from(nodes.values());
    }

    function applyFlows(map, flows) {
        const lineSourceId = "analytics-flow-source";
        const nodeSourceId = "analytics-flow-node-source";
        const lineLayerId = "analytics-flow-layer";
        const nodeLayerId = "analytics-flow-nodes";
        const scopedFlows = filterFlowsWithinDistrict4(flows ?? []);
        const lineGeoJson = {
            type: "FeatureCollection",
            features: scopedFlows.map(flow => ({
                type: "Feature",
                properties: {
                    weight: flow.weight,
                    uniqueSessions: flow.uniqueSessions,
                    label: `${flow.fromPoiName} -> ${flow.toPoiName}`
                },
                geometry: {
                    type: "LineString",
                    coordinates: [
                        [flow.fromLng, flow.fromLat],
                        [flow.toLng, flow.toLat]
                    ]
                }
            }))
        };
        const nodeGeoJson = {
            type: "FeatureCollection",
            features: buildFlowNodeFeatures(scopedFlows)
        };

        updateGeoJsonSource(map, lineSourceId, lineGeoJson);
        updateGeoJsonSource(map, nodeSourceId, nodeGeoJson);

        if (!map.getLayer(lineLayerId)) {
            map.addLayer({
                id: lineLayerId,
                type: "line",
                source: lineSourceId,
                layout: {
                    "line-cap": "round",
                    "line-join": "round"
                },
                paint: {
                    "line-color": [
                        "interpolate",
                        ["linear"],
                        ["get", "weight"],
                        1,
                        "rgba(45, 212, 191, 0.45)",
                        3,
                        "rgba(34, 211, 238, 0.68)",
                        8,
                        "rgba(59, 130, 246, 0.82)"
                    ],
                    "line-width": [
                        "interpolate",
                        ["linear"],
                        ["get", "weight"],
                        1,
                        2,
                        3,
                        4,
                        8,
                        7
                    ],
                    "line-opacity": 0.88
                }
            });
        }

        if (!map.getLayer(nodeLayerId)) {
            map.addLayer({
                id: nodeLayerId,
                type: "circle",
                source: nodeSourceId,
                paint: {
                    "circle-radius": 5,
                    "circle-color": "rgba(191, 219, 254, 0.9)",
                    "circle-stroke-width": 1.2,
                    "circle-stroke-color": "rgba(15, 23, 42, 0.95)"
                }
            });
        }

        fitDistrict4Bounds(map);
    }

    function renderWithStyle(containerId, accessToken, styleUrl, callback) {
        const container = ensureContainer(containerId);
        if (!container || !accessToken) {
            return;
        }

        const instance = getOrCreateInstance(containerId, accessToken, styleUrl);
        const { map } = instance;
        let hasApplied = false;

        const apply = () => {
            if (hasApplied) {
                return;
            }

            hasApplied = true;
            callback(map);
            map.resize();
        };

        if (map.isStyleLoaded()) {
            apply();
            return;
        }

        map.once("load", apply);
        map.once("style.load", apply);
    }

    function renderHeatmap(containerId, accessToken, styleUrl, points) {
        renderWithStyle(containerId, accessToken, styleUrl, map => applyHeatmap(map, points ?? []));
    }

    function renderFlows(containerId, accessToken, styleUrl, flows) {
        renderWithStyle(containerId, accessToken, styleUrl, map => applyFlows(map, flows ?? []));
    }

    function dispose(containerId) {
        const instance = instances.get(containerId);
        if (!instance) {
            return;
        }

        const { map } = instance;
        removeLayerIfExists(map, "analytics-heatmap-layer");
        removeLayerIfExists(map, "analytics-heatmap-points");
        removeLayerIfExists(map, "analytics-flow-layer");
        removeLayerIfExists(map, "analytics-flow-nodes");
        removeSourceIfExists(map, "analytics-heatmap-source");
        removeSourceIfExists(map, "analytics-flow-source");
        removeSourceIfExists(map, "analytics-flow-node-source");
        map.remove();
        instances.delete(containerId);
    }

    return {
        renderHeatmap,
        renderFlows,
        dispose
    };
})();
