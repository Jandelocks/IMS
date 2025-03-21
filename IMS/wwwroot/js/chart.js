document.addEventListener("DOMContentLoaded", function () {
    // Default filters
    let incidentFilter = "monthly";
    let userRoleFilter = "monthly";

    // Initialize Charts
    let incidentChartCtx = document.getElementById("incidentChart").getContext("2d");
    let userRoleChartCtx = document.getElementById("userRoleChart").getContext("2d");

    let incidentChart = new Chart(incidentChartCtx, {
        type: "radar",
        data: { labels: [], datasets: [{ label: "Incidents", data: [], backgroundColor: ["#ff6384", "#36a2eb", "#ffce56", "#4bc0c0", "#9966ff", "#ff9f40"] }] },
        options: { responsive: true }
    });

    let userRoleChart = new Chart(userRoleChartCtx, {
        type: "pie",
        data: { labels: [], datasets: [{ label: "Users", data: [], backgroundColor: ["#ff6384", "#36a2eb", "#ffce56"] }] },
        options: { responsive: true }
    });

    // Fetch Data Functions
    function fetchIncidentData() {
        fetch(`/Charts/GetIncidentData?filter=${incidentFilter}`)
            .then(response => response.json())
            .then(data => {
                incidentChart.data.labels = data.map(d => d.category);
                incidentChart.data.datasets[0].data = data.map(d => d.count);
                incidentChart.update();
            });
    }

    function fetchUserRoleData() {
        fetch(`/Charts/GetUserRoleData?filter=${userRoleFilter}`)
            .then(response => response.json())
            .then(data => {
                userRoleChart.data.labels = data.map(d => d.role);
                userRoleChart.data.datasets[0].data = data.map(d => d.count);
                userRoleChart.update();
            });
    }

    // Event Listeners for Buttons
    document.querySelectorAll(".incident-filter").forEach(button => {
        button.addEventListener("click", function () {
            incidentFilter = this.getAttribute("data-filter");
            fetchIncidentData();
        });
    });

    document.querySelectorAll(".user-role-filter").forEach(button => {
        button.addEventListener("click", function () {
            userRoleFilter = this.getAttribute("data-filter");
            fetchUserRoleData();
        });
    });

    // Load initial data
    fetchIncidentData();
    fetchUserRoleData();
});



document.addEventListener("DOMContentLoaded", function () {
    const userId = document.getElementById("moderatorChart").getAttribute("data-userid");

    fetch(`/Charts/GetModeratorIncidentChart?userId=${userId}`)
        .then(response => response.json())
        .then(data => {
            const ctx = document.getElementById("moderatorChart").getContext("2d");

            new Chart(ctx, {
                type: "bar",
                data: {
                    labels: ["Daily", "Monthly", "Yearly"],
                    datasets: [{
                        label: "Incidents Assigned",
                        data: [data.daily, data.monthly, data.yearly],
                        backgroundColor: ["#ff6384", "#36a2eb", "#ffce56"],
                        borderColor: ["#cc2e49", "#287dbd", "#d1a330"],
                        borderWidth: 1
                    }]
                },
                options: {
                    responsive: true,
                    scales: {
                        y: { beginAtZero: true }
                    }
                }
            });
        })
        .catch(error => console.error("Error loading chart data:", error));
});


document.addEventListener("DOMContentLoaded", function () {
    let chartInstance;
    let ctx = document.getElementById("incidentChartcount").getContext("2d");

    fetch("/Charts/GetIncidentCounts")
        .then(response => response.json())
        .then(data => {
            let datasets = {
                daily: {
                    labels: data.daily.map(d => moment(d.date).format("MMM D, YYYY")),
                    counts: data.daily.map(d => d.count)
                },
                monthly: {
                    labels: data.monthly.map(m => moment(`${m.year}-${m.month}-01`).format("MMM YYYY")),
                    counts: data.monthly.map(m => m.count)
                },
                yearly: {
                    labels: data.yearly.map(y => y.year.toString()),
                    counts: data.yearly.map(y => y.count)
                }
            };

            function updateChart(type) {
                if (chartInstance) chartInstance.destroy();

                chartInstance = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: datasets[type].labels,
                        datasets: [{
                            label: `Incidents (${type.charAt(0).toUpperCase() + type.slice(1)})`,
                            data: datasets[type].counts,
                            backgroundColor: type === "daily" ? "#4bc0c0" : type === "monthly" ? "#36a2eb" : "#ffce56",
                            borderColor: type === "daily" ? "#4bc0c0" : type === "monthly" ? "#36a2eb" : "#ffce56",
                            borderWidth: 1,
                            fill: true,
                        }]
                    }
                });
            }

            // Default chart (daily)
            updateChart("daily");

            // Button click event listener
            document.querySelectorAll(".total-incidents").forEach(btn => {
                btn.addEventListener("click", function () {
                    updateChart(this.getAttribute("data-filter"));
                });
            });
        });
});

document.addEventListener("DOMContentLoaded", function () {
    let chartInstance;
    let ctx = document.getElementById("departmentIncidentChart").getContext("2d");

    function fetchIncidentData(filterType) {
        fetch("/Charts/GetModeratorDepartmentIncidents")
            .then(response => response.json())
            .then(data => {
                if (data.error) {
                    console.error(data.error);
                    return;
                }

                // Extract department and category-wise counts
                let department = data[0]?.department?.department || "Unknown Department";
                let incidentCounts = {
                    daily: data[0]?.Daily || 0,
                    monthly: data[0]?.Monthly || 0,
                    yearly: data[0]?.Yearly || 0
                };

                let categoryLabels = data[0]?.categories.map(c => c.category) || ["No Data"];
                let categoryCounts = data[0]?.categories.map(c => c.count) || [0];

                updateChart(department, filterType, incidentCounts, categoryLabels, categoryCounts);
            })
            .catch(error => console.error("Error fetching incident data:", error));
    }

    function updateChart(department, type, counts, categoryLabels, categoryCounts) {
        if (chartInstance) chartInstance.destroy();

        chartInstance = new Chart(ctx, {
            type: "bar",
            data: {
                labels: categoryLabels.length > 0 ? categoryLabels : ["No Data"],
                datasets: [
                    {
                        label: `Incidents (${type.charAt(0).toUpperCase() + type.slice(1)})`,
                        data: categoryCounts.length > 0 ? categoryCounts : [0],
                        backgroundColor: "#36a2eb",
                        borderColor: "#007bff",
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                scales: {
                    y: { beginAtZero: true }
                }
            }
        });
    }

    // Default load (Daily)
    fetchIncidentData("daily");

    // Button click event listener
    document.querySelectorAll(".incident-filter").forEach(btn => {
        btn.addEventListener("click", function () {
            let filterType = this.getAttribute("data-filter");
            fetchIncidentData(filterType);
        });
    });
});
