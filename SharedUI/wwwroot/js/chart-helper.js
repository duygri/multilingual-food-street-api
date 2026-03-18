// Chart.js helper for Admin Analytics Dashboard
window.ChartHelper = (() => {
    const charts = {};

    function destroyIfExists(id) {
        if (charts[id]) {
            charts[id].destroy();
            delete charts[id];
        }
    }

    /**
     * Render a line chart for play count over time.
     * @param {string} canvasId - ID of the <canvas> element
     * @param {string[]} labels  - Array of date strings
     * @param {number[]} data    - Array of play counts
     */
    function renderTimeline(canvasId, labels, data) {
        destroyIfExists(canvasId);

        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        const gradient = ctx.getContext('2d').createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, 'rgba(99,102,241,0.35)');
        gradient.addColorStop(1, 'rgba(99,102,241,0.02)');

        charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Lượt nghe',
                    data,
                    borderColor: '#6366f1',
                    backgroundColor: gradient,
                    borderWidth: 2.5,
                    tension: 0.4,
                    fill: true,
                    pointBackgroundColor: '#6366f1',
                    pointRadius: 4,
                    pointHoverRadius: 6,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#1e1b4b',
                        titleColor: '#c7d2fe',
                        bodyColor: '#e0e7ff',
                        padding: 10,
                        callbacks: {
                            label: ctx => ` ${ctx.parsed.y} lượt nghe`
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        ticks: { color: '#6b7280', maxTicksLimit: 10 }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(0,0,0,0.05)' },
                        ticks: { color: '#6b7280', precision: 0 }
                    }
                }
            }
        });
    }

    /**
     * Render a doughnut chart.
     * @param {string} canvasId
     * @param {string[]} labels
     * @param {number[]} data
     * @param {string[]} colors
     */
    function renderDoughnut(canvasId, labels, data, colors) {
        destroyIfExists(canvasId);

        const ctx = document.getElementById(canvasId);
        if (!ctx) return;

        charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data,
                    backgroundColor: colors || ['#6366f1', '#10b981', '#f59e0b', '#ef4444'],
                    borderWidth: 2,
                    borderColor: '#fff',
                    hoverOffset: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { color: '#374151', padding: 14, usePointStyle: true }
                    },
                    tooltip: {
                        backgroundColor: '#1e1b4b',
                        titleColor: '#c7d2fe',
                        bodyColor: '#e0e7ff',
                    }
                }
            }
        });
    }

    return { renderTimeline, renderDoughnut };
})();
